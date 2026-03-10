using System;

namespace Naninovel
{
    /// <summary>
    /// Used by Naninovel editor tools to extract documentation from custom types,
    /// such as commands and expression functions.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class DocumentationAttribute : Attribute
    {
        public readonly string Summary;
        public readonly string Remarks;
        public readonly string Examples;

        /// <param name="summary">Description of the type.</param>
        /// <param name="remarks">Additional info about the type.</param>
        /// <param name="examples">Examples on using the type.</param>
        public DocumentationAttribute (string summary, string remarks = null, string examples = null)
        {
            Summary = summary;
            Remarks = remarks;
            Examples = examples;
        }
    }
}
