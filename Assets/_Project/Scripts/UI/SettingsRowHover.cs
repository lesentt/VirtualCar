using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 配置行悬停：高亮行并显示说明。
/// </summary>
public class SettingsRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string descriptionKey;
    public SettingsUIController controller;
    public bool restoreNavHighlight;
    public bool highlightBackground = true;

    Image background;
    Color normalColor;
    bool hasBackground;

    void Awake()
    {
        background = GetComponent<Image>();
        if (background != null)
        {
            normalColor = background.color;
            hasBackground = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller?.ShowTooltip(descriptionKey);
        if (hasBackground && highlightBackground && !restoreNavHighlight)
            background.color = controller != null ? controller.RowHoverColor : normalColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller?.HideTooltip();
        if (restoreNavHighlight)
            controller?.RefreshNavHighlight();
        else if (hasBackground && highlightBackground)
            background.color = normalColor;
    }
}
