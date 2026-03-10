using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to load and unload <see cref="Resource"/> objects, agnostic to the provision source.
    /// </summary>
    /// <remarks>
    /// Path argument in all the APIs is assumed local to the loader, ie w/o the provision source prefix.
    /// To get local path, use <see cref="GetLocalPath(string)"/> or <see cref="GetLocalPath(Resource)"/>.
    /// </remarks>
    public interface IResourceLoader
    {
        /// <summary>
        /// Given resource with specified full path is loaded by this loader,
        /// returns local (to the loader) path of the resource, null otherwise.
        /// </summary>
        [CanBeNull] string GetLocalPath (string fullPath);
        /// <summary>
        /// Given specified resource is loaded by this loader,
        /// returns local (to the loader) path of the resource, null otherwise.
        /// </summary>
        [CanBeNull] string GetLocalPath (Resource resource);
        /// <summary>
        /// Checks whether a resource with the specified path is available (can be loaded).
        /// </summary>
        UniTask<bool> ExistsAsync (string path);
        /// <summary>
        /// Locates paths of all the available resources (optionally) filtered by a base path.
        /// </summary>
        UniTask<IReadOnlyCollection<string>> LocateAsync ([CanBeNull] string path = null);
        /// <summary>
        /// Checks whether a resource with the specified local path is loaded by this loader.
        /// </summary>
        bool IsLoaded (string path);
        /// <summary>
        /// Returns a resource with the specified local path in case it's loaded by this loader, null otherwise.
        /// </summary>
        [CanBeNull] Resource GetLoadedOrNull (string path);
        /// <summary>
        /// Returns all the resources currently loaded by this loader.
        /// </summary>
        IReadOnlyCollection<Resource> GetAllLoaded ();
        /// <summary>
        /// Attempts to load a resource with the specified path.
        /// </summary>
        UniTask<Resource> LoadAsync (string path);
        /// <summary>
        /// Attempts to load all the available resources (optionally) filtered by a base path.
        /// </summary>
        UniTask<IReadOnlyCollection<Resource>> LoadAllAsync ([CanBeNull] string path = null);
        /// <summary>
        /// Given resource with specified path is loaded by this loader,
        /// registers specified object as holder of the resource.
        /// The resource won't be unloaded while it's held by at least one object.
        /// </summary>
        void Hold (string path, object holder);
        /// <summary>
        /// Given resource with specified path is loaded by this loader,
        /// removes specified object from holder list of the resource.
        /// Will (optionally) unload the resource in case no other objects are holding it.
        /// </summary>
        void Release (string path, object holder, bool unload = true);
        /// <summary>
        /// Removes specified holder object from holder list of all the resources loaded by this loader.
        /// Will (optionally) unload the affected resources in case no other objects are holding them.
        /// </summary>
        void ReleaseAll (object holder, bool unload = true);
        /// <summary>
        /// Given resource with specified path is loaded by this loader,
        /// checks whether specified holder object is in holder list of the resource.
        /// </summary>
        bool IsHeldBy (string path, object holder);
        /// <summary>
        /// Given resource with specified path is loaded by this loader,
        /// returns number of objects in holder list of the resource.
        /// </summary>
        int CountHolders (string path);
    }

    /// <summary>
    /// Implementation is able to load and unload <see cref="Resource{TResource}"/> objects, agnostic to the provision source.
    /// </summary>
    public interface IResourceLoader<TResource> : IResourceLoader
        where TResource : Object
    {
        /// <inheritdoc cref="IResourceLoader.GetLoadedOrNull"/>
        new Resource<TResource> GetLoadedOrNull (string path);
        /// <inheritdoc cref="IResourceLoader.GetAllLoaded"/>
        new IReadOnlyCollection<Resource<TResource>> GetAllLoaded ();
        /// <inheritdoc cref="IResourceLoader.LoadAsync"/>
        new UniTask<Resource<TResource>> LoadAsync (string path);
        /// <inheritdoc cref="IResourceLoader.LoadAllAsync"/>
        new UniTask<IReadOnlyCollection<Resource<TResource>>> LoadAllAsync (string path = null);
    }
}
