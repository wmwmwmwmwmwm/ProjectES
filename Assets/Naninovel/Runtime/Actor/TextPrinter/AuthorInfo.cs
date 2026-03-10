using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Information about an author of a printed message.
    /// </summary>
    [System.Serializable]
    public struct AuthorInfo
    {
        /// <summary>
        /// Actor ID of the author.
        /// </summary>
        public string Id => id;
        /// <summary>
        /// Custom name label of the author, if any.
        /// </summary>
        public LocalizableText Label => label;

        [SerializeField] private string id;
        [SerializeField] private LocalizableText label;

        public AuthorInfo (string id, LocalizableText label = default)
        {
            this.id = id;
            this.label = label;
        }
    }
}
