using System;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Hosts events routed by <see cref="GenericActor{TBehaviour,TMeta}"/>.
    /// </summary>
    public abstract class GenericActorBehaviour : MonoBehaviour
    {
        [Serializable]
        public class AppearanceChangedEvent : UnityEvent<string> { }
        [Serializable]
        public class VisibilityChangedEvent : UnityEvent<bool> { }
        [Serializable]
        public class TintColorChangedEvent : UnityEvent<Color> { }

        /// <summary>
        /// Invoked when appearance of the actor is changed.
        /// </summary>
        public event Action<string> OnAppearanceChanged;
        /// <summary>
        /// Invoked when visibility of the actor is changed.
        /// </summary>
        public event Action<bool> OnVisibilityChanged;
        /// <summary>
        /// Invoked when tint color of the actor is changed.
        /// </summary>
        public event Action<Color> OnTintColorChanged;

        [Tooltip("Invoked when appearance of the actor is changed.")]
        [SerializeField] public AppearanceChangedEvent onAppearanceChanged;
        [Tooltip("Invoked when visibility of the actor is changed.")]
        [SerializeField] public VisibilityChangedEvent onVisibilityChanged;
        [Tooltip("Invoked when tint color of the actor is changed.")]
        [SerializeField] public TintColorChangedEvent onTintColorChanged;

        public virtual void InvokeAppearanceChangedEvent (string value)
        {
            OnAppearanceChanged?.Invoke(value);
            onAppearanceChanged?.Invoke(value);
        }

        public virtual void InvokeVisibilityChangedEvent (bool value)
        {
            OnVisibilityChanged?.Invoke(value);
            onVisibilityChanged?.Invoke(value);
        }

        public virtual void InvokeTintColorChangedEvent (Color value)
        {
            OnTintColorChanged?.Invoke(value);
            onTintColorChanged?.Invoke(value);
        }
    }
}
