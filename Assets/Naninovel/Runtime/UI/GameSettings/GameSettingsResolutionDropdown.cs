using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    public class GameSettingsResolutionDropdown : ScriptableDropdown
    {
        private bool allowApplySettings;
        private List<Resolution> filteredResolutions;

        protected override void Start ()
        {
            base.Start();

#if !UNITY_STANDALONE && !UNITY_EDITOR
            transform.parent.gameObject.SetActive(false);
#else
            var desiredRatios = Engine.GetConfiguration<UIConfiguration>().screenRatios?.ToList() ?? new List<Vector2Int>();
            filteredResolutions = Screen.resolutions
                .Where(r =>
                {
                    if (desiredRatios == null || desiredRatios.Count == 0)
                        return true;

                    float ratio = (float)r.width / r.height;
                    return desiredRatios.Any(dr => Mathf.Approximately(ratio, (float)dr.x / dr.y));
                })
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.First())
                .ToList();

            var filtered = filteredResolutions
                .Select(r => $"{r.width} x {r.height}")
                .ToList();

            InitializeOptions(filtered);
#endif
        }

        protected override void OnValueChanged (int value)
        {
            if (!allowApplySettings) return; // Prevent changing resolution when UI initializes.
            var resolution = Screen.resolutions[value];

#if UNITY_2022_2_OR_NEWER
            if (value >= 0 && value < filteredResolutions.Count)
            {
                Resolution resolutionData = filteredResolutions[value];
                Screen.SetResolution(resolutionData.width, resolutionData.height, Screen.fullScreenMode);
            }
#else
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRate);
#endif
        }

        private void InitializeOptions (List<string> availableOptions)
        {
            UIComponent.ClearOptions();
            UIComponent.AddOptions(availableOptions);
            UIComponent.value = GetCurrentResolutionIndex();
            UIComponent.RefreshShownValue();
            allowApplySettings = true;
        }

        /// <summary>
        /// Finds index of the closest to the real (current) available (native to display) resolution.
        /// </summary>
        private int GetCurrentResolutionIndex ()
        {
            if (Screen.resolutions.Length == 0) return 0;

            #if UNITY_2022_2_OR_NEWER
            var currentResolution = new Resolution { width = Screen.width, height = Screen.height, refreshRateRatio = Screen.currentResolution.refreshRateRatio };
            #else
            var currentResolution = new Resolution { width = Screen.width, height = Screen.height, refreshRate = Screen.currentResolution.refreshRate };
            #endif
            var closestResolution = Screen.resolutions.Aggregate((x, y) => ResolutionDiff(x, currentResolution) < ResolutionDiff(y, currentResolution) ? x : y);
            return Array.IndexOf(Screen.resolutions, closestResolution);

            int ResolutionDiff (Resolution a, Resolution b) =>
                Mathf.Abs(a.width - b.width) + Mathf.Abs(a.height - b.height) +
                #if UNITY_2022_2_OR_NEWER
                Mathf.Abs((int)a.refreshRateRatio.value - (int)b.refreshRateRatio.value);
                #else
                Mathf.Abs(a.refreshRate - b.refreshRate);
                #endif
        }
    }
}
