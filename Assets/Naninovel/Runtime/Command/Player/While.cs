using Naninovel.Metadata;

namespace Naninovel.Commands
{
    /// <summary>
    /// Executes nested lines in a loop, as long as specified conditional expression resolves to `true`.
    /// </summary>
    [RequireNested, Branch(BranchTraits.Nest | BranchTraits.Return), IgnoreParameter(nameof(ConditionalExpression)), IgnoreParameter(nameof(Wait))]
    public class While : Command, Command.INestedHost
    {
        /// <summary>
        /// A [script expression](/guide/script-expressions), which should return a boolean value
        /// determining whether the associated nested block should continue executing in loop.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, ConditionContext]
        public StringParameter Expression;

        public override bool ShouldExecute => true;

        public virtual int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (playlist.IsEnteringNestedAt(playedIndex))
                return ExpressionEvaluator.Evaluate<bool>(Expression, Err)
                    ? playedIndex + 1
                    : playlist.SkipNestedAt(playedIndex, Indent);
            if (playlist.IsExitingNestedAt(playedIndex, Indent))
                return playlist.IndexOf(this);
            return playedIndex + 1;
        }

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            if (Assigned(ConditionalExpression))
                Warn("Parameter 'if' in '@while' command is ignored; use nameless parameter for the condition instead.");
            return UniTask.CompletedTask;
        }
    }
}
