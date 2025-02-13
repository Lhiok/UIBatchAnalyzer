using UnityEngine;

namespace UIBatchAnalyzer
{
    
    public static class ColorUtility
    {

        private static readonly Color[] s_presetColors = new Color[] {
            Color.green,
            Color.yellow,
            Color.white,
            Color.red
        };

        public static Color GetPresetColor(int index)
        {
            return s_presetColors[index % s_presetColors.Length];
        }
    }
}