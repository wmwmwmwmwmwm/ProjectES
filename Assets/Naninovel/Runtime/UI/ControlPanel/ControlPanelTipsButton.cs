namespace Naninovel.UI
{
    public class ControlPanelTipsButton : ScriptableLabeledButton
    {
        private IUIManager uiManager;

        public virtual void SetActiveIfTipsExist (bool active)
        {
            if (TipsExist()) gameObject.SetActive(active);
        }

        protected override void Awake ()
        {
            base.Awake();

            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void Start ()
        {
            base.Start();
            if (!TipsExist())
                gameObject.SetActive(false);
        }

        protected override void OnButtonClick ()
        {
            uiManager.GetUI<IPauseUI>()?.Hide();
            uiManager.GetUI<ITipsUI>()?.Show();
        }

        protected virtual bool TipsExist ()
        {
            var tipsUI = uiManager.GetUI<ITipsUI>();
            return tipsUI != null && tipsUI.TipsCount > 0;
        }
    }
}
