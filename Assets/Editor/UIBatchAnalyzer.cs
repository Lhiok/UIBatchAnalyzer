using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UIBatchAnalyzer
{

    [InitializeOnLoad]
    public static class UIBatchAnalyzer
    {

        private static Dictionary<int, BatchData> m_batchDataDict;

        static UIBatchAnalyzer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }

        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // 获取游戏对象
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null)
            {
                return;
            }
            
            // 为Canvas绘制按钮
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                Rect buttonRect = new Rect(selectionRect.xMax - 80, selectionRect.y, 80, selectionRect.height);
                if (GUI.Button(buttonRect, "Analyze"))
                {
                    if (m_batchDataDict == null)
                    {
                        m_batchDataDict = new Dictionary<int, BatchData>();
                    }
                    CanvasUtility.AnalyzeCanvas(canvas.transform as RectTransform, m_batchDataDict);
                }
            }

            // 为UI绘制深度/批次信息
            if (m_batchDataDict != null && m_batchDataDict.TryGetValue(instanceID, out BatchData batchData))
            {
                Rect labelRect = new Rect(selectionRect.xMax - 100, selectionRect.y, 100, selectionRect.height);
                if (canvas != null)
                {
                    labelRect.x -= 100;
                }
                GUIStyle uIStyle = new GUIStyle();
                uIStyle.normal.textColor = ColorUtility.GetPresetColor(batchData.colorIndex);
                uIStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(labelRect, string.Format("Depth: {0} Batch: {1}", batchData.depth, batchData.batchIndex), uIStyle);
            }
        }
    }
}
