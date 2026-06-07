using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 统一将所有 Canvas 设为按屏幕尺寸缩放，保证 HUD 与配置面板自适应分辨率。
/// </summary>
[DefaultExecutionOrder(-200)]
public class UiCanvasSetup : MonoBehaviour
{
    public const float GlobalUiScale = 1.35f;
    public static readonly Vector2 BaseReferenceResolution = new Vector2(1920f, 1080f);

    [SerializeField] [Range(1f, 2f)] float uiScale = GlobalUiScale;
    [SerializeField] [Range(0f, 1f)] float matchWidthOrHeight = 0.5f;

    public static Vector2 GetReferenceResolution(float scale)
    {
        float s = Mathf.Max(1f, scale);
        return new Vector2(BaseReferenceResolution.x / s, BaseReferenceResolution.y / s);
    }

    void Awake()
    {
        ApplyToAllCanvases();
    }

    public void ApplyToAllCanvases()
    {
        Vector2 resolution = GetReferenceResolution(uiScale);
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
            ApplyScaler(canvases[i], resolution, matchWidthOrHeight);
    }

    public static void ApplyScaler(Canvas canvas, float scale, float match = 0.5f)
    {
        ApplyScaler(canvas, GetReferenceResolution(scale), match);
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
