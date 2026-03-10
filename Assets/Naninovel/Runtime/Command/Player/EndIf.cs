namespace Naninovel.Commands
{
    /// <summary>
    /// Alternative to using indentation in conditional blocks: marks end of the block
    /// opened with previous [@if] command, no matter the indentation.
    /// For usage examples see [conditional execution](/guide/naninovel-scripts#conditional-execution) guide.
    /// </summary>
    [IgnoreParameter(nameof(Wait))]
    public class EndIf : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default) => UniTask.CompletedTask;
    }
}
