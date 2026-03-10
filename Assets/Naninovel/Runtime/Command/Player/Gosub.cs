using Naninovel.Metadata;

namespace Naninovel.Commands
{
    /// <summary>
    /// Navigates naninovel script playback to the provided path and saves that path to global state; 
    /// [@return] commands use this info to redirect to command after the last invoked gosub command. 
    /// </summary>
    /// <remarks>
    /// While this command can be used as a function (subroutine) to invoke a common set of script lines,
    /// remember that NaniScript is a scenario scripting DSL and is not suited for general programming.
    /// It's strongly recommended to use [custom commands](/guide/custom-commands) instead.
    /// </remarks>
    [Branch(BranchTraits.Endpoint | BranchTraits.Return), IgnoreParameter(nameof(Wait))]
    public class Gosub : Command, Command.IForceWait
    {
        /// <summary>
        /// Path to navigate into in the following format: `ScriptName.LabelName`.
        /// When label name is omitted, will play provided script from the start.
        /// When script name is omitted, will attempt to find a label in the currently played script.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, EndpointContext]
        public NamedStringParameter Path;
        /// <summary>
        /// When specified, will reset the engine services state before loading a script (in case the path is leading to another script).
        /// Specify `*` to reset all the services, or specify service names to exclude from reset.
        /// By default, the state does not reset.
        /// </summary>
        [ParameterAlias("reset")]
        public StringListParameter ResetState;

        protected IScriptPlayer Player => Engine.GetService<IScriptPlayer>();
        protected IScriptManager Scripts => Engine.GetService<IScriptManager>();

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            PushReturnSpot();
            if (!TryGetScriptNameAndLabel(out var scriptName, out var label)) return UniTask.CompletedTask;
            if (ShouldNavigatePlayedScript(scriptName)) NavigatePlayedScript(label);
            else Player.Play(scriptName, label);
            return UniTask.CompletedTask;
        }

        protected virtual void PushReturnSpot ()
        {
            var returnIndex = Player.Playlist.MoveAt(Player.PlayedIndex);
            var returnCommand = Player.Playlist[returnIndex];
            Player.GosubReturnSpots.Push(returnCommand.PlaybackSpot);
        }

        protected virtual bool TryGetScriptNameAndLabel (out string scriptName, out string label)
        {
            scriptName = Path.Name;
            label = Path.Value.Value;
            var valid = !string.IsNullOrWhiteSpace(scriptName) || Player.PlayedScript;
            if (!valid) Err("Failed to execute '@gosub' command: script name is not specified and no script is currently played.");
            return valid;
        }

        protected virtual bool ShouldNavigatePlayedScript (string scriptName)
        {
            return string.IsNullOrWhiteSpace(scriptName) ||
                   Player.PlayedScript && scriptName.EqualsFastIgnoreCase(Player.PlayedScript.Name);
        }

        protected virtual void NavigatePlayedScript (string label)
        {
            if (string.IsNullOrEmpty(label)) Player.Resume();
            else if (Player.PlayedScript.LabelExists(label)) Player.PlayFromLabel(label);
            else Err($"Failed navigating script playback to '{label}' label: label not found in '{Player.PlayedScript.Name}' script.");
        }

        protected virtual async UniTask Reset ()
        {
            var stateManager = Engine.GetService<IStateManager>();
            if (Assigned(ResetState) && ResetState.Length == 1 && ResetState[0] == "*") await stateManager.ResetStateAsync();
            else if (Assigned(ResetState) && ResetState.Length > 0) await stateManager.ResetStateAsync(ResetState);
        }
    }
}
