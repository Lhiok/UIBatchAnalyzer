# UIBatchAnalyzer
运行时对UnityUI进行简单分析并在Hierarchy窗口显示
* 未考虑<b>order</b>和<b>layer</b>影响
* 嵌套Canvas处理过于复杂，不同Canvas批次信息独立计算
* 由于无法获取_texID，批次顺序可能与实际顺序存在差异
