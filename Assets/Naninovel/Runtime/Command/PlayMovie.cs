using Naninovel.UI;

namespace Naninovel.Commands
{
    /// <summary>
    /// Plays a movie with the provided name (path).
    /// </summary>
    /// <remarks>
    /// Will fade-out the screen before playing the movie and fade back in after the play.
    /// Playback can be canceled by activating a `cancel` input (`Esc` key by default).
    /// </remarks>
    [CommandAlias("movie")]
    public class PlayMovie : Command, Command.IPreloadable
    {
        /// <summary>
        /// Name of the movie resource to play.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, ResourceContext(MoviesConfiguration.DefaultPathPrefix)]
        public StringParameter MovieName;
        /// <summary>
        /// Duration (in seconds) of the fade animation. 
        /// When not specified, will use fade duration set in the movie configuration.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration;
        /// <summary>
        /// Whether to block interaction with the game while the movie is playing,
        /// preventing the player from skipping it.
        /// </summary>
        [ParameterAlias("block")]
        public BooleanParameter BlockInteraction;

        protected virtual IMoviePlayer Player => Engine.GetService<IMoviePlayer>();

        public async UniTask PreloadResourcesAsync ()
        {
            if (!Assigned(MovieName) || MovieName.DynamicValue) return;
            await Player.HoldResourcesAsync(MovieName, this);
        }

        public void ReleasePreloadedResources ()
        {
            if (!Assigned(MovieName) || MovieName.DynamicValue) return;
            Player?.ReleaseResources(MovieName, this);
        }

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var movieUI = Engine.GetService<IUIManager>()?.GetUI<IMovieUI>();
            if (movieUI is null) return;

            var blocker = BlockInteraction ? new InteractionBlocker() : null;

            var fadeDuration = Assigned(Duration) ? Duration.Value : Player.Configuration.FadeDuration;
            await movieUI.ChangeVisibilityAsync(true, fadeDuration, asyncToken);

            var movieTexture = await Player.PlayAsync(MovieName, asyncToken);
            movieUI.SetMovieTexture(movieTexture);

            while (Player.Playing)
                await AsyncUtils.WaitEndOfFrameAsync(asyncToken);

            blocker?.Dispose();

            await movieUI.ChangeVisibilityAsync(false, fadeDuration, asyncToken);
        }
    }
}
