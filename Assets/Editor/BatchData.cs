using UnityEngine;

namespace UIBatchAnalyzer
{

    public class CanvasData
    {
        public string name;
        public int instanceID;
        public int colorIndex; // 颜色索引
        public bool overrideOrder; // 是否勾选覆盖order
        public bool hasVertex; // 是否有顶点
    }

    public class CanvasRendererData
    {
        public string name;
        public int instanceID;
        public Rect rect; // 渲染区域
        public bool hasClip; // 是否裁剪
        public Rect clipRect; // 裁剪矩形
        public int textureID; // 纹理ID
        public int materialID; // 材质ID
    }

    public class BatchData
    {
        public string name;
        public int instanceID;
        public bool bCanvas; // 是否是Canvas
        public Rect rect; // 渲染区域
        public bool hasClip; // 是否裁剪
        public Rect clipRect; // 裁剪矩形
        public int textureID; // 纹理ID
        public int materialID; // 材质ID
        public int hierarchyIndex; // hierarchy顺序
        public int depth; // 深度
        public int batchIndex; // 批次
        public int colorIndex; // 颜色索引
    }
}