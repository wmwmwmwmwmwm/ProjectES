namespace Naninovel.Commands
{
    /// <summary>
    /// Stops the naninovel script execution.
    /// </summary>
    [IgnoreParameter(nameof(Wait))]
    public class Stop : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            Engine.GetService<IScriptPlayer>().Stop();

            return UniTask.CompletedTask;
        }
    }
}
