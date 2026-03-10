using System.Collections.Generic;
using TMPro;

namespace Naninovel
{
    /// <summary>
    /// Allows accessing localization resources authored externally by players for the built game.
    /// </summary>
    public interface ICommunityLocalization : IEngineService
    {
        /// <summary>
        /// Whether the community localization is currently in effect.
        /// </summary>
        bool Active { get; }
        /// <summary>
        /// Author of the active localization.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Loads localized script and managed text documents.
        /// </summary>
        UniTask<IReadOnlyList<(string Text, string Category)>> LoadLocalizedDocumentsAsync ();
        /// <summary>
        /// Loads font asset based on font face provided by the community localization author.
        /// </summary>
        UniTask<TMP_FontAsset> LoadFontAsync ();
    }
}
