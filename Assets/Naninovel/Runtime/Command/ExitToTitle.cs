namespace Naninovel.Commands
{
    /// <summary>
    /// Resets engine state and shows `ITitleUI` UI (main menu).
    /// </summary>
    [CommandAlias("title")]
    public class ExitToTitle : Command, Command.IForceWait
    {
        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var gameState = Engine.GetService<IStateManager>();
            var uiManager = Engine.GetService<IUIManager>();

            using (await LoadingScreen.ShowAsync())
                await gameState.ResetStateAsync();
            // Don't check for the cancellation, as it's always cancelled after state reset.

            uiManager.GetUI<UI.ITitleUI>()?.Show();
        }
    }
}
