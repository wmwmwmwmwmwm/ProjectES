using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Executes one of the nested commands, picked randomly.
    /// </summary>
    [CommandAlias("random"), RequireNested, Branch(BranchTraits.Nest | BranchTraits.Return), IgnoreParameter(nameof(Wait))]
    public class PickRandom : Command, Command.INestedHost
    {
        /// <summary>
        /// Customized probability for the nested commands, in 0.0 to 1.0 range.
        /// By default all the commands have equal probability of being picked.
        /// </summary>
        public DecimalListParameter Weight;

        public virtual int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (playlist.IsEnteringNestedAt(playedIndex))
                return PickRandomNested(playlist, playedIndex);
            var exitIndex = playlist.GetNestedExitIndexAt(playedIndex, Indent);
            return playlist.ExitNestedAt(exitIndex, Indent);
        }

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            return UniTask.CompletedTask;
        }

        protected virtual int PickRandomNested (ScriptPlaylist playlist, int hostIndex)
        {
            var maxSeed = -1f;
            var maxIndex = -1;
            var weightIndex = -1;
            for (int i = hostIndex + 1; i < playlist.Count; i++)
            {
                if (playlist[i].Indent == Indent + 1)
                {
                    var seed = Random.value * (Weight?.ElementAtOrNull(++weightIndex) ?? 1f);
                    if (seed > maxSeed && playlist[i].ShouldExecute)
                    {
                        maxSeed = seed;
                        maxIndex = i;
                    }
                }
                if (playlist.IsExitingNestedAt(i, Indent)) break;
            }
            return maxIndex == -1 ? playlist.SkipNestedAt(hostIndex + 1, Indent) : maxIndex;
        }
    }
}
