using UnityEngine;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TitleMenu : CustomUI, ITitleUI
    {
        private IScriptPlayer player;
        private string titleScriptName;

        protected override void Awake ()
        {
            base.Awake();

            player = Engine.GetService<IScriptPlayer>();
            titleScriptName = Engine.GetConfiguration<ScriptsConfiguration>().TitleScript;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float? duration = null, AsyncToken asyncToken = default)
        {
            if (visible && !string.IsNullOrEmpty(titleScriptName))
                using (new InteractionBlocker())
                    await PlayTitleScript(asyncToken);
            await base.ChangeVisibilityAsync(visible, duration, asyncToken);
        }

        protected virtual async UniTask PlayTitleScript (AsyncToken asyncToken)
        {
            while (Engine.Initializing) await AsyncUtils.WaitEndOfFrameAsync();
            await player.PreloadAndPlayAsync(titleScriptName);
            asyncToken.ThrowIfCanceled();
            while (player.Playing) await AsyncUtils.WaitEndOfFrameAsync();
            asyncToken.ThrowIfCanceled();
        }
    }
}
