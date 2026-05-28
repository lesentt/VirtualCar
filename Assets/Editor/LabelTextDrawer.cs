#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LabelTextAttribute))]
public class LabelTextDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        LabelTextAttribute labelText = (LabelTextAttribute)attribute;
        TooltipAttribute tooltip = fieldInfo.GetCustomAttribute<TooltipAttribute>();
        string tip = tooltip != null ? tooltip.tooltip : property.tooltip;
        GUIContent content = new GUIContent(labelText.Label, tip);
        EditorGUI.PropertyField(position, property, content, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }
}
#endif
