using UnityEngine;

/// <summary>
/// 在 Inspector 中显示中文参数名（配合 LabelTextDrawer 使用）。
/// </summary>
public class LabelTextAttribute : PropertyAttribute
{
    public string Label { get; }

    public LabelTextAttribute(string label)
    {
        Label = label;
    }
}
