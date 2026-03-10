using System.Linq;
using Naninovel.Metadata;

namespace Naninovel
{
    public class DefaultMetadataProvider : IMetadataProvider
    {
        public Project GetMetadata (MetadataOptions options)
        {
            var meta = new Project();
            options.NotifyProgress("Processing commands...", 0);
            meta.Commands = MetadataGenerator.GenerateCommandsMetadata(options.Commands,
                options.GetCommandDocumentation, options.GetParameterDocumentation);
            options.NotifyProgress("Processing resources...", .25f);
            meta.Resources = MetadataGenerator.GenerateResourcesMetadata();
            options.NotifyProgress("Processing actors...", .50f);
            meta.Actors = MetadataGenerator.GenerateActorsMetadata();
            options.NotifyProgress("Processing variables...", .75f);
            meta.Variables = MetadataGenerator.GenerateVariablesMetadata();
            options.NotifyProgress("Processing functions...", .95f);
            meta.Functions = MetadataGenerator.GenerateFunctionsMetadata();
            options.NotifyProgress("Processing constants...", .99f);
            var customsFunctions = ExpressionFunctions.Resolve()
                .Where(fn => fn.Method.DeclaringType?.Namespace != typeof(ExpressionFunctions).Namespace);
            meta.Constants = MetadataGenerator.GenerateConstantsMetadata(options.Commands, customsFunctions);
            meta.Syntax = Compiler.Syntax;
            return meta;
        }
    }
}
