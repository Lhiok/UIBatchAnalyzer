using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace UIBatchAnalyzer
{
    
    public static class CanvasRenderUtility
    {

        private static readonly VertexHelper s_vertexHelper = new VertexHelper();
        private static readonly Type[] s_OnPopulateMeshParamsTypes = new Type[] { typeof(VertexHelper) };
        private static readonly object[] s_OnPopulateMeshParams = new object[] { s_vertexHelper };

        public static int GetTextureID(CanvasRenderer renderer)
        {
            Debug.Assert(renderer != null);
            Graphic graphic = renderer.GetComponent<Graphic>();
            if (graphic != null)
            {
                // 无法获取m_texID 使用地址替代 顺序可能不对
                return (int)graphic.mainTexture.GetNativeTexturePtr();
            }
            return -1;
        }

        public static int GetMaterialID(CanvasRenderer renderer)
        {
            Debug.Assert(renderer != null);
            return renderer.GetMaterial().GetInstanceID();
        }

        public static Rect GetRect(CanvasRenderer renderer)
        {
            Debug.Assert(renderer != null);
            Graphic graphic = renderer.GetComponent<Graphic>();
            return graphic != null? GetUGUIRect(graphic): GetRectTransformRect(renderer.transform as RectTransform);
        }

        private static Rect GetUGUIRect(Graphic graphic)
        {
            if (!graphic.isActiveAndEnabled || graphic.color.a == 0)
            {
                return Rect.zero;
            }

            RectTransform rectTransform = graphic.rectTransform;

            // TextMeshProUGUI
            if (graphic is TextMeshProUGUI)
            {
                TextMeshProUGUI textMeshProUGUI = graphic as TextMeshProUGUI;
                return GetRectTransformRectByVertices(rectTransform, textMeshProUGUI.mesh.vertices);
            }

            // 生成顶点信息
            s_vertexHelper.Clear();
            if (!RectUtility.IsZero(rectTransform.rect))
            {
                Type graphicType = graphic.GetType();
                MethodInfo methodInfo = graphicType.GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, s_OnPopulateMeshParamsTypes, null);
                Debug.Assert(methodInfo != null);
                methodInfo.Invoke(graphic, s_OnPopulateMeshParams);
            }

            // 修改顶点信息
            List<Component> components = ListPool<Component>.New();
            graphic.GetComponents(typeof(IMeshModifier), components);
            for (int index = 0; index < components.Count; ++index)
            {
                ((IMeshModifier)components[index]).ModifyMesh(s_vertexHelper);
            }
            ListPool<Component>.Free(components);

            // 计算区域
            Type vertexHelperType = s_vertexHelper.GetType();
            FieldInfo fileInfo = vertexHelperType.GetField("m_Positions", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(fileInfo != null);
            List<Vector3> vertices = fileInfo.GetValue(s_vertexHelper) as List<Vector3>;
            return GetRectTransformRectByVertices(rectTransform, vertices.ToArray());
        }

        private static Rect GetRectTransformRect(RectTransform rectTransform)
        {
            Vector3 pivot = rectTransform.pivot;
            Vector3 postion = rectTransform.position;
            Vector3 size = rectTransform.sizeDelta;
            Vector3 lossyScale = rectTransform.lossyScale;
            float width = size.x * lossyScale.x;
            float height = size.y * lossyScale.y;
            return new Rect(postion.x - pivot.x * width, postion.y - pivot.y * height, width, height);
        }

        private static Rect GetRectTransformRectByVertices(RectTransform rectTransform, Vector3[] vertices)
        {
            if (vertices == null || vertices.Length == 0)
            {
                return Rect.zero;
            }

            Vector2 bottomLeft = TMP_Math.MAX_16BIT;
            Vector2 topRight = TMP_Math.MIN_16BIT;
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 vertex = vertices[index];
                bottomLeft.x = Mathf.Min(bottomLeft.x, vertex.x);
                bottomLeft.y = Mathf.Min(bottomLeft.y, vertex.y);
                topRight.x = Mathf.Max(topRight.x, vertex.x);
                topRight.y = Mathf.Max(topRight.y, vertex.y);
            }

            Vector3 postion = rectTransform.position;
            Vector3 lossyScale = rectTransform.lossyScale;
            float x = postion.x + bottomLeft.x * lossyScale.x;
            float y = postion.y + bottomLeft.y * lossyScale.y;
            float width = (topRight.x - bottomLeft.x) * lossyScale.x;
            float height = (topRight.y - bottomLeft.y) * lossyScale.y;
            return new Rect(x, y, width, height);
        }
    }
}