using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 统一将所有 Canvas 设为按屏幕尺寸缩放，保证 HUD 与配置面板自适应分辨率。
/// </summary>
[DefaultExecutionOrder(-200)]
public class UiCanvasSetup : MonoBehaviour
{
    [SerializeField] Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] [Range(0f, 1f)] float matchWidthOrHeight = 0.5f;

    void Awake()
    {
        ApplyToAllCanvases();
    }

    public void ApplyToAllCanvases()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
            ApplyScaler(canvases[i], referenceResolution, matchWidthOrHeight);
    }

    static void ApplyScaler(Canvas canvas, Vector2 resolution, float match)
    {
        if (canvas.renderMode == RenderMode.WorldSpace)
            return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = resolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = match;
        scaler.referencePixelsPerUnit = 100f;
    }
}
