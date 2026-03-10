using System;

namespace Naninovel
{
    /// <summary>
    /// Handles pre-/loading and unloading resources associated with scenario scripts.
    /// </summary>
    public interface IScriptLoader : IEngineService
    {
        /// <summary>
        /// Event invoked when script load progress is changed, in 0.0 to 1.0 range.
        /// </summary>
        event Action<float> OnLoadProgress;

        /// <summary>
        /// Loads resources associated with specified script and unloads resources associated
        /// with the previously loaded scripts in accordance with <see cref="ResourcePolicy"/>.
        /// </summary>
        /// <param name="playlist">Script playlist to preload.</param>
        /// <param name="startIndex">Playlist command index in the loaded list to start loading from.</param>
        UniTask Load (ScriptPlaylist playlist, int startIndex = 0);
    }
}
