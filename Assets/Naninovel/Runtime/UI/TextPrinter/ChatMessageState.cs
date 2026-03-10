using System;
using UnityEngine;

namespace Naninovel
{
    [Serializable]
    public struct ChatMessageState : IEquatable<ChatMessageState>
    {
        public LocalizableText PrintedText => printedText;
        public string AuthorId => authorId;

        [SerializeField] private LocalizableText printedText;
        [SerializeField] private string authorId;

        public ChatMessageState (LocalizableText printedText, string authorId)
        {
            this.printedText = printedText;
            this.authorId = authorId;
        }

        public bool Equals (ChatMessageState other)
        {
            return printedText.Equals(other.printedText) && authorId == other.authorId;
        }

        public override bool Equals (object obj)
        {
            return obj is ChatMessageState other && Equals(other);
        }

        public override int GetHashCode ()
        {
            unchecked { return (printedText.GetHashCode() * 397) ^ (authorId != null ? authorId.GetHashCode() : 0); }
        }

        public static bool operator == (ChatMessageState left, ChatMessageState right)
        {
            return left.Equals(right);
        }

        public static bool operator != (ChatMessageState left, ChatMessageState right)
        {
            return !left.Equals(right);
        }
    }
}
