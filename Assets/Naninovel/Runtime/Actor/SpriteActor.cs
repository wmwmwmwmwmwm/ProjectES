using System.Linq;
using Naninovel.FX;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="MonoBehaviourActor{TMeta}"/> using <see cref="TransitionalSpriteRenderer"/> to represent appearance of the actor.
    /// </summary>
    public abstract class SpriteActor<TMeta> : MonoBehaviourActor<TMeta>, Blur.IBlurable
        where TMeta : OrthoActorMetadata
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected virtual StandaloneAppearanceLoader<Texture2D> AppearanceLoader { get; }
        protected virtual TransitionalRenderer TransitionalRenderer { get; private set; }

        private readonly Semaphore changeAppearanceLock = new Semaphore(1);

        private string appearance;
        private bool visible;
        private Resource<Texture2D> defaultAppearance;

        protected SpriteActor (string id, TMeta meta, StandaloneAppearanceLoader<Texture2D> loader)
            : base(id, meta)
        {
            AppearanceLoader = loader;
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            AppearanceLoader.OnLocalized -= HandleAppearanceLocalized;
            AppearanceLoader.OnLocalized += HandleAppearanceLocalized;
            TransitionalRenderer = TransitionalRenderer.CreateFor(ActorMeta, GameObject, false);
            SetVisibility(false);
        }

        public virtual UniTask BlurAsync (float intensity, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            return TransitionalRenderer.BlurAsync(intensity, duration, easingType, asyncToken);
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, AsyncToken asyncToken = default)
        {
            var previousAppearance = this.appearance;
            this.appearance = appearance;

            try
            {
                await changeAppearanceLock.WaitAsync(asyncToken.CancellationToken);

                var textureResource = string.IsNullOrWhiteSpace(appearance)
                    ? await LoadDefaultAppearanceAsync(asyncToken)
                    : await LoadAppearanceAsync(appearance, asyncToken);

                AppearanceLoader.Hold(appearance, this);
                await TransitionalRenderer.TransitionToAsync(textureResource, duration, easingType, transition, asyncToken);

                // When using `wait:false` this async method won't be waited, which potentially could lead to a situation, where
                // a consequent same method will re-set the currently disposed resource.
                // Here we check that the disposed (previousAppearance) resource is not actually being used right now, before disposing it.
                if (previousAppearance != appearance)
                    AppearanceLoader?.Release(previousAppearance, this);
            }
            finally { changeAppearanceLock.Release(); }
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            this.visible = visible;

            // When appearance is not set (and default one is not preloaded for some reason, eg when using dynamic parameters) 
            // and revealing the actor -- attempt to load default appearance texture.
            if (!Visible && visible && string.IsNullOrWhiteSpace(Appearance) && !AppearanceLoader.IsLoaded(defaultAppearance?.Path))
                await ChangeAppearanceAsync(null, 0, asyncToken: asyncToken);

            await TransitionalRenderer.FadeToAsync(visible ? TintColor.a : 0, duration, easingType, asyncToken);
        }

        public override void Dispose ()
        {
            base.Dispose();

            if (AppearanceLoader != null)
            {
                AppearanceLoader.OnLocalized -= HandleAppearanceLocalized;
                AppearanceLoader.ReleaseAll(this);
            }
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => TransitionalRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = TransitionalRenderer.TintColor.a;
            TransitionalRenderer.TintColor = tintColor;
        }

        protected virtual async UniTask<Resource<Texture2D>> LoadAppearanceAsync (string appearance, AsyncToken asyncToken)
        {
            var texture = await AppearanceLoader.LoadAsync(appearance);
            asyncToken.ThrowIfCanceled();

            if (!texture.Valid)
            {
                Engine.Warn($"Failed to load '{appearance}' appearance texture for '{Id}' sprite actor: the resource is not found.");
                return null;
            }

            ApplyTextureSettings(texture);
            return texture;
        }

        protected virtual async UniTask<Resource<Texture2D>> LoadDefaultAppearanceAsync (AsyncToken asyncToken)
        {
            if (defaultAppearance != null && defaultAppearance.Valid) return defaultAppearance;

            var defaultTexturePath = await LocateDefaultAppearanceAsync(asyncToken);
            if (!string.IsNullOrEmpty(defaultTexturePath))
            {
                // Never release default appearances, otherwise they're reloaded each time actor is removed/added.
                defaultAppearance = await AppearanceLoader.LoadAndHoldAsync(defaultTexturePath, Engine.Behaviour);
                asyncToken.ThrowIfCanceled();
            }
            else defaultAppearance = new Resource<Texture2D>(null, Engine.LoadInternalResource<Texture2D>("Textures/UnknownActor"));

            ApplyTextureSettings(defaultAppearance);

            if (!TransitionalRenderer.MainTexture)
                TransitionalRenderer.MainTexture = defaultAppearance;

            return defaultAppearance;
        }

        protected virtual async UniTask<string> LocateDefaultAppearanceAsync (AsyncToken asyncToken)
        {
            var texturePaths = (await AppearanceLoader.LocateAsync(string.Empty))?.ToList();
            asyncToken.ThrowIfCanceled();
            if (texturePaths != null && texturePaths.Count > 0)
            {
                // First, look for an appearance with a name, equal to actor's ID.
                if (texturePaths.Any(t => t.EqualsFast(Id)))
                    return texturePaths.First(t => t.EqualsFast(Id));
                // Then, try a `Default` appearance.
                if (texturePaths.Any(t => t.EqualsFast("Default")))
                    return texturePaths.First(t => t.EqualsFast("Default"));
                // Finally, fallback to a first defined appearance.
                return texturePaths.FirstOrDefault();
            }
            return null;
        }

        protected virtual void ApplyTextureSettings (Texture2D texture)
        {
            if (texture && texture.wrapMode != TextureWrapMode.Clamp)
                texture.wrapMode = TextureWrapMode.Clamp;
        }

        protected virtual void HandleAppearanceLocalized (Resource<Texture2D> resource)
        {
            if (Appearance == AppearanceLoader.GetLocalPath(resource))
                Appearance = Appearance;
        }
    }
}
