using Naninovel.Metadata;

namespace Naninovel.Commands
{
    /// <summary>
    /// Holds script execution until all the nested async commands finished execution.
    /// Useful for grouping multiple async commands to wait until they all finish
    /// before proceeding with the script playback.
    /// </summary>
    /// <remarks>
    /// The nested block is expected to always finish; don't nest any commands that could
    /// navigate outside the nested block, as this may cause undefined behaviour.
    /// </remarks>
    [RequireNested, Branch(BranchTraits.Nest | BranchTraits.Return), IgnoreParameter(nameof(Wait))]
    public class Await : Command, Command.INestedHost, Command.IForceWait
    {
        private bool initial;

        public virtual int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (playlist.IsEnteringNestedAt(playedIndex))
                return initial
                    ? playedIndex + 1
                    : playlist.SkipNestedAt(playedIndex, Indent);

            if (playlist.IsExitingNestedAt(playedIndex, Indent))
                return initial
                    ? playlist.IndexOf(this)
                    : playlist.ExitNestedAt(playedIndex, Indent);

            return playedIndex + 1;
        }

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var player = Engine.GetService<IScriptPlayer>();

            if (!initial)
            {
                initial = true;
                return;
            }

            try
            {
                while (player.ExecutingCommands.Count > 1 && asyncToken.EnsureNotCanceledOrCompleted())
                    await AsyncUtils.WaitEndOfFrameAsync(asyncToken);
            }
            finally
            {
                initial = false;
                if (!asyncToken.Canceled && asyncToken.Completed)
                    player.SynchronizeAndDoAsync(() => UniTask.CompletedTask).Forget();
            }
        }
    }
}
