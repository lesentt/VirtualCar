using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 运行时构建左下角车辆驾驶 HUD（车速、档位、损伤、物理数据等）。
/// </summary>
public class VehicleHudController : MonoBehaviour
{
    static readonly Color PanelBg = new Color(0.06f, 0.07f, 0.09f, 0.88f);
    static readonly Color PanelBorder = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color TextPrimary = new Color(0.95f, 0.96f, 0.98f);
    static readonly Color TextMuted = new Color(0.55f, 0.58f, 0.62f);
    static readonly Color AccentCyan = new Color(0.49f, 0.83f, 0.99f);
    static readonly Color AccentGreen = new Color(0.3f, 0.87f, 0.5f);
    static readonly Color AccentYellow = new Color(0.98f, 0.75f, 0.14f);
    static readonly Color AccentOrange = new Color(0.98f, 0.57f, 0.24f);
    static readonly Color AccentRed = new Color(0.94f, 0.27f, 0.27f);
    static readonly Color TabNormal = new Color(0.1f, 0.11f, 0.14f, 0.85f);
    static readonly Color TabActive = new Color(0.32f, 0.34f, 0.38f, 0.95f);
    static readonly Color CardBg = new Color(0.04f, 0.05f, 0.07f, 0.6f);
    static readonly Color WheelOn = new Color(0.3f, 0.87f, 0.5f, 0.95f);
    static readonly Color WheelOff = new Color(0.25f, 0.27f, 0.3f, 0.7f);
    static readonly Color BarBg = new Color(0.08f, 0.09f, 0.11f, 0.9f);
    static readonly Color ThrottleFill = new Color(0.49f, 0.83f, 0.99f, 0.85f);

    const float PanelWidth = 340f;
    const float PanelHeight = 500f;
    const float PanelMargin = 16f;
    const int Pad = 14;

    static Font uiFont;

    GameObject hudRoot;
    RectTransform contentRect;
    Text vehicleNameText;
    Text speedText;
    Text gearText;
    Image gearBadgeImage;
    Image throttleFillImage;
    Text throttleText;
    Text accelValueText;
    Image[] wheelDots;
    Text damageStatusText;
    Text damagePercentText;
    Image damageBarFill;
    Text damageMetaText;
    Text driveForceText;
    Text frictionText;
    Text gripText;
    Text massText;

    const int MaxVehicleTabs = 4;

    readonly GameObject[] tabRoots = new GameObject[MaxVehicleTabs];
    readonly Image[] tabImages = new Image[MaxVehicleTabs];
    readonly Text[] tabLabels = new Text[MaxVehicleTabs];

    public GameObject HudRoot => hudRoot;

    public void Build(Transform canvasParent)
    {
        if (hudRoot != null)
            Destroy(hudRoot);

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        hudRoot = CreatePanel(canvasParent, "VehicleHud", PanelWidth, PanelHeight,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(PanelMargin, PanelMargin), PanelBg);
        AddBorder(hudRoot, PanelBorder);
        hudRoot.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(hudRoot.transform, false);
        contentRect = content.GetComponent<RectTransform>();
        StretchFull(contentRect);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(Pad, Pad, Pad, Pad);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateHeaderRow(content.transform);
        CreateDividerRow(content.transform);
        CreateSpeedRow(content.transform);
        CreateThrottleRow(content.transform);
        CreateDriveRow(content.transform);
        CreateDividerRow(content.transform);
        CreateDamageBlock(content.transform);
        CreateDividerRow(content.transform);
        CreateStatsGrid(content.transform);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    public void SetVehicleTabs(int total, int activeIndex)
    {
        for (int i = 0; i < tabRoots.Length; i++)
        {
            if (tabRoots[i] == null) continue;
            bool visible = i < total;
            tabRoots[i].SetActive(visible);
            if (visible)
            {
                tabImages[i].color = i == activeIndex ? TabActive : TabNormal;
                if (tabLabels[i] != null)
                    tabLabels[i].color = i == activeIndex ? TextPrimary : TextMuted;
            }
        }
    }

    public void Refresh(
        string vehicleName, float speedKmh, string gear, float throttle01, float accelerationG,
        int groundedWheels, float damagePercent, string damageStatus, float driveMultiplier,
        int collisionCount, float lastImpulse, float driveForce, float groundResistance,
        float gripCoefficient, float massKg)
    {
        if (hudRoot == null) return;

        if (vehicleNameText != null) vehicleNameText.text = vehicleName;
        if (speedText != null) speedText.text = Mathf.RoundToInt(speedKmh).ToString();
        if (gearText != null) gearText.text = gear;
        if (gearBadgeImage != null) gearBadgeImage.color = GetGearColor(gear);

        float throttle = Mathf.Clamp01(Mathf.Abs(throttle01));
        if (throttleFillImage != null) throttleFillImage.fillAmount = throttle;
        if (throttleText != null) throttleText.text = $"油门 {throttle * 100f:F0}%";

        if (accelValueText != null) accelValueText.text = $"{accelerationG:F2} G";

        if (wheelDots != null)
        {
            for (int i = 0; i < wheelDots.Length; i++)
            {
                if (wheelDots[i] == null) continue;
                wheelDots[i].color = i < groundedWheels ? WheelOn : WheelOff;
            }
        }

        float damage = Mathf.Clamp(damagePercent, 0f, 100f);
        if (damageStatusText != null)
        {
            damageStatusText.text = damageStatus;
            damageStatusText.color = GetDamageStatusColor(damageStatus);
        }

        if (damagePercentText != null) damagePercentText.text = $"{damage:F0}%";

        if (damageBarFill != null)
        {
            damageBarFill.fillAmount = damage / 100f;
            damageBarFill.color = GetDamageBarColor(damage);
        }

        if (damageMetaText != null)
            damageMetaText.text = $"动力 {driveMultiplier:P0}  ·  碰撞 {collisionCount} 次  ·  冲量 {lastImpulse:F0}";

        if (driveForceText != null) driveForceText.text = $"{driveForce:F0} N·m";
        if (frictionText != null) frictionText.text = $"{groundResistance:F0} N";
        if (gripText != null) gripText.text = $"{gripCoefficient:F2}";
        if (massText != null) massText.text = $"{massKg:F0} kg";
    }

    void CreateHeaderRow(Transform parent)
    {
        GameObject row = CreateLayoutRow(parent, 28f);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        ConfigureRow(h, 6f, TextAnchor.MiddleLeft);

        CreateFixedImage(row.transform, "Dot", AccentCyan, 10f, 10f);

        vehicleNameText = CreateRowText(row.transform, "VehicleName", "car", 14, TextAnchor.MiddleLeft, TextPrimary);
        SetFlexible(vehicleNameText.gameObject, 1f, 0f);

        GameObject tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        tabs.transform.SetParent(row.transform, false);
        ConfigureRow(tabs.GetComponent<HorizontalLayoutGroup>(), 4f, TextAnchor.MiddleRight);
        SetFixed(tabs, 112f, 28f);

        for (int i = 0; i < MaxVehicleTabs; i++)
        {
            GameObject tab = CreateFixedImage(tabs.transform, $"Tab_{i + 1}", TabNormal, 24f, 22f);
            tabLabels[i] = CreateRowText(tab.transform, "Label", (i + 1).ToString(), 11,
                TextAnchor.MiddleCenter, TextMuted);
            StretchFull(tabLabels[i].rectTransform);
            tabRoots[i] = tab;
            tabImages[i] = tab.GetComponent<Image>();
        }
    }

    void CreateSpeedRow(Transform parent)
    {
        GameObject row = CreateLayoutRow(parent, 76f);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        ConfigureRow(h, 8f, TextAnchor.MiddleLeft);

        GameObject left = new GameObject("SpeedLeft", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        left.transform.SetParent(row.transform, false);
        VerticalLayoutGroup leftLayout = left.GetComponent<VerticalLayoutGroup>();
        leftLayout.spacing = -2f;
        leftLayout.childAlignment = TextAnchor.LowerLeft;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;
        SetFlexible(left, 1f, 0f);

        speedText = CreateRowText(left.transform, "SpeedValue", "0", 44, TextAnchor.MiddleLeft, TextPrimary);
        SetFlexible(speedText.gameObject, 1f, 0f);
        CreateRowText(left.transform, "SpeedUnit", "km/h", 12, TextAnchor.MiddleLeft, TextMuted);

        GameObject badge = CreateFixedImage(row.transform, "GearBadge", AccentCyan, 48f, 48f);
        gearBadgeImage = badge.GetComponent<Image>();
        gearText = CreateRowText(badge.transform, "Gear", "N", 26, TextAnchor.MiddleCenter, TextPrimary);
        StretchFull(gearText.rectTransform);
    }

    void CreateThrottleRow(Transform parent)
    {
        GameObject block = CreateLayoutRow(parent, 32f);
        VerticalLayoutGroup v = block.AddComponent<VerticalLayoutGroup>();
        v.spacing = 3f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        GameObject bar = CreateFixedImage(block.transform, "ThrottleBar", BarBg, 0f, 12f);
        SetFlexible(bar, 1f, 0f);
        throttleFillImage = CreateBarFill(bar.transform, "Fill", ThrottleFill);

        throttleText = CreateRowText(block.transform, "ThrottleLabel", "油门 0%", 11, TextAnchor.MiddleRight, TextMuted);
        SetFixed(throttleText.gameObject, 0f, 14f);
    }

    void CreateDriveRow(Transform parent)
    {
        GameObject row = CreateLayoutRow(parent, 42f);
        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        ConfigureRow(h, 8f, TextAnchor.MiddleLeft);

        GameObject left = new GameObject("AccelBlock", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        left.transform.SetParent(row.transform, false);
        VerticalLayoutGroup leftV = left.GetComponent<VerticalLayoutGroup>();
        leftV.spacing = 1f;
        leftV.childAlignment = TextAnchor.MiddleLeft;
        leftV.childControlWidth = true;
        leftV.childControlHeight = true;
        leftV.childForceExpandWidth = true;
        leftV.childForceExpandHeight = false;
        SetFlexible(left, 1f, 0f);

        CreateRowText(left.transform, "AccelLabel", "加速度", 11, TextAnchor.MiddleLeft, TextMuted);
        accelValueText = CreateRowText(left.transform, "AccelValue", "0.00 G", 14, TextAnchor.MiddleLeft, TextPrimary);

        GameObject right = new GameObject("WheelBlock", typeof(RectTransform), typeof(LayoutElement));
        right.transform.SetParent(row.transform, false);
        SetFixed(right, 76f, 42f);

        CreateAnchoredText(right.transform, "WheelLabel", "接地", 11, TextAnchor.UpperRight, TextMuted,
            new Vector2(1f, 1f), new Vector2(-2f, -2f), new Vector2(72f, 14f));

        wheelDots = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float x = -2f - (3 - i) * 14f;
            wheelDots[i] = CreateAnchoredImage(right.transform, $"Wheel_{i}", WheelOff, 10f, 10f,
                new Vector2(1f, 0f), new Vector2(x, 8f)).GetComponent<Image>();
        }
    }

    void CreateDamageBlock(Transform parent)
    {
        GameObject block = CreateLayoutRow(parent, 74f);
        VerticalLayoutGroup v = block.AddComponent<VerticalLayoutGroup>();
        v.spacing = 5f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        GameObject header = new GameObject("DamageHeader", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        header.transform.SetParent(block.transform, false);
        ConfigureRow(header.GetComponent<HorizontalLayoutGroup>(), 4f, TextAnchor.MiddleLeft);
        SetFixed(header, 0f, 18f);

        CreateRowText(header.transform, "DamageLabel", "车体状态", 11, TextAnchor.MiddleLeft, TextMuted)
            .gameObject.AddComponent<LayoutElement>().SetFlexible(1f, 0f);
        damageStatusText = CreateRowText(header.transform, "DamageStatus", "完好", 12, TextAnchor.MiddleRight, AccentGreen);
        SetFixed(damageStatusText.gameObject, 42f, 16f);
        damagePercentText = CreateRowText(header.transform, "DamagePercent", "0%", 12, TextAnchor.MiddleRight, TextPrimary);
        SetFixed(damagePercentText.gameObject, 36f, 16f);

        GameObject bar = CreateFixedImage(block.transform, "DamageBar", BarBg, 0f, 10f);
        SetFlexible(bar, 1f, 0f);
        damageBarFill = CreateBarFill(bar.transform, "Fill", AccentGreen);

        damageMetaText = CreateRowText(block.transform, "DamageMeta",
            "动力 100%  ·  碰撞 0 次  ·  冲量 0", 10, TextAnchor.MiddleLeft, TextMuted);
        damageMetaText.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetFixed(damageMetaText.gameObject, 0f, 28f);
    }

    void CreateStatsGrid(Transform parent)
    {
        GameObject grid = CreateLayoutRow(parent, 132f);
        VerticalLayoutGroup v = grid.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        CreateStatRow(grid.transform, "Row1", 60f, "驱动力", out driveForceText, "地面阻力", out frictionText);
        CreateStatRow(grid.transform, "Row2", 60f, "抓地 μ", out gripText, "质量", out massText);
    }

    void CreateStatRow(Transform parent, string name, float height,
        string labelA, out Text valueA, string labelB, out Text valueB)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        ConfigureRow(row.GetComponent<HorizontalLayoutGroup>(), 6f, TextAnchor.UpperLeft);
        SetFixed(row, 0f, height);
        CreateStatCard(row.transform, "CardA", labelA, out valueA);
        CreateStatCard(row.transform, "CardB", labelB, out valueB);
    }

    void CreateStatCard(Transform parent, string name, string label, out Text valueText)
    {
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = CardBg;
        SetFlexible(card, 1f, 0f);
        AddBorder(card, PanelBorder);

        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(8, 8, 6, 6);
        cardLayout.spacing = 2f;
        cardLayout.childAlignment = TextAnchor.UpperLeft;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        Text labelText = CreateRowText(card.transform, "Label", label, 10, TextAnchor.UpperLeft, TextMuted);
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        valueText = CreateRowText(card.transform, "Value", "--", 13, TextAnchor.UpperLeft, AccentCyan);
        valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    static void CreateDividerRow(Transform parent)
    {
        GameObject row = CreateLayoutRow(parent, 1f);
        Image line = row.AddComponent<Image>();
        line.color = DividerColor;
        line.raycastTarget = false;
    }

    static GameObject CreateLayoutRow(Transform parent, float height)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        LayoutElement le = row.GetComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleWidth = 1f;
        le.minWidth = 0f;
        return row;
    }

    static void ConfigureRow(HorizontalLayoutGroup group, float spacing, TextAnchor alignment)
    {
        group.spacing = spacing;
        group.childAlignment = alignment;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
    }

    static Text CreateRowText(Transform parent, string name, string content, int fontSize,
        TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        SetFixed(obj, 0f, fontSize + 4f);

        Text text = obj.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static Text CreateAnchoredText(Transform parent, string name, string content, int fontSize,
        TextAnchor alignment, Color color, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text text = obj.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static GameObject CreateFixedImage(Transform parent, string name, Color color, float width, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (width > 0f && height > 0f)
            SetFixed(go, width, height);
        else
            SetFlexible(go, 1f, height > 0f ? height : 12f);
        return go;
    }

    static GameObject CreateAnchoredImage(Transform parent, string name, Color color, float width, float height,
        Vector2 anchor, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(width, height);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    static GameObject CreatePanel(Transform parent, string name, float width, float height,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(width, height);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    static Image CreateBarFill(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2f, 2f);
        rect.offsetMax = new Vector2(-2f, -2f);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 0f;
        image.raycastTarget = false;
        return image;
    }

    static void SetFixed(GameObject go, float width, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = 0f;
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0f;

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect != null && width > 0f && height > 0f)
            rect.sizeDelta = new Vector2(width, height);
    }

    static void SetFlexible(GameObject go, float flexibleWidth, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = 0f;
        le.preferredWidth = -1f;
        le.flexibleWidth = flexibleWidth;
        if (height > 0f)
        {
            le.minHeight = height;
            le.preferredHeight = height;
        }
    }

    static void AddBorder(GameObject go, Color color)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static Color GetGearColor(string gear)
    {
        switch (gear)
        {
            case "R": return AccentOrange;
            case "N": return TabNormal;
            case "P": return AccentRed;
            default: return AccentCyan;
        }
    }

    static Color GetDamageStatusColor(string status)
    {
        switch (status)
        {
            case "轻损": return AccentYellow;
            case "重损": return AccentOrange;
            case "报废": return AccentRed;
            default: return AccentGreen;
        }
    }

    static Color GetDamageBarColor(float percent)
    {
        if (percent < 35f) return Color.Lerp(AccentGreen, AccentYellow, percent / 35f);
        if (percent < 70f) return Color.Lerp(AccentYellow, AccentOrange, (percent - 35f) / 35f);
        return Color.Lerp(AccentOrange, AccentRed, (percent - 70f) / 30f);
    }
}

static class VehicleHudLayoutExtensions
{
    public static void SetFlexible(this LayoutElement le, float flexibleWidth, float height)
    {
        le.minWidth = 0f;
        le.preferredWidth = -1f;
        le.flexibleWidth = flexibleWidth;
        if (height > 0f)
        {
            le.minHeight = height;
            le.preferredHeight = height;
        }
    }
}
