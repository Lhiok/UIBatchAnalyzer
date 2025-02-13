using UnityEngine;

namespace UIBatchAnalyzer
{

    public static class RectUtility
    {

        public static bool IsZero(Rect rect)
        {
            return rect.width <= 0 || rect.height <= 0;
        }

        public static bool IsEqual(Rect a, Rect b)
        {
            return CompareApproximately(a.x, b.x) && CompareApproximately(a.y, b.y) && CompareApproximately(a.width, b.width) && CompareApproximately(a.height, b.height);
        }

        private static bool CompareApproximately(float a, float b, float epsilon = 0.00001f)
        {
            return Mathf.Abs(a - b) <= epsilon;
        }
    }
}