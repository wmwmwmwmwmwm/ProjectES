using Naninovel.Metadata;

namespace Naninovel.Commands
{
    /// <summary>
    /// Allows grouping commands inside nested block.
    /// </summary>
    [RequireNested, Branch(BranchTraits.Nest | BranchTraits.Return), IgnoreParameter(nameof(Wait))]
    public class Group : Command, Command.INestedHost
    {
        public virtual int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (playlist.IsEnteringNestedAt(playedIndex))
                return ShouldExecute ? playedIndex + 1 : playlist.SkipNestedAt(playedIndex, Indent);
            if (playlist.IsExitingNestedAt(playedIndex, Indent))
                return playlist.ExitNestedAt(playedIndex, Indent);
            return playedIndex + 1;
        }

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            return UniTask.CompletedTask;
        }
    }
}
