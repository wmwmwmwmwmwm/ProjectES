namespace Naninovel.Commands
{
    /// <summary>
    /// Prevents player from rolling back to the previous state snapshots.
    /// </summary>
    [IgnoreParameter(nameof(Wait))]
    public class PurgeRollback : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            Engine.GetService<IStateManager>()?.PurgeRollbackData();
            return UniTask.CompletedTask;
        }
    }
}
