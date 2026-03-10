using JetBrains.Annotations;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IScriptPlayer"/> and associated types.
    /// </summary>
    public static class ScriptPlayerExtensions
    {
        /// <summary>
        /// Starts playback of script with specified name and label.
        /// </summary>
        public static void Play (this IScriptPlayer player, string scriptName, [CanBeNull] string label)
        {
            var script = Engine.GetService<IScriptManager>().GetScript(scriptName);
            var list = new ScriptPlaylist(script);
            var index = string.IsNullOrEmpty(label) ? 0 : list.GetIndexByLine(script.GetLineIndexForLabel(label), 0);
            player.Play(list, index);
        }

        /// <summary>
        /// Starts playback of script with specified name at specified line and inline indexes.
        /// </summary>
        public static void Play (this IScriptPlayer player, string scriptName, int lineIndex, int inlineIndex)
        {
            var script = Engine.GetService<IScriptManager>().GetScript(scriptName);
            var list = new ScriptPlaylist(script);
            var index = list.GetIndexByLine(lineIndex, inlineIndex);
            player.Play(list, index);
        }

        /// <summary>
        /// Starts playback of script at specified playback spot.
        /// </summary>
        public static void Play (this IScriptPlayer player, PlaybackSpot spot)
        {
            var script = Engine.GetService<IScriptManager>().GetScript(spot.ScriptName);
            var list = new ScriptPlaylist(script);
            var index = list.IndexOf(spot);
            player.Play(list, index);
        }

        /// <summary>
        /// Starts (resumes) <see cref="PlayedScript"/> playback at specified line and inline indexes.
        /// </summary>
        /// <param name="startLineIndex">Line index to start playback from.</param>
        /// <param name="startInlineIndex">Command inline index to start playback from.</param>
        public static void PlayFromLine (this IScriptPlayer player, int startLineIndex, int startInlineIndex = 0)
        {
            if (player.Playlist is null || !player.PlayedScript) throw new Error("Failed to start or resume playback: player doesn't have played script.");
            var playbackIndex = player.Playlist.GetIndexByLine(startLineIndex, startInlineIndex);
            if (playbackIndex < 0) throw new Error($"Failed to start playback: `{player.PlayedScript}` script can't be played at line #{startLineIndex}.{startInlineIndex}.");
            player.Resume(playbackIndex);
        }

        /// <summary>
        /// Starts (resumes) <see cref="PlayedScript"/> playback at specified label.
        /// </summary>
        /// <param name="label">Name of a label within the script to start playback from.</param>
        public static void PlayFromLabel (this IScriptPlayer player, string label)
        {
            if (player.Playlist is null || !player.PlayedScript) throw new Error("Failed to start or resume playback: player doesn't have played script.");
            if (!player.PlayedScript.LabelExists(label)) throw new Error($"Failed navigating script playback to `{label}` label: label not found in `{player.PlayedScript.Name}` script.");
            player.PlayFromLine(player.PlayedScript.GetLineIndexForLabel(label));
        }

        /// <summary>
        /// Preloads the script's commands and starts playing at specified line and inline indexes.
        /// </summary>
        /// <param name="script">The script to play.</param>
        /// <param name="startLineIndex">Line index to start playback from.</param>
        /// <param name="startInlineIndex">Command inline index to start playback from.</param>
        public static async UniTask PreloadAndPlayAsync (this IScriptPlayer player, Script script, int startLineIndex = 0, int startInlineIndex = 0)
        {
            var loader = Engine.GetService<IScriptLoader>();
            var list = new ScriptPlaylist(script);
            var preloadIdx = list.GetIndexByLine(startLineIndex, startInlineIndex);
            if (preloadIdx < 0) throw new Error($"Failed to start playback: `{script.Name}` script can't be played at line #{startLineIndex}.{startInlineIndex}.");
            await loader.Load(list, preloadIdx);
            player.Play(list, ResolvePlaylistIndex());

            int ResolvePlaylistIndex ()
            {
                if (startLineIndex > 0 || startInlineIndex > 0)
                {
                    var startCommand = list.GetCommandAfterLine(startLineIndex, startInlineIndex);
                    if (startCommand is null) throw new Error($"Script player failed to start: no commands found in script `{script.Name}` at line #{startLineIndex}.{startInlineIndex}.");
                    return list.IndexOf(startCommand);
                }
                return 0;
            }
        }

        /// <summary>
        /// Preloads the script's commands and starts playing at the provided label; throws in case label is not found in the script.
        /// </summary>
        /// <param name="script">The script to play.</param>
        /// <param name="label">Name of a label within the script to start playback from.</param>
        public static UniTask PreloadAndPlayAsync (this IScriptPlayer player, Script script, string label)
        {
            if (!script.LabelExists(label)) throw new Error($"Failed navigating script playback to `{label}` label: label not found in `{script.Name}` script.");
            return player.PreloadAndPlayAsync(script, script.GetLineIndexForLabel(label));
        }

        /// <summary>
        /// Loads the provided script, preloads the script's commands and starts playing at the provided line and inline indexes.
        /// </summary>
        /// <remarks>Preload progress is reported by <see cref="IScriptPlayer.OnPreloadProgress"/> event.</remarks>
        /// <param name="scriptName">Name (resource path) of the script to load and play.</param>
        /// <param name="startLineIndex">Line index to start playback from.</param>
        /// <param name="startInlineIndex">Command inline index to start playback from.</param>
        public static UniTask PreloadAndPlayAsync (this IScriptPlayer player, string scriptName, int startLineIndex = 0, int startInlineIndex = 0)
        {
            if (!Engine.GetService<IScriptManager>().TryGetScript(scriptName, out var script))
                throw new Error($"Script player failed to start: script with name `{scriptName}` not found.");
            return player.PreloadAndPlayAsync(script, startLineIndex, startInlineIndex);
        }

        /// <summary>
        /// Loads the provided script, preloads the script's commands and starts playing at the provided label.
        /// </summary>
        /// <remarks>Preload progress is reported by <see cref="IScriptPlayer.OnPreloadProgress"/> event.</remarks>
        /// <param name="scriptName">Name (resource path) of the script to load and play.</param>
        /// <param name="label">Name of a label within the script to start playback from.</param>
        public static UniTask PreloadAndPlayAsync (this IScriptPlayer player, string scriptName, string label)
        {
            if (!Engine.GetService<IScriptManager>().TryGetScript(scriptName, out var script))
                throw new Error($"Script player failed to start: script with name `{scriptName}` not found.");
            return player.PreloadAndPlayAsync(script, script.GetLineIndexForLabel(label));
        }

        /// <summary>
        /// Plays specified script independently of the current playback status; returns when all commands are executed.
        /// Will as well preload and release the associated resources before/after playing.
        /// </summary>
        /// <remarks>
        /// Use to additively play transient (runtime-only, non-resource) script without interrupting normal script playback.
        /// Be aware, that transient scripts can't use any features associated with playback state, such as
        /// gosub or if/elseif commands, localizable text (it'll be parsed as plain text), etc.
        /// </remarks>
        /// <param name="playlist">The playlist to play.</param>
        public static async UniTask PlayTransient (this IScriptPlayer player, ScriptPlaylist playlist, AsyncToken token = default)
        {
            await playlist.LoadResourcesAsync();
            foreach (var command in playlist)
            {
                if (!command.ShouldExecute) continue;
                if (player.Configuration.ShouldWait(command))
                    try { await command.ExecuteAsync(token); }
                    catch (AsyncOperationCanceledException) { }
                else command.ExecuteAsync(token).Forget();
                token.ThrowIfCanceled();
            }
            playlist.ReleaseResources();
        }

        /// <param name="scriptName">Script name to distinguish the script in error logs.</param>
        /// <param name="scriptText">The script text to play.</param>
        /// <inheritdoc cref="PlayTransient(Naninovel.IScriptPlayer,ScriptPlaylist,Naninovel.AsyncToken)"/>
        public static UniTask PlayTransient (this IScriptPlayer player, string scriptName, string scriptText, AsyncToken token = default)
        {
            return PlayTransient(player, new ScriptPlaylist(Script.FromTransient(scriptName, scriptText)), token);
        }

        /// <summary>
        /// Checks whether currently played command has lower indentation level
        /// than next one, ie playback would enter nested block.
        /// </summary>
        public static bool IsEnteringNested (this IScriptPlayer player)
        {
            return player.Playlist.IsEnteringNestedAt(player.PlayedIndex);
        }
    }
}
