using System.Collections.Generic;

namespace Naninovel.Commands
{
    /// <summary>
    /// Applies [digital glitch](/guide/special-effects#digital-glitch)
    /// post-processing effect to the main camera simulating digital video distortion and artifacts.
    /// </summary>
    [CommandAlias("glitch")]
    public class SpawnGlitch : SpawnEffect
    {
        /// <summary>
        /// The duration of the effect, in seconds; default is 1.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration;
        /// <summary>
        /// The intensity of the effect, in 0.0 to 10.0 range; default is 1.
        /// </summary>
        [ParameterAlias("power")]
        public DecimalParameter Intensity;

        protected override string Path => "DigitalGlitch";

        protected override StringListParameter GetSpawnParameters () => new List<string> {
            ToSpawnParam(Duration),
            ToSpawnParam(Intensity)
        };
    }
}
