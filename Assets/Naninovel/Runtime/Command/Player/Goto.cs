using System;
using System.Linq;
using Naninovel.Metadata;

namespace Naninovel.Commands
{
    /// <summary>
    /// Navigates naninovel script playback to the provided path.
    /// </summary>
    [Branch(BranchTraits.Endpoint)]
    public class Goto : Command, Command.IForceWait, Command.IPreloadable
    {
        /// <summary>
        /// When applied to an <see cref="IEngineService"/> implementation, the service won't be reset
        /// while executing the goto command and <see cref="StateConfiguration.ResetOnGoto"/> is enabled.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public sealed class DontResetAttribute : Attribute { }

        /// <summary>
        /// When assigned to <see cref="ResetState"/>, forces reset of all the services, 
        /// except the ones with <see cref="DontResetAttribute"/>.
        /// </summary>
        public const string ResetAllFlag = "*";
        /// <summary>
        /// When assigned to <see cref="ResetState"/>, forces no reset.
        /// </summary>
        public const string NoResetFlag = "-";

        /// <summary>
        /// Path to navigate into in the following format: `ScriptName.LabelName`.
        /// When label name is omitted, will play provided script from the start.
        /// When script name is omitted, will attempt to find a label in the currently played script.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, EndpointContext]
        public NamedStringParameter Path;
        /// <summary>
        /// When specified, will control whether to reset the engine services state before loading a script (in case the path is leading to another script):<br/>
        /// - Specify `*` to reset all the services, except the ones with `Goto.DontReset` attribute.<br/>
        /// - Specify service type names (separated by comma) to exclude from reset; all the other services will be reset, including the ones with `Goto.DontReset` attribute.<br/>
        /// - Specify `-` to force no reset (even if it's enabled by default in the configuration).<br/><br/>
        /// Notice, that while some services have `Goto.DontReset` attribute applied and are not reset by default, they should still be specified when excluding specific services from reset.
        /// </summary>
        [ParameterAlias("reset")]
        public StringListParameter ResetState;
        /// <summary>
        /// Whether to hold resources in the target script, which make them preload together with the script this command specified in.
        /// Has no effect outside `Conservative` resource policy. Refer to [memory management](/guide/memory-management) guide for more info.
        /// </summary>
        public BooleanParameter Hold;
        /// <summary>
        /// Whether to release resources before navigating to the target script to free the memory.
        /// Has no effect outside `Optimistic` resource policy. Refer to [memory management](/guide/memory-management) guide for more info.
        /// </summary>
        public BooleanParameter Release;

        protected IScriptPlayer Player => Engine.GetService<IScriptPlayer>();
        protected IScriptLoader Loader => Engine.GetService<IScriptLoader>();
        protected ResourcePolicy Policy => Engine.GetConfiguration<ResourceProviderConfiguration>().ResourcePolicy;

        private static Type[] dontResetTypes;

        public virtual UniTask PreloadResourcesAsync ()
        {
            EnsureDontResetTypesLoaded();
            return UniTask.CompletedTask;
        }

        public virtual void ReleasePreloadedResources () { }

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            if (!TryGetScriptNameAndLabel(out var scriptName, out var label)) return;
            if (ShouldNavigatePlayedScript(scriptName)) NavigatePlayedScript(label);
            else if (ShouldMaskWithLoadingScreen())
                using (await LoadingScreen.ShowAsync(asyncToken))
                    await NavigateOtherScriptAsync(scriptName, label);
            else await NavigateOtherScriptAsync(scriptName, label);
        }

        protected virtual bool TryGetScriptNameAndLabel (out string scriptName, out string label)
        {
            scriptName = Path.Name;
            label = Path.Value.Value;
            var valid = !string.IsNullOrWhiteSpace(scriptName) || Player.PlayedScript;
            if (!valid) Err("Failed to execute '@goto' command: script name is not specified and no script is currently played.");
            return valid;
        }

        protected virtual bool ShouldNavigatePlayedScript (string scriptName)
        {
            return string.IsNullOrWhiteSpace(scriptName) ||
                   Player.PlayedScript && scriptName.EqualsFastIgnoreCase(Player.PlayedScript.Name);
        }

        protected virtual void NavigatePlayedScript (string label)
        {
            if (string.IsNullOrEmpty(label)) Player.Resume();
            else if (Player.PlayedScript.LabelExists(label)) Player.PlayFromLabel(label);
            else Err($"Failed navigating script playback to `{label}` label: label not found in `{Player.PlayedScript.Name}` script.");
        }

        protected virtual bool ShouldMaskWithLoadingScreen ()
        {
            if (!Player.Configuration.ShowLoadingUI) return false;
            if (Policy == ResourcePolicy.Lazy) return false;
            if (Policy == ResourcePolicy.Conservative) return !Assigned(Hold) || !Hold;
            return Assigned(Release) && Release;
        }

        protected virtual async UniTask NavigateOtherScriptAsync (string scriptName, string label)
        {
            EnsureDontResetTypesLoaded();
            var state = Engine.GetService<IStateManager>();
            if (ShouldResetAll()) await state.ResetStateAsync(dontResetTypes, () => Player.PreloadAndPlayAsync(scriptName, label: label));
            else if (ShouldResetSpecific()) await state.ResetStateAsync(ResetState, () => Player.PreloadAndPlayAsync(scriptName, label: label));
            else await Player.PreloadAndPlayAsync(scriptName, label: label);
        }

        protected virtual bool ShouldResetAll ()
        {
            var config = Engine.GetConfiguration<StateConfiguration>();
            return !Assigned(ResetState) && config.ResetOnGoto ||
                   Assigned(ResetState) && ResetState.Length == 1 && ResetState[0] == ResetAllFlag;
        }

        protected virtual bool ShouldResetSpecific ()
        {
            return Assigned(ResetState) && ResetState.Length > 0 && ResetState[0] != NoResetFlag;
        }

        private static void EnsureDontResetTypesLoaded ()
        {
            if (dontResetTypes is null)
                dontResetTypes = Engine.Types.Where(t => t.IsDefined(typeof(DontResetAttribute), false)).ToArray();
        }
    }
}
