using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Naninovel.Metadata;
using Naninovel.Parsing;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows generating metadata to be used in external tools, eg IDE extension.
    /// </summary>
    public static class MetadataGenerator
    {
        /// <summary>
        /// Generates project-specific metadata (actors, resources, custom commands, etc).
        /// Doesn't include built-in commands, expression functions and constants.
        /// </summary>
        public static Project GenerateProjectMetadata ()
        {
            try
            {
                var meta = new Project();
                var customCommands = Command.CommandTypes.Values.Where(IsCustomCommand).ToList();
                var options = new MetadataOptions(customCommands, DisplayProgress, ResolveCustomCommandDocs, ResolveCustomParameterDocs);
                var providers = TypeCache.GetTypesDerivedFrom(typeof(IMetadataProvider)).Select(Activator.CreateInstance).Cast<IMetadataProvider>();
                foreach (var provider in providers)
                    meta = MergeMetadata(meta, provider.GetMetadata(options));
                return meta;
            }
            finally { EditorUtility.ClearProgressBar(); }

            bool IsCustomCommand (Type type)
            {
                return type.Namespace != Command.DefaultNamespace || Compiler.Commands.ContainsKey(type.Name);
            }

            void DisplayProgress (string info, float progress)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Generating Metadata", info, progress))
                    throw new OperationCanceledException("Metadata generation cancelled by the user.");
            }
        }

        /// <summary>
        /// Generates metadata for all the available commands, both built-in and custom.
        /// Doesn't include documentation.
        /// </summary>
        public static Metadata.Command[] GenerateCommandsMetadata ()
        {
            return GenerateCommandsMetadata(Command.CommandTypes.Values, _ => default, _ => null);
        }

        /// <summary>
        /// Generates metadata for the provided command types.
        /// </summary>
        /// <param name="commands">Command types to generate metadata for.</param>
        /// <param name="getCommandDoc">Function to retrieve documentation for command of the specified type.</param>
        /// <param name="getParamDoc">Function to retrieve documentation for parameter of the specified field.</param>
        public static Metadata.Command[] GenerateCommandsMetadata (IReadOnlyCollection<Type> commands,
            Func<Type, CommandDocumentation> getCommandDoc, Func<FieldInfo, string> getParamDoc)
        {
            var commandsMeta = new List<Metadata.Command>();
            foreach (var commandType in commands)
            {
                Compiler.Commands.TryGetValue(commandType.Name, out var locale);
                var commandDoc = getCommandDoc(commandType);
                var metadata = new Metadata.Command {
                    Id = commandType.Name,
                    Alias = !string.IsNullOrWhiteSpace(locale.Alias) ? locale.Alias
                        : ReflectionUtils.GetAttributeValue<Command.CommandAliasAttribute>(commandType, 0) as string,
                    Localizable = typeof(Command.ILocalizable).IsAssignableFrom(commandType),
                    Nest = ResolveNestMeta(commandType),
                    Branch = ResolveBranchMeta(commandType),
                    Documentation = CreateDocsMeta(
                        !string.IsNullOrWhiteSpace(locale.Summary) ? locale.Summary : commandDoc.Summary,
                        !string.IsNullOrWhiteSpace(locale.Remarks) ? locale.Remarks : commandDoc.Remarks,
                        !string.IsNullOrWhiteSpace(locale.Example) ? locale.Example : commandDoc.Examples),
                    Parameters = GenerateParametersMetadata(commandType, locale, getParamDoc)
                };
                commandsMeta.Add(metadata);
            }
            return commandsMeta.OrderBy(c => string.IsNullOrEmpty(c.Alias) ? c.Id : c.Alias).ToArray();
        }

        /// <summary>
        /// Generated constants metadata based on <see cref="ConstantContextAttribute"/> assigned to the commands
        /// of the provided types and expression functions (enums only).
        /// </summary>
        public static Constant[] GenerateConstantsMetadata (IEnumerable<Type> commands, IEnumerable<ExpressionFunction> functions)
        {
            var enumTypes = new HashSet<Type>();
            foreach (var command in commands)
            {
                if (command.GetCustomAttribute<ConstantContextAttribute>() is ConstantContextAttribute cmdAttr && cmdAttr.EnumType != null)
                    enumTypes.Add(cmdAttr.EnumType);
                foreach (var param in GetParameterFields(command))
                    if (param.GetCustomAttribute<ConstantContextAttribute>() is ConstantContextAttribute paramAttr && paramAttr.EnumType != null)
                        enumTypes.Add(paramAttr.EnumType);
            }
            foreach (var fn in functions)
            foreach (var param in fn.Method.GetParameters())
                if (param.GetCustomAttribute<ConstantContextAttribute>() is ConstantContextAttribute paramAttr && paramAttr.EnumType != null)
                    enumTypes.Add(paramAttr.EnumType);
            var constants = new List<Constant>();
            foreach (var type in enumTypes)
            {
                var values = Enum.GetNames(type);
                if (Compiler.Constants.TryGetValue(type.Name, out var l10n))
                    for (int i = 0; i < values.Length; i++)
                        if (l10n.Values.FirstOrDefault(v => v.Value.EqualsFastIgnoreCase(values[i])) is ConstantValueLocalization cv)
                            if (!string.IsNullOrWhiteSpace(cv.Alias))
                                values[i] = cv.Alias;
                constants.Add(new Constant { Name = type.Name, Values = values });
            }
            return constants.ToArray();
        }

        /// <summary>
        /// Generates metadata for the resources stored via editor provider.
        /// </summary>
        public static Metadata.Resource[] GenerateResourcesMetadata ()
        {
            var resources = new List<Metadata.Resource>();
            var editorResources = EditorResources.LoadOrDefault();
            var records = editorResources.GetAllRecords();
            foreach (var kv in records)
            {
                var record = editorResources.GetRecordByGuid(kv.Value);
                if (!record.HasValue) continue;
                var resource = new Metadata.Resource {
                    Type = record.Value.PathPrefix,
                    Path = record.Value.Name
                };
                resources.Add(resource);
            }
            return resources.ToArray();
        }

        /// <summary>
        /// Generates metadata for the actors stored via editor provider.
        /// </summary>
        public static Actor[] GenerateActorsMetadata ()
        {
            var actors = new List<Actor>();
            var editorResources = EditorResources.LoadOrDefault();
            var allResources = editorResources.GetAllRecords().Keys.ToArray();
            var chars = Configuration.GetOrDefault<CharactersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in chars)
            {
                var charActor = new Actor {
                    Id = kv.Key,
                    Description = kv.Value.HasName ? kv.Value.DisplayName : "",
                    Type = kv.Value.Loader.PathPrefix,
                    Appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                };
                actors.Add(charActor);
            }
            var backs = Configuration.GetOrDefault<BackgroundsConfiguration>().Metadata.ToDictionary();
            foreach (var kv in backs)
            {
                var backActor = new Actor {
                    Id = kv.Key,
                    Type = kv.Value.Loader.PathPrefix,
                    Appearances = FindAppearances(kv.Key, kv.Value.Loader.PathPrefix, kv.Value.Implementation)
                };
                actors.Add(backActor);
            }
            var choiceHandlers = Configuration.GetOrDefault<ChoiceHandlersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in choiceHandlers)
            {
                var choiceHandlerActor = new Actor {
                    Id = kv.Key,
                    Type = kv.Value.Loader.PathPrefix
                };
                actors.Add(choiceHandlerActor);
            }
            var printers = Configuration.GetOrDefault<TextPrintersConfiguration>().Metadata.ToDictionary();
            foreach (var kv in printers)
            {
                var printerActor = new Actor {
                    Id = kv.Key,
                    Type = kv.Value.Loader.PathPrefix
                };
                actors.Add(printerActor);
            }
            return actors.ToArray();

            string[] FindAppearances (string actorId, string pathPrefix, string actorImplementation)
            {
                var prefabPath = allResources.FirstOrDefault(p => p.EndsWithFast($"{pathPrefix}/{actorId}"));
                var assetGUID = prefabPath != null ? editorResources.GetGuidByPath(prefabPath) : null;
                var assetPath = assetGUID != null ? AssetDatabase.GUIDToAssetPath(assetGUID) : null;
                var prefabAsset = assetPath != null ? AssetDatabase.LoadMainAssetAtPath(assetPath) : null;
                if (prefabAsset && actorImplementation.Contains("Layered"))
                {
                    var layeredBehaviour = (prefabAsset as GameObject)?.GetComponent<LayeredActorBehaviour>();
                    return layeredBehaviour ? layeredBehaviour.GetCompositionMap().Keys.ToArray() : Array.Empty<string>();
                }
                if (prefabAsset && (actorImplementation.Contains("Generic") ||
                                    actorImplementation.Contains("Live2D") ||
                                    actorImplementation.Contains("Spine")))
                {
                    var animator = (prefabAsset as GameObject)?.GetComponentInChildren<Animator>();
                    var controller = animator ? animator.runtimeAnimatorController as AnimatorController : null;
                    return controller
                        ? controller.parameters.Where(p => p.type == AnimatorControllerParameterType.Trigger).Select(p => p.name).ToArray()
                        : Array.Empty<string>();
                }
                #if SPRITE_DICING_AVAILABLE
                if (prefabAsset && actorImplementation.Contains("Diced"))
                {
                    return (prefabAsset as SpriteDicing.DicedSpriteAtlas)?.Sprites.Select(s => s.name).ToArray() ?? Array.Empty<string>();
                }
                #endif
                {
                    var multiplePrefix = $"{pathPrefix}/{actorId}/";
                    return allResources.Where(p => p.Contains(multiplePrefix)).Select(p => p.GetAfter(multiplePrefix)).ToArray();
                }
            }
        }

        /// <summary>
        /// Generates metadata for custom variables assigned in configuration menu.
        /// </summary>
        public static string[] GenerateVariablesMetadata ()
        {
            var config = Configuration.GetOrDefault<CustomVariablesConfiguration>();
            return config.PredefinedVariables.Select(p => p.Name).ToArray();
        }

        /// <summary>
        /// Generates metadata for custom expression functions (declared outside of Naninovel namespace).
        /// </summary>
        public static Function[] GenerateFunctionsMetadata ()
        {
            var customsFunctions = ExpressionFunctions.Resolve()
                .Where(fn => fn.Method.DeclaringType?.Namespace != typeof(ExpressionFunctions).Namespace);
            return GenerateFunctionsMetadata(customsFunctions);
        }

        /// <summary>
        /// Generates metadata for specified expression functions.
        /// </summary>
        public static Function[] GenerateFunctionsMetadata (IEnumerable<ExpressionFunction> functions)
        {
            return functions.Select(fn => new Function {
                Name = fn.Id,
                Documentation = CreateDocsMeta(fn.Summary, fn.Remarks, fn.Examples),
                Parameters = fn.Method.GetParameters().Select(GenerateParameterMetadata).ToArray()
            }).ToArray();

            FunctionParameter GenerateParameterMetadata (System.Reflection.ParameterInfo info)
            {
                return new FunctionParameter {
                    Name = info.Name,
                    Type = ResolveParameterType(info.ParameterType),
                    Context = GetContext(info),
                    Variadic = info.IsDefined(typeof(ParamArrayAttribute))
                };
            }

            Metadata.ValueType ResolveParameterType (Type valueType)
            {
                if (valueType.IsArray) valueType = valueType.GetElementType();
                if (valueType == typeof(string)) return Metadata.ValueType.String;
                if (valueType == typeof(bool)) return Metadata.ValueType.Boolean;
                if (valueType == typeof(int)) return Metadata.ValueType.Integer;
                return Metadata.ValueType.Decimal;
            }

            ValueContext GetContext (System.Reflection.ParameterInfo info)
            {
                var attr = info.GetCustomAttribute<ParameterContextAttribute>();
                if (attr is null) return null;
                return new ValueContext {
                    Type = attr.Type,
                    SubType = attr.SubType
                };
            }
        }

        private static Metadata.Parameter[] GenerateParametersMetadata (Type commandType, CommandLocalization locale, Func<FieldInfo, string> summaryResolver)
        {
            var result = new List<Metadata.Parameter>();
            foreach (var fieldInfo in GetParameterFields(commandType))
                if (!IsIgnored(fieldInfo))
                    result.Add(ExtractParameterMetadata(locale, fieldInfo, summaryResolver));
            return result.ToArray();

            bool IsIgnored (FieldInfo i) => IsIgnoredViaField(i) || IsIgnoredViaClass(i);
            bool IsIgnoredViaField (FieldInfo i) => i.GetCustomAttribute<IgnoreParameterAttribute>() != null;
            bool IsIgnoredViaClass (FieldInfo i) => i.ReflectedType?.GetCustomAttributes<IgnoreParameterAttribute>().Any(a => a.ParameterId == i.Name) ?? false;
        }

        private static FieldInfo[] GetParameterFields (Type commandType)
        {
            return commandType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .Where(f => f.FieldType.GetInterface(nameof(ICommandParameter)) != null).ToArray();
        }

        private static Metadata.Parameter ExtractParameterMetadata (CommandLocalization locale, FieldInfo field, Func<FieldInfo, string> summaryResolver)
        {
            var l10n = locale.Parameters?.FirstOrDefault(p => p.Id == field.Name);
            var nullableName = typeof(INullable<>).Name;
            var namedName = typeof(INamed<>).Name;
            var meta = new Metadata.Parameter {
                Id = field.Name,
                Alias = string.IsNullOrWhiteSpace(l10n?.Alias)
                    ? ReflectionUtils.GetAttributeValue<Command.ParameterAliasAttribute>(field, 0) as string
                    : l10n.Value.Alias,
                Required = ReflectionUtils.GetAttributeData<Command.RequiredParameterAttribute>(field) != null,
                Localizable = field.FieldType == typeof(LocalizableTextParameter),
                DefaultValue = ReflectionUtils.GetAttributeValue<Command.ParameterDefaultValueAttribute>(field, 0) as string,
                ValueContext = GetValueContext(field),
                Documentation = CreateDocsMeta(!string.IsNullOrWhiteSpace(l10n?.Summary) ? l10n.Value.Summary : summaryResolver(field), null, null)
            };
            meta.Nameless = meta.Alias == Command.NamelessParameterAlias;
            if (TryResolveValueType(field.FieldType, out var valueType))
                meta.ValueContainerType = ValueContainerType.Single;
            else if (GetInterface(nameof(IEnumerable)) != null) SetListValue();
            else SetNamedValue();
            meta.ValueType = valueType;
            return meta;

            Type GetInterface (string name) => field.FieldType.GetInterface(name);

            Type GetNullableType () => GetInterface(nullableName).GetGenericArguments()[0];

            void SetListValue ()
            {
                var elementType = GetNullableType().GetGenericArguments()[0];
                var namedElementType = elementType.BaseType?.GetGenericArguments()[0];
                if (namedElementType?.GetInterface(nameof(INamedValue)) != null)
                {
                    meta.ValueContainerType = ValueContainerType.NamedList;
                    var namedType = namedElementType.GetInterface(namedName).GetGenericArguments()[0];
                    TryResolveValueType(namedType, out valueType);
                }
                else
                {
                    meta.ValueContainerType = ValueContainerType.List;
                    TryResolveValueType(elementType, out valueType);
                }
            }

            void SetNamedValue ()
            {
                meta.ValueContainerType = ValueContainerType.Named;
                var namedType = GetNullableType().GetInterface(namedName).GetGenericArguments()[0];
                TryResolveValueType(namedType, out valueType);
            }
        }

        private static ValueContext[] GetValueContext (MemberInfo member)
        {
            var valueAttr = FindAttribute(false);
            if (valueAttr is null) return null;
            if (valueAttr is EndpointContextAttribute)
                return new[] {
                    new ValueContext { Type = ValueContextType.Endpoint, SubType = Constants.EndpointScript },
                    new ValueContext { Type = ValueContextType.Endpoint, SubType = Constants.EndpointLabel }
                };
            return FindAttribute(true) is ParameterContextAttribute namedValueAttr
                ? new[] { GetValue(valueAttr), GetValue(namedValueAttr) }
                : new[] { GetValue(valueAttr) };

            ValueContext GetValue (ParameterContextAttribute attr) =>
                new ValueContext { Type = attr.Type, SubType = attr.SubType };
            ParameterContextAttribute FindAttribute (bool namedValue) =>
                FindFieldLevelContext(namedValue) ?? FindClassLevelContext(namedValue);
            ParameterContextAttribute FindClassLevelContext (bool namedValue) =>
                member.ReflectedType?.GetCustomAttributes<ParameterContextAttribute>()
                    .Where(a => a.ParameterId == member.Name).FirstOrDefault(a => OfSingleOrNamed(a, namedValue));
            ParameterContextAttribute FindFieldLevelContext (bool namedValue) =>
                member.GetCustomAttributes<ParameterContextAttribute>().FirstOrDefault(a => OfSingleOrNamed(a, namedValue));
            bool OfSingleOrNamed (ParameterContextAttribute a, bool namedValue) => a.Index < 0 || a.Index == (namedValue ? 1 : 0);
        }

        private static bool TryResolveValueType (Type type, out Metadata.ValueType result)
        {
            var nullableName = typeof(INullable<>).Name;
            var valueTypeName = type.GetInterface(nullableName)?.GetGenericArguments()[0].Name;
            switch (valueTypeName)
            {
                case nameof(String):
                case nameof(NullableString):
                case nameof(LocalizableText):
                    result = Metadata.ValueType.String;
                    return true;
                case nameof(Int32):
                case nameof(NullableInteger):
                    result = Metadata.ValueType.Integer;
                    return true;
                case nameof(Single):
                case nameof(NullableFloat):
                    result = Metadata.ValueType.Decimal;
                    return true;
                case nameof(Boolean):
                case nameof(NullableBoolean):
                    result = Metadata.ValueType.Boolean;
                    return true;
            }
            result = default;
            return false;
        }

        private static CommandDocumentation ResolveCustomCommandDocs (Type type)
        {
            var summary = ReflectionUtils.GetAttributeValue<DocumentationAttribute>(type, 0) as string;
            var remarks = ReflectionUtils.GetAttributeValue<DocumentationAttribute>(type, 1) as string;
            var example = ReflectionUtils.GetAttributeValue<DocumentationAttribute>(type, 2) as string;
            return new CommandDocumentation(summary, remarks, example);
        }

        private static Nest ResolveNestMeta (Type commandType)
        {
            if (!typeof(Command.INestedHost).IsAssignableFrom(commandType)) return null;
            return new Nest { Required = commandType.GetCustomAttribute<RequireNestedAttribute>() != null };
        }

        private static Branch ResolveBranchMeta (Type commandType)
        {
            var branch = commandType.GetCustomAttribute<BranchAttribute>();
            if (branch is null) return null;
            return new Branch { Traits = branch.Traits, SwitchRoot = branch.SwitchRoot };
        }

        private static string ResolveCustomParameterDocs (FieldInfo field)
        {
            return ReflectionUtils.GetAttributeValue<DocumentationAttribute>(field, 0) as string;
        }

        private static Documentation CreateDocsMeta (string summary, string remarks, string examples)
        {
            if (string.IsNullOrWhiteSpace(summary) && string.IsNullOrWhiteSpace(remarks) && string.IsNullOrWhiteSpace(examples))
                return null;
            return new Documentation { Summary = summary, Remarks = remarks, Examples = examples };
        }

        private static Project MergeMetadata (Project meta1, Project meta2)
        {
            return new Project {
                Actors = meta1.Actors.Concat(meta2.Actors).ToArray(),
                Commands = meta1.Commands.Concat(meta2.Commands).ToArray(),
                Constants = meta1.Constants.Concat(meta2.Constants).ToArray(),
                Functions = meta1.Functions.Concat(meta2.Functions).ToArray(),
                Resources = meta1.Resources.Concat(meta2.Resources).ToArray(),
                Variables = meta1.Variables.Concat(meta2.Variables).ToArray(),
                Syntax = new Syntax(
                    commentLine: MergeString(meta1.Syntax.CommentLine, meta2.Syntax.CommentLine),
                    labelLine: MergeString(meta1.Syntax.LabelLine, meta2.Syntax.LabelLine),
                    commandLine: MergeString(meta1.Syntax.CommandLine, meta2.Syntax.CommandLine),
                    authorAssign: MergeString(meta1.Syntax.AuthorAssign, meta2.Syntax.AuthorAssign),
                    authorAppearance: MergeString(meta1.Syntax.AuthorAppearance, meta2.Syntax.AuthorAppearance),
                    expressionOpen: MergeString(meta1.Syntax.ExpressionOpen, meta2.Syntax.ExpressionOpen),
                    expressionClose: MergeString(meta1.Syntax.ExpressionClose, meta2.Syntax.ExpressionClose),
                    inlinedOpen: MergeString(meta1.Syntax.InlinedOpen, meta2.Syntax.InlinedOpen),
                    inlinedClose: MergeString(meta1.Syntax.InlinedClose, meta2.Syntax.InlinedClose),
                    parameterAssign: MergeString(meta1.Syntax.ParameterAssign, meta2.Syntax.ParameterAssign),
                    listDelimiter: MergeString(meta1.Syntax.ListDelimiter, meta2.Syntax.ListDelimiter),
                    namedDelimiter: MergeString(meta1.Syntax.NamedDelimiter, meta2.Syntax.NamedDelimiter),
                    textIdOpen: MergeString(meta1.Syntax.TextIdOpen, meta2.Syntax.TextIdOpen),
                    textIdClose: MergeString(meta1.Syntax.TextIdClose, meta2.Syntax.TextIdClose),
                    booleanFlag: MergeString(meta1.Syntax.BooleanFlag, meta2.Syntax.BooleanFlag),
                    @true: MergeString(meta1.Syntax.True, meta2.Syntax.True),
                    @false: MergeString(meta1.Syntax.False, meta2.Syntax.False)
                )
            };

            string MergeString (string s1, string s2) => !string.IsNullOrWhiteSpace(s2) ? s2 : s1;
        }
    }
}
