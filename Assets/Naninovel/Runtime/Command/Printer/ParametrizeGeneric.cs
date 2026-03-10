namespace Naninovel.Commands
{
    /// <summary>
    /// Used to apply [generic parameters](/guide/naninovel-scripts#generic-parameters) via `[< ...]` syntax.
    /// </summary>
    [CommandAlias("<")]
    public class ParametrizeGeneric : Command, Command.ILocalizable
    {
        /// <summary>
        /// ID of the printer actor to use.
        /// </summary>
        [ParameterAlias("printer"), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        /// <summary>
        /// ID of the actor, which should be associated with the printed message.
        /// Specify `*` or use `,` to delimit multiple actor IDs to make all/selected characters authors of the text;
        /// useful when coupled with `as` parameter to represent multiple characters speaking at the same time.
        /// </summary>
        [ParameterAlias("author"), ActorContext(CharactersConfiguration.DefaultPathPrefix)]
        public StringParameter AuthorId;
        /// <summary>
        /// When specified, will use the label instead of author ID (or associated display name)
        /// to represent author name in the text printer while printing the message. Useful to
        /// override default name for a few messages or represent multiple authors speaking at the same time
        /// without triggering author-specific behaviour of the text printer, such as message color or avatar.
        /// </summary>
        [ParameterAlias("as")]
        public LocalizableTextParameter AuthorLabel;
        /// <summary>
        /// Text reveal speed multiplier; should be positive or zero. Setting to one will yield the default speed.
        /// </summary>
        [ParameterAlias("speed")]
        public DecimalParameter RevealSpeed;
        /// <summary>
        /// Whether to not wait for user input after finishing the printing task.
        /// </summary>
        [ParameterAlias("skip")]
        public BooleanParameter SkipWaitingInput;
        /// <summary>
        /// Whether to not reset printed text before printing this line effectively appending the text.
        /// </summary>
        [ParameterAlias("join")]
        public BooleanParameter Join;

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default) => UniTask.CompletedTask;
    }
}
