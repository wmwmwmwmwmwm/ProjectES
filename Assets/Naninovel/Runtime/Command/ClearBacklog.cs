namespace Naninovel.Commands
{
    /// <summary>
    /// Removes all the messages from [printer backlog](/guide/text-printers#printer-backlog).
    /// </summary>
    [IgnoreParameter(nameof(Wait))]
    public class ClearBacklog : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            Engine.GetService<IUIManager>()?.GetUI<UI.IBacklogUI>()?.Clear();
            return UniTask.CompletedTask;
        }
    }
}
