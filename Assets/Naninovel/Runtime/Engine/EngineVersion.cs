using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Stores engine version and build number.
    /// </summary>
    public class EngineVersion : ScriptableObject
    {
        /// <summary>
        /// Version identifier of the engine release.
        /// </summary>
        public string Version => engineVersion;
        /// <summary>
        /// Date and time the release was built.
        /// </summary>
        public string Build => buildDate;
        /// <summary>
        /// Whether this is the final build in the release stream,
        /// ie the stream won't get new patches and is out of support.
        /// </summary>
        public bool Final => final;

        [SerializeField] private string engineVersion = string.Empty;
        [SerializeField] private bool final;
        [SerializeField, ReadOnly] private string buildDate = string.Empty;

        public static EngineVersion LoadFromResources ()
        {
            const string assetPath = nameof(EngineVersion);
            return Engine.LoadInternalResource<EngineVersion>(assetPath);
        }
    }
}
