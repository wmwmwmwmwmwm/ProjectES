using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <inheritdoc cref="IMoviePlayer"/>
    [InitializeAtRuntime]
    public class MoviePlayer : IMoviePlayer
    {
        public event Action OnMoviePlay;
        public event Action OnMovieStop;

        public virtual MoviesConfiguration Configuration { get; }
        public virtual bool Playing { get; private set; }

        protected virtual VideoPlayer Player { get; private set; }
        protected virtual AudioSource AudioSource { get; private set; }
        protected virtual bool UrlStreaming => Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor;

        private readonly IInputManager input;
        private readonly IResourceProviderManager resources;
        private readonly ILocalizationManager l10n;
        private readonly IAudioManager audio;
        private LocalizableResourceLoader<VideoClip> videoLoader;
        private string playedMovieName;
        private IInputSampler skipInput;
        private string streamExtension;

        public MoviePlayer (MoviesConfiguration config, IResourceProviderManager resources,
            ILocalizationManager l10n, IInputManager input, IAudioManager audio)
        {
            Configuration = config;
            this.resources = resources;
            this.l10n = l10n;
            this.input = input;
            this.audio = audio;
        }

        public virtual UniTask InitializeServiceAsync ()
        {
            videoLoader = Configuration.Loader.CreateLocalizableFor<VideoClip>(resources, l10n);
            streamExtension = Engine.GetConfiguration<ResourceProviderConfiguration>().VideoStreamExtension;
            skipInput = input.GetSkipMovie();

            Player = CreatePlayer();
            AudioSource = SetupAudioSource();

            if (Configuration.SkipOnInput && skipInput != null)
                skipInput.OnStart += Stop;

            return UniTask.CompletedTask;
        }

        public virtual void ResetService ()
        {
            if (Playing) Stop();
            videoLoader?.ReleaseAll(this);
        }

        public virtual void DestroyService ()
        {
            if (Playing) Stop();
            if (Player) ObjectUtils.DestroyOrImmediate(Player.gameObject);
            if (skipInput != null) skipInput.OnStart -= Stop;
            videoLoader?.ReleaseAll(this);
        }

        public virtual async UniTask<Texture> PlayAsync (string movieName, AsyncToken asyncToken = default)
        {
            if (Playing) Stop();
            playedMovieName = movieName;
            SetIsPlaying(true);
            if (UrlStreaming) Player.url = BuildStreamUrl(movieName);
            else Player.clip = await LoadMovieClipAsync(movieName, asyncToken);
            await PreparePlayerAsync(asyncToken);
            Player.Play();
            return Player.texture;
        }

        public virtual void Stop ()
        {
            if (!Playing) return;

            if (Player) Player.Stop();
            videoLoader?.Release(playedMovieName, this);
            playedMovieName = null;
            SetIsPlaying(false);
        }

        public virtual async UniTask HoldResourcesAsync (string movieName, object holder)
        {
            if (UrlStreaming) return;
            await videoLoader.LoadAndHoldAsync(movieName, holder);
        }

        public virtual void ReleaseResources (string movieName, object holder)
        {
            if (UrlStreaming) return;
            videoLoader?.Release(movieName, holder);
        }

        protected virtual VideoPlayer CreatePlayer ()
        {
            var player = Engine.CreateObject<VideoPlayer>(nameof(MoviePlayer));
            player.playOnAwake = false;
            player.skipOnDrop = Configuration.SkipFrames;
            player.source = UrlStreaming ? VideoSource.Url : VideoSource.VideoClip;
            player.renderMode = VideoRenderMode.APIOnly;
            player.isLooping = false;
            player.loopPointReached += _ => Stop();
            return player;
        }

        protected virtual AudioSource SetupAudioSource ()
        {
            var source = Player.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.bypassReverbZones = true;
            source.bypassEffects = true;
            if (audio.AudioMixer)
                source.outputAudioMixerGroup = audio.AudioMixer.FindMatchingGroups("Master")?.FirstOrDefault();
            return source;
        }

        protected virtual string BuildStreamUrl (string movieName)
        {
            var clipPath = $"{Configuration.Loader.PathPrefix}/{movieName}{streamExtension}";
            return PathUtils.Combine(Application.streamingAssetsPath, clipPath);
        }

        protected virtual async UniTask<VideoClip> LoadMovieClipAsync (string movieName, AsyncToken asyncToken)
        {
            var videoResource = await videoLoader.LoadAndHoldAsync(movieName, this);
            asyncToken.ThrowIfCanceled();
            if (!videoResource.Valid) throw new Error($"Failed to load `{movieName}` movie.");
            return videoResource.Object;
        }

        protected virtual async UniTask PreparePlayerAsync (AsyncToken asyncToken)
        {
            Player.Prepare();
            while (Playing && !Player.isPrepared)
                await AsyncUtils.WaitEndOfFrameAsync(asyncToken);

            // Can't set this in audio source setup as Unity is failing
            // to play audio after playing a clip w/o audio in such case.
            Player.controlledAudioTrackCount = 1;
            Player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            Player.SetTargetAudioSource(0, AudioSource);
        }

        private void SetIsPlaying (bool playing)
        {
            Playing = playing;
            if (playing) OnMoviePlay?.Invoke();
            else OnMovieStop?.Invoke();
        }
    }
}
