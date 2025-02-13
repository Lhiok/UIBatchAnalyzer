
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace UIBatchAnalyzer
{
    
    public static class CanvasUtility
    {

        public static void AnalyzeCanvas(RectTransform rootCanvas, Dictionary<int, BatchData> batchDataDict)
        {
            Debug.Assert(batchDataDict != null);
            Debug.Assert(rootCanvas != null);

            batchDataDict.Clear();
            Dictionary<int, CanvasData> canvasDataDict = new Dictionary<int, CanvasData>();
            Dictionary<int, CanvasRendererData> rendererDataDict = new Dictionary<int, CanvasRendererData>();
            CollectCanvasData(rootCanvas, null, false, Rect.zero, canvasDataDict, rendererDataDict);
            CollectBatchData(rootCanvas, canvasDataDict, rendererDataDict, batchDataDict, null);
        }

        private static void CollectCanvasData(
            RectTransform rectTransform,
            CanvasData parentCanvasData,
            bool hasClip,
            Rect clipRect,
            Dictionary<int, CanvasData> canvasDataDict,
            Dictionary<int, CanvasRendererData> rendererDataDict)
        {
            if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
            {
                return;
            }

            int instanceID = rectTransform.gameObject.GetInstanceID();

            // Canvas
            Canvas canvas = rectTransform.GetComponent<Canvas>();
            if (canvas != null)
            {
                if (!canvas.isActiveAndEnabled)
                {
                    return;
                }
                CanvasData subCanvasData = new CanvasData();
                subCanvasData.name = rectTransform.name;
                subCanvasData.instanceID = instanceID;
                subCanvasData.colorIndex = parentCanvasData != null? parentCanvasData.colorIndex + 1: 0;
                subCanvasData.overrideOrder = canvas.overrideSorting;
                subCanvasData.hasVertex = false;
                canvasDataDict.Add(instanceID, subCanvasData);
                parentCanvasData = subCanvasData;
            }

            // CanvasRenderer
            CanvasRenderer renderer = rectTransform.GetComponent<CanvasRenderer>();
            if (renderer != null)
            {
                Rect rect = CanvasRenderUtility.GetRect(renderer);
                if (!RectUtility.IsZero(rect) && (!hasClip || clipRect.Overlaps(rect)))
                {
                    Debug.Assert(parentCanvasData != null);
                    parentCanvasData.hasVertex = true;

                    CanvasRendererData rendererData = new CanvasRendererData();
                    rendererData.name = rectTransform.name;
                    rendererData.instanceID = instanceID;
                    rendererData.rect = rect;
                    rendererData.hasClip = hasClip;
                    rendererData.clipRect = clipRect;
                    rendererData.textureID = CanvasRenderUtility.GetTextureID(renderer);
                    rendererData.materialID = CanvasRenderUtility.GetMaterialID(renderer);
                    rendererDataDict.Add(instanceID, rendererData);
                }
            }

            // Mask2D
            RectMask2D mask2D = rectTransform.GetComponent<RectMask2D>();
            if (mask2D != null && mask2D.isActiveAndEnabled)
            {
                Rect rect = rectTransform.rect;
                Vector4 padding = mask2D.padding;
                Vector3 postion = rectTransform.position;
                rect.x += postion.x;
                rect.y += postion.y;
                if (hasClip)
                {
                    clipRect.xMin = Mathf.Max(clipRect.xMin, rect.x + padding.x);
                    clipRect.yMin = Mathf.Max(clipRect.yMin, rect.y + padding.w);
                    clipRect.xMax = Mathf.Min(clipRect.xMax, rect.x + rect.width - padding.z);
                    clipRect.yMax = Mathf.Min(clipRect.yMax, rect.y + rect.height - padding.y);
                }
                else
                {
                    hasClip = true;
                    clipRect.xMin = rect.x + padding.x;
                    clipRect.yMin = rect.y + padding.w;
                    clipRect.xMax = rect.x + rect.width - padding.z;
                    clipRect.yMax = rect.y + rect.height - padding.y;
                }

                if (RectUtility.IsZero(clipRect))
                {
                    return;
                }
            }

            for (int index = 0; index < rectTransform.childCount; ++index)
            {
                CollectCanvasData(rectTransform.GetChild(index) as RectTransform, parentCanvasData, hasClip, clipRect, canvasDataDict, rendererDataDict);
            }
        }

        private static void CollectBatchData(
            RectTransform rectTransform,
            Dictionary<int, CanvasData> canvasDataDict,
            Dictionary<int, CanvasRendererData> rendererDataDict,
            Dictionary<int, BatchData> batchDataDict,
            List<BatchData> batchDataList)
        {
            if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
            {
                return;
            }

            int instanceID = rectTransform.gameObject.GetInstanceID();

            // Canvas
            Canvas canvas = rectTransform.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvasDataDict.TryGetValue(instanceID, out CanvasData canvasData);
                if (canvasData == null || !canvasData.hasVertex)
                {
                    return;
                }
                if (batchDataList != null && !canvasData.overrideOrder)
                {
                    BatchData batchData = new BatchData();
                    batchData.name = canvasData.name;
                    batchData.instanceID = canvasData.instanceID;
                    batchData.bCanvas = true;
                    batchData.hierarchyIndex = batchDataList.Count;
                    batchDataList.Add(batchData);
                }
                batchDataList = ListPool<BatchData>.New();
            }

            Debug.Assert(batchDataList != null);

            // CanvasRenderer
            if (rendererDataDict.TryGetValue(instanceID, out CanvasRendererData rendererData))
            {
                BatchData batchData = new BatchData();
                batchData.name = rendererData.name;
                batchData.instanceID = rendererData.instanceID;
                batchData.bCanvas = false;
                batchData.rect = rendererData.rect;
                batchData.hasClip = rendererData.hasClip;
                batchData.clipRect = rendererData.clipRect;
                batchData.textureID = rendererData.textureID;
                batchData.materialID = rendererData.materialID;
                batchData.hierarchyIndex = batchDataList.Count;
                batchDataList.Add(batchData);
            }

            for (int index = 0; index < rectTransform.childCount; ++index)
            {
                CollectBatchData(rectTransform.GetChild(index) as RectTransform, canvasDataDict, rendererDataDict, batchDataDict, batchDataList);
            }

            if (canvas != null)
            {
                // 深度测试
                DepthTest(batchDataList);
                // 批次测试
                BatchTest(batchDataList);
                canvasDataDict.TryGetValue(instanceID, out CanvasData canvasData);
                Debug.Assert(canvasData != null);
                int colorIndex = canvasData.colorIndex;
                for (int index = 0; index < batchDataList.Count; ++index)
                {
                    BatchData batchData = batchDataList[index];
                    if (!batchData.bCanvas)
                    {
                        batchData.colorIndex = colorIndex;
                        batchDataDict.Add(batchData.instanceID, batchData);
                    }
                }
                ListPool<BatchData>.Free(batchDataList);
            }
        }

        private static void DepthTest(List<BatchData> batchDataList)
        {
            int maxDepth = -1;
            int baseDepth = 0;
            List<BatchData> testBatchList = ListPool<BatchData>.New();
            for (int index = 0; index < batchDataList.Count; ++index)
            {
                BatchData batchData = batchDataList[index];
                if (batchData.bCanvas)
                {
                    batchData.depth = ++maxDepth;
                    baseDepth = maxDepth + 1;
                    testBatchList.Clear();
                    continue;
                }

                batchData.depth = baseDepth;
                for (int i = 0; i < testBatchList.Count; ++i)
                {
                    BatchData preBatchData = testBatchList[i];
                    if (!preBatchData.rect.Overlaps(batchData.rect))
                    {
                        continue;
                    }
                    if (CanBatch(preBatchData, batchData))
                    {
                        batchData.depth = Math.Max(batchData.depth, preBatchData.depth);
                    }
                    else
                    {
                        batchData.depth = Math.Max(batchData.depth, preBatchData.depth + 1);
                    }
                }
                testBatchList.Add(batchData);
                maxDepth = Math.Max(maxDepth, batchData.depth);
            }
            ListPool<BatchData>.Free(testBatchList);
        }

        private static void BatchTest(List<BatchData> batchDataList)
        {
            Debug.Assert(batchDataList.Count > 0);
            batchDataList.Sort(SortBatchData);
            BatchData lastBatchData = batchDataList[0];
            lastBatchData.batchIndex = 1;
            for (int index = 1; index < batchDataList.Count; ++index)
            {
                BatchData batchData = batchDataList[index];
                if (CanBatch(lastBatchData, batchData))
                {
                    batchData.batchIndex = lastBatchData.batchIndex;
                }
                else
                {
                    batchData.batchIndex = lastBatchData.batchIndex + 1;
                }
                lastBatchData = batchData;
            }
        }

        private static bool CanBatch(BatchData x, BatchData y)
        {
            return !x.bCanvas
                && !y.bCanvas
                && x.hasClip == y.hasClip
                && RectUtility.IsEqual(x.clipRect, y.clipRect)
                && x.textureID > 0
                && y.textureID > 0
                && x.textureID == y.textureID
                && x.materialID == y.materialID;
        }

        private static int SortBatchData(BatchData x, BatchData y)
        {
            // 深度
            if (x.depth != y.depth)
            {
                return x.depth - y.depth;
            }
            // 材质
            if (x.materialID != y.materialID)
            {
                return x.materialID - y.materialID;
            }
            // 纹理
            if (x.textureID != y.textureID)
            {
                return x.textureID - y.textureID;
            }
            // 顺序
            return x.hierarchyIndex - y.hierarchyIndex;
        }
    }
}