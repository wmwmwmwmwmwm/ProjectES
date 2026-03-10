using System.Collections.Generic;

namespace Naninovel.UI
{
    public class ScriptNavigatorPanel : NavigatorPanel, IScriptNavigatorUI
    {
        protected override IReadOnlyCollection<Script> Scripts => ScriptManager.Scripts;
    }
}
