using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="SpriteActor{TMeta}"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(Texture2D), true)]
    public class SpriteCharacter : SpriteActor<CharacterMetadata>, ICharacterActor
    {
        public CharacterLookDirection LookDirection
        {
            get => TransitionalRenderer.GetLookDirection(ActorMeta.BakedLookDirection);
            set => TransitionalRenderer.SetLookDirection(value, ActorMeta.BakedLookDirection);
        }

        public SpriteCharacter (string id, CharacterMetadata meta, StandaloneAppearanceLoader<Texture2D> loader)
            : base(id, meta, loader) { }

        public UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration,
            EasingType easingType = default, AsyncToken asyncToken = default)
        {
            return TransitionalRenderer.ChangeLookDirectionAsync(lookDirection,
                ActorMeta.BakedLookDirection, duration, easingType, asyncToken);
        }
    }
}
