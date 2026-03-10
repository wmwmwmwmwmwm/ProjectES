using System.Collections.Generic;

namespace Naninovel.UI
{
    public class ExternalScriptsBrowserPanel : NavigatorPanel, IExternalScriptsUI
    {
        protected override IReadOnlyCollection<Script> Scripts => ScriptManager.ExternalScripts;
    }
}
