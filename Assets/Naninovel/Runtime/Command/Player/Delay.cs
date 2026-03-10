using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Delays execution of the nested commands for specified time interval.
    /// </summary>
    /// <remarks>
    /// Be aware, that the delayed execution won't happen if game gets saved/loaded
    /// or rolled-back. It's fine to use delayed execution for "cosmetic" events,
    /// such as one-shot visual or audio effects, but don't delay commands, which
    /// could affect persistent game state, as this could lead to undefined behaviour.
    /// </remarks>
    [RequireNested, Branch(BranchTraits.Nest | BranchTraits.Return), IgnoreParameter(nameof(Wait))]
    public class Delay : Command, Command.INestedHost
    {
        /// <summary>
        /// Delay time, in seconds.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public DecimalParameter Seconds;

        public virtual int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            // Nested commands are played in transient execution context after the delay.
            if (playlist.IsEnteringNestedAt(playedIndex))
                return playlist.SkipNestedAt(playedIndex, Indent);
            throw new Error("Nested commands of @delay command should never be executed under main context. " +
                            "This could happen if you navigate to labels nested under @delay, which is not supported.");
        }

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var delayedList = BuildDelayedList();
            WaitDelayAsync(Seconds, asyncToken)
                .ContinueWith(() => ExecuteDelayedAsync(delayedList, asyncToken)).Forget();
            return UniTask.CompletedTask;
        }

        protected virtual ScriptPlaylist BuildDelayedList ()
        {
            var player = Engine.GetService<IScriptPlayer>();
            var start = player.PlayedIndex + 1;
            var count = player.Playlist.GetNestedExitIndexAt(player.PlayedIndex, Indent) - start + 1;
            var commands = player.Playlist.GetRange(start, count);
            return new ScriptPlaylist($"Delayed transient for {PlaybackSpot}", commands);
        }

        protected virtual async UniTask WaitDelayAsync (float waitTime, AsyncToken token)
        {
            var player = Engine.GetService<IScriptPlayer>();
            if (player.SkipActive) return;

            var startTime = Engine.Time.Time;
            while (Application.isPlaying && !player.Synchronizing && token.EnsureNotCanceledOrCompleted())
            {
                await AsyncUtils.WaitEndOfFrameAsync(token);
                var waitedEnough = Engine.Time.Time - startTime >= waitTime;
                if (waitedEnough) break;
            }
        }

        protected virtual async UniTask ExecuteDelayedAsync (ScriptPlaylist delayedList, AsyncToken token)
        {
            if (!token.EnsureNotCanceledOrCompleted()) return;
            var player = Engine.GetService<IScriptPlayer>();
            await player.PlayTransient(delayedList, token);
        }
    }
}
