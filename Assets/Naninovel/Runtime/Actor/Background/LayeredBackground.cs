using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="LayeredActorBehaviour"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(LayeredBackgroundBehaviour), false)]
    public class LayeredBackground : LayeredActor<LayeredBackgroundBehaviour, BackgroundMetadata>, IBackgroundActor
    {
        private BackgroundMatcher matcher;

        public LayeredBackground (string id, BackgroundMetadata meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta, loader) { }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();
            matcher = BackgroundMatcher.CreateFor(ActorMeta, TransitionalRenderer);
        }

        public override void Dispose ()
        {
            base.Dispose();
            matcher?.Stop();
        }
    }
}
