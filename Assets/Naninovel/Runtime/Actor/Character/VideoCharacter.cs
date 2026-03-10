using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="VideoClip"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(VideoClip), true)]
    public class VideoCharacter : VideoActor<CharacterMetadata>, ICharacterActor
    {
        protected override string MixerGroup => Configuration.GetOrDefault<AudioConfiguration>().VoiceGroupPath;

        public CharacterLookDirection LookDirection
        {
            get => TransitionalRenderer.GetLookDirection(ActorMeta.BakedLookDirection);
            set => TransitionalRenderer.SetLookDirection(value, ActorMeta.BakedLookDirection);
        }

        public VideoCharacter (string id, CharacterMetadata meta, StandaloneAppearanceLoader<VideoClip> loader)
            : base(id, meta, loader) { }

        public UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration,
            EasingType easingType = default, AsyncToken asyncToken = default)
        {
            return TransitionalRenderer.ChangeLookDirectionAsync(lookDirection,
                ActorMeta.BakedLookDirection, duration, easingType, asyncToken);
        }
    }
}
