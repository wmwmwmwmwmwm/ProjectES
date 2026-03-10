using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Used by actors which appearance resources are loaded independently of the main actor resource
    /// or otherwise actors which resources are mapped 1-1 to appearances, like sprite actors.
    /// </summary>
    public class StandaloneAppearanceLoader<TResource> : LocalizableResourceLoader<TResource> where TResource : Object
    {
        public StandaloneAppearanceLoader (string actorId, ActorMetadata meta, IResourceProviderManager resources, ILocalizationManager l10n) :
            base(resources.GetProviders(meta.Loader.ProviderTypes), resources, l10n, $"{meta.Loader.PathPrefix}/{actorId}") { }
    }

    /// <summary>
    /// Used by actors which appearance resources are all embedded into single resource and can't be loaded independently,
    /// such as diced sprite actors, which have all their appearances backed into single atlas texture.
    /// </summary>
    public class EmbeddedAppearanceLoader<TResource> : LocalizableResourceLoader<TResource>, IResourceLoader<TResource> where TResource : Object
    {
        private readonly string actorId;

        public EmbeddedAppearanceLoader (string actorId, ActorMetadata meta, IResourceProviderManager resources, ILocalizationManager l10n) :
            base(resources.GetProviders(meta.Loader.ProviderTypes), resources, l10n, meta.Loader.PathPrefix)
        {
            this.actorId = actorId;
        }

        UniTask<bool> IResourceLoader.ExistsAsync (string path) => ExistsAsync(actorId);
        bool IResourceLoader.IsLoaded (string path) => IsLoaded(actorId);
        Resource IResourceLoader.GetLoadedOrNull (string path) => GetLoadedOrNull(actorId);
        async UniTask<Resource> IResourceLoader.LoadAsync (string path) => await LoadAsync(actorId);
        UniTask<Resource<TResource>> IResourceLoader<TResource>.LoadAsync (string path) => LoadAsync(actorId);
        void IResourceLoader.Hold (string path, object holder) => Hold(actorId, holder);
        void IResourceLoader.Release (string path, object holder, bool unload) => Release(actorId, holder, unload);
        void IResourceLoader.ReleaseAll (object holder, bool unload) => ReleaseAll(unload);
        bool IResourceLoader.IsHeldBy (string path, object holder) => IsHeldBy(actorId, holder);
        int IResourceLoader.CountHolders (string path) => CountHolders(actorId);
    }
}
