using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Parsing;

namespace Naninovel
{
    /// <summary>
    /// Provides access to NaniScript compiler APIs.
    /// </summary>
    public static class Compiler
    {
        /// <inheritdoc cref="Parsing.ISyntax"/>
        public static Syntax Syntax { get; private set; }
        /// <summary>
        /// Prefix to identify global script variables.
        /// </summary>
        public static string GlobalVariablePrefix { get; private set; }
        /// <summary>
        /// Prefix to identify managed text script variables.
        /// </summary>
        public static string TextVariablePrefix { get; private set; }
        /// <inheritdoc cref="Parsing.NamedValueParser"/>
        public static NamedValueParser NamedValueParser { get; private set; }
        /// <inheritdoc cref="Parsing.ListValueParser"/>
        public static ListValueParser ListValueParser { get; private set; }
        /// <inheritdoc cref="Parsing.ScriptSerializer"/>
        public static ScriptSerializer ScriptSerializer { get; private set; }
        /// <inheritdoc cref="Naninovel.ScriptAssetParser"/>
        public static IScriptParser ScriptAssetParser { get; private set; }
        /// <inheritdoc cref="Naninovel.ScriptAssetSerializer"/>
        public static ScriptAssetSerializer ScriptAssetSerializer { get; private set; }
        /// <summary>
        /// Locale-specific aliases for commands and their parameters, mapped by implementation type name.
        /// </summary>
        public static Dictionary<string, CommandLocalization> Commands { get; private set; }
        /// <summary>
        /// Locale-specific aliases for expression functions, mapped by associated C# method name.
        /// </summary>
        public static Dictionary<string, FunctionLocalization> Functions { get; private set; }
        /// <summary>
        /// Locale-specific aliases for constants baked by C# enums, mapped by associated C# enum type name.
        /// </summary>
        public static Dictionary<string, ConstantLocalization> Constants { get; private set; }

        private static ScriptParser scriptParser;
        private static IErrorHandler errors;

        public static List<IScriptLine> ParseText (string scriptText, IErrorHandler errors = null)
        {
            Compiler.errors = errors;
            return scriptParser.ParseText(scriptText);
        }

        public static IScriptLine ParseLine (string lineText, IErrorHandler errors = null)
        {
            Compiler.errors = errors;
            return scriptParser.ParseLine(lineText);
        }

        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        #else
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        #endif
        private static void Initialize ()
        {
            var cfg = Configuration.GetOrDefault<ScriptsConfiguration>();
            Syntax = cfg.CompilerLocalization.GetSyntax();
            GlobalVariablePrefix = cfg.CompilerLocalization.GlobalVariablePrefix;
            TextVariablePrefix = cfg.CompilerLocalization.TextVariablePrefix;
            Commands = cfg.CompilerLocalization.Commands.ToDictionary(kv => kv.Id);
            Functions = cfg.CompilerLocalization.Functions.ToDictionary(kv => kv.MethodName);
            Constants = cfg.CompilerLocalization.Constants.ToDictionary(kv => kv.TypeName);
            scriptParser = new ScriptParser(new Parsing.ParseOptions {
                Syntax = Syntax,
                Handlers = new ParseHandlers { ErrorHandler = errors }
            });
            ScriptAssetParser = CreateScriptParser(cfg.ScriptParser);
            ScriptSerializer = new ScriptSerializer(Syntax);
            NamedValueParser = new NamedValueParser(Syntax);
            ListValueParser = new ListValueParser(Syntax);
            ScriptAssetSerializer = new ScriptAssetSerializer(Syntax);
        }

        private static IScriptParser CreateScriptParser (string typeName)
        {
            var type = Type.GetType(typeName);
            if (type is null) throw new Error($"Failed to create script parser from '{typeName}': Failed to resolve type.");
            var parser = Activator.CreateInstance(type) as IScriptParser;
            if (parser == null) throw new Error($"Failed to create script parser from '{typeName}': Type doesn't implement '{nameof(IScriptParser)}' interface.");
            return parser;
        }
    }
}
