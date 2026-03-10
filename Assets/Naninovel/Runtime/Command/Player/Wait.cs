using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Holds script execution until the specified wait condition.
    /// </summary>
    [IgnoreParameter(nameof(Wait))]
    public class Wait : Command
    {
        /// <summary>
        /// Literal used to indicate "wait-for-input" mode.
        /// </summary>
        public const string InputLiteral = "i";

        /// <summary>
        /// Wait conditions:<br/>
        ///  - `i` user press continue or skip input key;<br/>
        ///  - `0.0` timer (seconds);<br/>
        ///  - `i0.0` timer, that is skip-able by continue or skip input keys.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter WaitMode;

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            // Don't just return here if skip is enabled; state snapshot is marked as allowed for player rollback when setting waiting for input.

            // Always wait for at least a frame; otherwise skip-able timer (eg, @wait i3) may not behave correctly
            // when used before/after a generic text line: https://forum.naninovel.com/viewtopic.php?p=156#p156
            await AsyncUtils.WaitEndOfFrameAsync(asyncToken);

            if (!Assigned(WaitMode))
            {
                Warn($"`{nameof(WaitMode)}` parameter is not specified, the wait command will do nothing.");
                return;
            }

            var waitMode = WaitMode.Value;
            if (waitMode.EqualsFastIgnoreCase(InputLiteral))
                await WaitForInputAsync(asyncToken);
            else if (waitMode.StartsWithFast(InputLiteral) && ParseUtils.TryInvariantFloat(waitMode.GetAfterFirst(InputLiteral), out var waitTime))
                await WaitForTimerAsync(waitTime, asyncToken);
            else if (ParseUtils.TryInvariantFloat(waitMode, out waitTime))
                await WaitForTimerAsync(waitTime, asyncToken.CancellationToken);
            else Warn($"Failed to resolve value of the `{nameof(WaitMode)}` parameter for the wait command. Check the API reference for list of supported values.");
        }

        protected virtual async UniTask WaitForInputAsync (AsyncToken asyncToken)
        {
            var player = Engine.GetService<IScriptPlayer>();
            player.SetWaitingForInputEnabled(true);
            while (Application.isPlaying && asyncToken.EnsureNotCanceledOrCompleted())
            {
                await AsyncUtils.WaitEndOfFrameAsync(asyncToken);
                if (!player.WaitingForInput || player.AutoPlayActive) break;
            }
        }

        protected virtual async UniTask WaitForTimerAsync (float waitTime, AsyncToken asyncToken)
        {
            var player = Engine.GetService<IScriptPlayer>();
            if (player.SkipActive) return;

            var startTime = Engine.Time.Time;
            while (Application.isPlaying && !player.Synchronizing && asyncToken.EnsureNotCanceledOrCompleted())
            {
                await AsyncUtils.WaitEndOfFrameAsync(asyncToken);
                var waitedEnough = Engine.Time.Time - startTime >= waitTime;
                if (waitedEnough) break;
            }
        }
    }
}
