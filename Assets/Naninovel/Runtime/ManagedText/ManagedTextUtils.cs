using System.Reflection;
using Naninovel.ManagedText;

namespace Naninovel
{
    public static class ManagedTextUtils
    {
        public const BindingFlags ManagedFieldBindings = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly InlineManagedTextParser inlineParser = new InlineManagedTextParser();
        private static readonly MultilineManagedTextParser multilineParser = new MultilineManagedTextParser();

        /// <summary>
        /// Parses specified managed text document text.
        /// </summary>
        /// <param name="text">The document text to parse.</param>
        /// <param name="category">Specify to resolve document format (inline or multiline).</param>
        /// <param name="name">When specified, will include the name to parsing exception messages.</param>
        public static ManagedTextDocument Parse (string text, string category = null, string name = null)
        {
            var multi = string.IsNullOrEmpty(category)
                ? ManagedTextDetector.IsMultiline(text)
                : Configuration.GetOrDefault<ManagedTextConfiguration>().IsMultilineCategory(category);
            return multi ? multilineParser.Parse(text) : ParseInline(text, name);
        }

        /// <summary>
        /// Serializes specified managed text document into text string.
        /// </summary>
        /// <param name="document">The document to serialize into text string.</param>
        /// <param name="category">Specify to resolve document format (inline or multiline).</param>
        /// <param name="spacing">Number of line breaks to insert between records.</param>
        public static string Serialize (ManagedTextDocument document, string category = null, int spacing = 1)
        {
            var multi = Configuration.GetOrDefault<ManagedTextConfiguration>().IsMultilineCategory(category);
            return multi
                ? new MultilineManagedTextSerializer(spacing).Serialize(document)
                : new InlineManagedTextSerializer(spacing).Serialize(document);
        }

        private static ManagedTextDocument ParseInline (string text, string name = null)
        {
            try { return inlineParser.Parse(text); }
            catch (InlineManagedTextParser.SyntaxError e) { throw new Error($"Failed to parse '{name}' managed text document: {e.Message}"); }
        }
    }
}
