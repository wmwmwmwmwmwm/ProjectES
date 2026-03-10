using System.Linq;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides all the visible characters on scene.
    /// </summary>
    [CommandAlias("hideChars")]
    public class HideAllCharacters : Command
    {
        /// <summary>
        /// Duration (in seconds) of the fade animation.
        /// </summary>
        [ParameterAlias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var manager = Engine.GetService<ICharacterManager>();
            var duration = Assigned(Duration) ? Duration.Value : manager.ActorManagerConfiguration.DefaultDuration;
            var easing = manager.ActorManagerConfiguration.DefaultEasing;
            await UniTask.WhenAll(manager.GetAllActors().Select(a => a.ChangeVisibilityAsync(false, duration, easing, asyncToken)));
        }
    }
}
