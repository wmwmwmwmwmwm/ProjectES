using System;

namespace Naninovel
{
    /// <summary>
    /// Arguments associated with the print text events invoked by <see cref="ITextPrinterManager"/>. 
    /// </summary>
    public class PrintTextArgs : EventArgs
    {
        /// <summary>
        /// Printer actor which is printing the text.
        /// </summary>
        public readonly ITextPrinterActor Printer;
        /// <summary>
        /// Text of the message to print.
        /// </summary>
        public readonly LocalizableText Text;
        /// <summary>
        /// ID of a character actor to which the printed text belongs (if any).
        /// </summary>
        public readonly AuthorInfo Author;
        /// <summary>
        /// Text print speed (base reveal speed modifier).
        /// </summary>
        public readonly float Speed;

        public PrintTextArgs (ITextPrinterActor printer, LocalizableText text, AuthorInfo author, float speed)
        {
            Printer = printer;
            Text = text;
            Author = author;
            Speed = speed;
        }
    }
}
