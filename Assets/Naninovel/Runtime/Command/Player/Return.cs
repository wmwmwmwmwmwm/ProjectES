namespace Naninovel.Commands
{
    /// <summary>
    /// Attempts to navigate naninovel script playback to a command after the last used [@gosub].
    /// See [@gosub] command summary for more info and usage examples.
    /// </summary>
    public class Return : Command, Command.IForceWait
    {
        /// <summary>
        /// When specified, will reset the engine services state before returning to the initial script 
        /// from which the gosub was entered (in case it's not the currently played script).
        /// Specify `*` to reset all the services, or specify service names to exclude from reset.
        /// By default, the state does not reset.
        /// </summary>
        [ParameterAlias("reset")]
        public StringListParameter ResetState;

        protected IScriptPlayer Player => Engine.GetService<IScriptPlayer>();

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            if (Player.GosubReturnSpots.Count == 0 || string.IsNullOrWhiteSpace(Player.GosubReturnSpots.Peek().ScriptName))
            {
                Warn("Failed to return to the last gosub: state data is missing or invalid.");
                return;
            }
            await Reset();
            Navigate();
        }

        protected virtual async UniTask Reset ()
        {
            var stateManager = Engine.GetService<IStateManager>();
            if (Assigned(ResetState) && ResetState.Length == 1 && ResetState[0] == "*") await stateManager.ResetStateAsync();
            else if (Assigned(ResetState) && ResetState.Length > 0) await stateManager.ResetStateAsync(ResetState);
        }

        protected virtual void Navigate ()
        {
            var spot = Player.GosubReturnSpots.Pop();
            if (Player.PlayedScript && Player.PlayedScript.Name.EqualsFastIgnoreCase(spot.ScriptName))
            {
                Player.PlayFromLine(spot.LineIndex);
                return;
            }
            Player.Play(spot.ScriptName, spot.LineIndex, spot.InlineIndex);
        }
    }
}
