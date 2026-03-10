using System.Linq;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides all the actors (characters, backgrounds, text printers, choice handlers) on scene.
    /// </summary>
    [CommandAlias("hideAll")]
    public class HideAllActors : Command
    {
        /// <summary>
        /// Duration (in seconds) of the fade animation.
        /// </summary>
        [ParameterAlias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var managers = Engine.FindAllServices<IActorManager>();
            await UniTask.WhenAll(managers.Select(m => HideManagedActorsAsync(m, asyncToken)));
        }

        private UniTask HideManagedActorsAsync (IActorManager manager, AsyncToken asyncToken)
        {
            var duration = Assigned(Duration) ? Duration.Value : manager.ActorManagerConfiguration.DefaultDuration;
            var easing = manager.ActorManagerConfiguration.DefaultEasing;
            return UniTask.WhenAll(manager.GetAllActors().Select(a => a.ChangeVisibilityAsync(false, duration, easing, asyncToken)));
        }
    }
}
