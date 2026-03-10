using System.Text;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Adds a [choice](/guide/choices) option to a choice handler with the specified ID (or default one).
    /// </summary>
    /// <remarks>
    /// When `goto`, `gosub` and `do` parameters are not specified, will continue script execution from the next script line.
    /// </remarks>
    [CommandAlias("choice"), Branch(BranchTraits.Interactive | BranchTraits.Nest | BranchTraits.Return | BranchTraits.Endpoint)]
    public class AddChoice : Command, Command.ILocalizable, Command.IPreloadable, Command.INestedHost
    {
        /// <summary>
        /// Text to show for the choice.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public LocalizableTextParameter ChoiceSummary;
        /// <summary>
        /// Whether the choice should be disabled or otherwise not accessible for player to pick;
        /// see [choice docs](/guide/choices#locked-choice) for more info. Disabled by default.
        /// </summary>
        [ParameterDefaultValue("false")]
        public BooleanParameter Lock = false;
        /// <summary>
        /// Path (relative to a `Resources` folder) to a [button prefab](/guide/choices#choice-button) representing the choice. 
        /// The prefab should have a `ChoiceHandlerButton` component attached to the root object.
        /// Will use a default button when not provided.
        /// </summary>
        [ParameterAlias("button")]
        public StringParameter ButtonPath;
        /// <summary>
        /// Local position of the choice button inside the choice handler (if supported by the handler implementation).
        /// </summary>
        [ParameterAlias("pos"), VectorContext("X,Y")]
        public DecimalListParameter ButtonPosition;
        /// <summary>
        /// ID of the choice handler to add choice for. Will use a default handler if not provided.
        /// </summary>
        [ParameterAlias("handler"), ActorContext(ChoiceHandlersConfiguration.DefaultPathPrefix)]
        public StringParameter HandlerId;
        /// <summary>
        /// Path to go when the choice is selected by user;
        /// see [@goto] command for the path format.
        /// </summary>
        [ParameterAlias("goto"), EndpointContext]
        public NamedStringParameter GotoPath;
        /// <summary>
        /// Path to a subroutine to go when the choice is selected by user;
        /// see [@gosub] command for the path format. When `goto` is assigned this parameter will be ignored.
        /// </summary>
        [ParameterAlias("gosub"), EndpointContext]
        public NamedStringParameter GosubPath;
        /// <summary>
        /// Set expression to execute when the choice is selected by user; 
        /// see [@set] command for syntax reference.
        /// </summary>
        [ParameterAlias("set"), ExpressionContext]
        public StringParameter SetExpression;
        /// <summary>
        /// Whether to automatically continue playing script from the next line, 
        /// when neither `goto` nor `gosub` parameters are specified. 
        /// Has no effect in case the script is already playing when the choice is processed.
        /// </summary>
        [ParameterAlias("play"), ParameterDefaultValue("true")]
        public BooleanParameter AutoPlay = true;
        /// <summary>
        /// Whether to also show choice handler the choice is added for;
        /// enabled by default.
        /// </summary>
        [ParameterAlias("show"), ParameterDefaultValue("true")]
        public BooleanParameter ShowHandler = true;
        /// <summary>
        /// Duration (in seconds) of the fade-in (reveal) animation.
        /// </summary>
        [ParameterAlias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;

        protected IChoiceHandlerManager Handlers => Engine.GetService<IChoiceHandlerManager>();

        public virtual async UniTask PreloadResourcesAsync ()
        {
            if (Assigned(HandlerId) && !HandlerId.DynamicValue)
            {
                var handlerId = Assigned(HandlerId) ? HandlerId.Value : Handlers.Configuration.DefaultHandlerId;
                await Handlers.GetOrAddActorAsync(handlerId);
            }

            if (Assigned(ButtonPath) && !ButtonPath.DynamicValue)
                await Handlers.ChoiceButtonLoader.LoadAndHoldAsync(ButtonPath, this);
        }

        public virtual void ReleasePreloadedResources ()
        {
            if (Assigned(ButtonPath) && !ButtonPath.DynamicValue)
                Handlers.ChoiceButtonLoader.Release(ButtonPath, this);
        }

        public virtual int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (playlist.IsEnteringNestedAt(playedIndex))
                // Always skip nested callback; it's executed when (if) the choice is picked by the player.
                return playlist.SkipNestedAt(playedIndex, Indent);

            if (!playlist.IsExitingNestedAt(playedIndex, Indent))
                return playedIndex + 1;

            // Exiting the block: navigate to the spot which was assigned to continue playback when choice was picked.
            var continueAt = Handlers.PopPickedChoice(PlaybackSpot);
            if (!continueAt.Valid)
                throw new Error(Engine.FormatMessage("Choice callback has nowhere to return. Make sure playable line exists after the nested block.", PlaybackSpot));
            if (continueAt.ScriptName != playlist.ScriptName)
                throw new Error(Engine.FormatMessage("Choice callback from another script is not supported.", PlaybackSpot));
            return playlist.IndexOf(continueAt);
        }

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var handler = await GetOrAddHandlerAsync(asyncToken);

            if (!handler.Visible && ShowHandler)
                ShowHandlerAsync(handler, asyncToken).Forget();

            var choice = CreateChoice();
            handler.AddChoice(choice);
        }

        protected virtual async UniTask<IChoiceHandlerActor> GetOrAddHandlerAsync (AsyncToken token)
        {
            var handlerId = Assigned(HandlerId) ? HandlerId.Value : Handlers.Configuration.DefaultHandlerId;
            var handler = await Handlers.GetOrAddActorAsync(handlerId);
            token.ThrowIfCanceled();
            return handler;
        }

        protected virtual UniTask ShowHandlerAsync (IChoiceHandlerActor handler, AsyncToken token)
        {
            var duration = Assigned(Duration) ? Duration.Value : Handlers.Configuration.DefaultDuration;
            return handler.ChangeVisibilityAsync(true, duration, asyncToken: token);
        }

        protected virtual ChoiceState CreateChoice ()
        {
            var nested = Engine.GetService<IScriptPlayer>().IsEnteringNested();
            var builder = new StringBuilder();

            if (nested)
            {
                if (Assigned(GotoPath) || Assigned(GosubPath) || Assigned(SetExpression) || !AutoPlay)
                    Warn("Using goto, gosub, set and play parameters with nested commands in '@choice' is not supported. Parameters will be ignored.");
            }
            else
            {
                if (Assigned(SetExpression))
                    builder.AppendLine($"{Compiler.Syntax.CommandLine}{nameof(SetCustomVariable)} {SetExpression}");
                if (Assigned(GotoPath))
                    builder.AppendLine($"{Compiler.Syntax.CommandLine}{nameof(Goto)} {GotoPath.Name ?? string.Empty}{(GotoPath.NamedValue.HasValue ? $".{GotoPath.NamedValue.Value}" : string.Empty)}");
                else if (Assigned(GosubPath))
                    builder.AppendLine($"{Compiler.Syntax.CommandLine}{nameof(Gosub)} {GosubPath.Name ?? string.Empty}{(GosubPath.NamedValue.HasValue ? $".{GosubPath.NamedValue.Value}" : string.Empty)}");
            }

            var onSelectScript = builder.ToString().TrimFull();
            var buttonPos = Assigned(ButtonPosition) ? (Vector2?)ArrayUtils.ToVector2(ButtonPosition) : null;
            var autoPlay = AutoPlay && !Assigned(GotoPath) && !Assigned(GosubPath);

            return new ChoiceState(PlaybackSpot, nested, ChoiceSummary, Lock, ButtonPath, buttonPos, onSelectScript, autoPlay);
        }
    }
}
