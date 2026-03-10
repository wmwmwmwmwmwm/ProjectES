using Naninovel.FX;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <see cref="LayeredActorBehaviour"/> to represent the actor.
    /// </summary>
    public abstract class LayeredActor<TBehaviour, TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TBehaviour : LayeredActorBehaviour
        where TMeta : OrthoActorMetadata
    {
        /// <summary>
        /// Behaviour component of the instantiated layered prefab associated with the actor.
        /// </summary>
        public virtual TBehaviour Behaviour { get; private set; }
        public override string Appearance { get => GetAppearance(); set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }

        private readonly EmbeddedAppearanceLoader<GameObject> prefabLoader;
        private RenderTexture appearanceTexture;
        private string defaultAppearance;
        private string appearance;
        private bool visible, perceivedVisibility;

        protected LayeredActor (string id, TMeta meta, EmbeddedAppearanceLoader<GameObject> loader)
            : base(id, meta)
        {
            prefabLoader = loader;
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            TransitionalRenderer = TransitionalRenderer.CreateFor(ActorMeta, GameObject, true);

            SetVisibility(false);

            var prefabResource = await prefabLoader.LoadAndHoldAsync(Id, this);
            var prefabInstance = Engine.Instantiate(prefabResource.Object, $"{Id}LayeredPrefab", parent: Transform);
            Behaviour = prefabInstance.GetComponent<TBehaviour>();
            defaultAppearance = string.IsNullOrEmpty(Behaviour.DefaultAppearance) ? Behaviour.Composition : Behaviour.DefaultAppearance;

            // Force render once, otherwise the render texture is initially empty.
            await ChangeAppearanceAsync(defaultAppearance, 0);

            Engine.Behaviour.OnBehaviourUpdate -= RenderAppearance;
            Engine.Behaviour.OnBehaviourUpdate += RenderAppearance;
        }

        public virtual UniTask BlurAsync (float intensity, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            return TransitionalRenderer.BlurAsync(intensity, duration, easingType, asyncToken);
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, AsyncToken asyncToken = default)
        {
            if (string.IsNullOrEmpty(appearance))
                appearance = defaultAppearance;

            Behaviour.NotifyAppearanceChanged(this.appearance = appearance);
            if (!Behaviour.RenderOnly) Behaviour.ApplyComposition(appearance);
            var previousTexture = appearanceTexture;
            appearanceTexture = Behaviour.Render(ActorMeta.PixelsPerUnit);
            await TransitionalRenderer.TransitionToAsync(appearanceTexture, duration, easingType, transition, asyncToken);

            if (previousTexture)
                RenderTexture.ReleaseTemporary(previousTexture);
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            this.visible = visible;
            if (visible && Behaviour) Behaviour.NotifyPerceivedVisibilityChanged(perceivedVisibility = true);
            await TransitionalRenderer.FadeToAsync(visible ? TintColor.a : 0, duration, easingType, asyncToken);
            if (!visible && Behaviour) Behaviour.NotifyPerceivedVisibilityChanged(perceivedVisibility = false);
        }

        public override void Dispose ()
        {
            if (Engine.Behaviour != null)
                Engine.Behaviour.OnBehaviourUpdate -= RenderAppearance;

            if (appearanceTexture)
                RenderTexture.ReleaseTemporary(appearanceTexture);

            prefabLoader?.ReleaseAll(this);

            if (Behaviour) ObjectUtils.DestroyOrImmediate(Behaviour.gameObject);

            base.Dispose();
        }

        protected virtual string GetAppearance () => Behaviour.RenderOnly ? appearance : Behaviour.Composition;

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => TransitionalRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        protected virtual void RenderAppearance ()
        {
            if (!perceivedVisibility || !Behaviour || !Behaviour.Animated || !appearanceTexture) return;

            var texture = Behaviour.Render(ActorMeta.PixelsPerUnit, appearanceTexture);
            if (texture != appearanceTexture)
            {
                RenderTexture.ReleaseTemporary(appearanceTexture);
                appearanceTexture = texture;
                TransitionalRenderer.MainTexture = texture;
            }
        }
    }
}
