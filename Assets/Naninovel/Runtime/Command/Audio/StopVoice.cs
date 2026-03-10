namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playback of the currently played voice clip.
    /// </summary>
    [IgnoreParameter(nameof(Wait))]
    public class StopVoice : AudioCommand
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            AudioManager.StopVoice();
            return UniTask.CompletedTask;
        }
    }
}
