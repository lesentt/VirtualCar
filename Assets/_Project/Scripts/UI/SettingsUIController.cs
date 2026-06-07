using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 运行时构建双栏配置面板：左侧主题导航，右侧具体编辑项（Tab 键开关）。
/// </summary>
[RequireComponent(typeof(GameSettingsManager))]
public class SettingsUIController : MonoBehaviour
{
    struct SliderBinding
    {
        public float min;
        public float max;
        public bool wholeNumbers;
        public string format;
        public System.Func<float> getter;
        public System.Action<float> setter;
    }

    struct ToggleBinding
    {
        public System.Func<bool> getter;
        public System.Action<bool> setter;
        public Text valueText;
    }

    enum SettingsTab
    {
        Driving,
        Deformation,
        Feedback,
        Environment,
        Session
    }

    static readonly Color BgOverlay = new Color(0.02f, 0.03f, 0.05f, 0.72f);
    static readonly Color PanelBg = new Color(0.06f, 0.07f, 0.09f, 0.94f);
    static readonly Color SidebarBg = new Color(0.04f, 0.05f, 0.07f, 0.98f);
    static readonly Color ContentBg = new Color(0.07f, 0.08f, 0.1f, 0.96f);
    static readonly Color NavNormal = new Color(0.1f, 0.11f, 0.14f, 0.85f);
    static readonly Color NavActive = new Color(0.32f, 0.34f, 0.38f, 0.95f);
    static readonly Color NavBorder = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color NavTextNormal = new Color(0.55f, 0.58f, 0.62f);
    static readonly Color NavTextActive = new Color(0.95f, 0.96f, 0.98f);
    static readonly Color RowDivider = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color ValueBoxBg = new Color(0.04f, 0.05f, 0.07f, 0.6f);
    static readonly Color ValueBoxBorder = new Color(1f, 1f, 1f, 0.22f);
    static readonly Color SectionText = new Color(0.62f, 0.65f, 0.7f);
    static readonly Color AccentText = new Color(0.78f, 0.86f, 0.96f);

    [SerializeField] bool buildOnStart = true;
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] float scrollSensitivity = 90f;

    GameSettingsManager settingsManager;
    GameUIManager uiManager;

    GameObject overlayRoot;
    GameObject panelRoot;
    Transform rightContentPage;
    Text categoryTitleText;
    Text tooltipText;
    Text infoText;

    readonly List<GameObject> navButtons = new List<GameObject>();
    readonly List<SliderBinding> activeSliders = new List<SliderBinding>();
    readonly List<ToggleBinding> activeToggles = new List<ToggleBinding>();
    SettingsTab currentTab = SettingsTab.Driving;
    int tooltipHoverCount;
    string activeTooltipKey;

    static Font uiFont;

    public Color RowHoverColor => new Color(0.14f, 0.16f, 0.2f, 0.95f);

    void Awake()
    {
        settingsManager = GetComponent<GameSettingsManager>();
        uiManager = FindObjectOfType<GameUIManager>();
    }

    void Start()
    {
        if (buildOnStart)
            BuildUI();

        if (settingsManager != null)
            settingsManager.SettingsChanged += RefreshAllControls;
    }

    void OnDestroy()
    {
        if (settingsManager != null)
            settingsManager.SettingsChanged -= RefreshAllControls;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && overlayRoot != null)
            SetPanelVisible(!overlayRoot.activeSelf);

        RefreshInfoText();
    }

    public void BuildUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[SettingsUIController] 未找到 Canvas，无法构建配置面板。");
            return;
        }

        if (overlayRoot != null)
            Destroy(overlayRoot);

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();

        overlayRoot = CreateStretchPanel(canvas.transform, "SettingsOverlay", BgOverlay);
        overlayRoot.SetActive(false);

        panelRoot = CreateCenterPanel(overlayRoot.transform, "SettingsPanel", 920f, 640f, PanelBg);
        CreateHeader(panelRoot.transform);
        CreateBody(panelRoot.transform);
        CreateFooter(panelRoot.transform);
        CreateToggleButton(canvas.transform);

        ShowTab(SettingsTab.Driving);
    }

    void SetPanelVisible(bool visible)
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(visible);
    }

    void CreateHeader(Transform parent)
    {
        CreateText(parent, "Title", "系统配置", 20, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(300f, 36f));

        Button closeBtn = CreateOutlineButton(parent, "CloseBtn", "关闭",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(72f, 32f));
        closeBtn.onClick.AddListener(() => SetPanelVisible(false));
    }

    void CreateBody(Transform parent)
    {
        GameObject body = new GameObject("Body", typeof(RectTransform));
        body.transform.SetParent(parent, false);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(16f, 56f);
        bodyRect.offsetMax = new Vector2(-16f, -56f);

        CreateLeftNav(body.transform);
        CreateRightPanel(body.transform);
    }

    void CreateLeftNav(Transform parent)
    {
        GameObject sidebar = CreateStretchPanel(parent, "Sidebar", SidebarBg);
        RectTransform sidebarRect = sidebar.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(0f, 0f);
        sidebarRect.anchorMax = new Vector2(0f, 1f);
        sidebarRect.pivot = new Vector2(0f, 0.5f);
        sidebarRect.sizeDelta = new Vector2(196f, 0f);
        sidebarRect.anchoredPosition = Vector2.zero;

        AddBorder(sidebar, NavBorder);

        (string label, SettingsTab tab)[] items =
        {
            ("驾驶物理", SettingsTab.Driving),
            ("形变损伤", SettingsTab.Deformation),
            ("视听反馈", SettingsTab.Feedback),
            ("环境道具", SettingsTab.Environment),
            ("场景控制", SettingsTab.Session)
        };

        navButtons.Clear();
        float y = -16f;
        foreach ((string label, SettingsTab tab) item in items)
        {
            Button btn = CreateNavButton(sidebar.transform, item.label, new Vector2(12f, y), new Vector2(172f, 46f));
            SettingsTab captured = item.tab;
            btn.onClick.AddListener(() => ShowTab(captured));
            AttachNavHover(btn.gameObject, item.label);
            navButtons.Add(btn.gameObject);
            y -= 54f;
        }
    }

    void CreateRightPanel(Transform parent)
    {
        GameObject right = CreateStretchPanel(parent, "RightPanel", ContentBg);
        RectTransform rightRect = right.GetComponent<RectTransform>();
        rightRect.anchorMin = Vector2.zero;
        rightRect.anchorMax = Vector2.one;
        rightRect.offsetMin = new Vector2(208f, 0f);
        rightRect.offsetMax = Vector2.zero;

        AddBorder(right, NavBorder);

        categoryTitleText = CreateText(right.transform, "CategoryTitle", "驾驶物理", 15, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -14f), new Vector2(-20f, 28f));
        categoryTitleText.fontStyle = FontStyle.Bold;
        categoryTitleText.color = NavTextActive;

        CreateDivider(right.transform, new Vector2(16f, -44f), new Vector2(-16f, -44f));

        GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(right.transform, false);
        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(0f, 0f);
        scrollRect.offsetMax = new Vector2(0f, -52f);
        scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 0f;
        layout.padding = new RectOffset(0, 0, 4, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = scrollSensitivity;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;

        rightContentPage = content.transform;
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        es.hideFlags = HideFlags.None;
    }

    public void ShowTooltip(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        tooltipHoverCount++;
        activeTooltipKey = key;
        UpdateTooltipDisplay();
    }

    public void HideTooltip()
    {
        tooltipHoverCount = Mathf.Max(0, tooltipHoverCount - 1);
        UpdateTooltipDisplay();
    }

    void UpdateTooltipDisplay()
    {
        if (tooltipText == null) return;

        if (tooltipHoverCount > 0)
        {
            tooltipText.text = SettingsDescriptions.Get(activeTooltipKey);
            tooltipText.color = AccentText;
        }
        else
        {
            tooltipText.text = SettingsDescriptions.DefaultHint;
            tooltipText.color = SectionText;
        }
    }

    void CreateFooter(Transform parent)
    {
        CreateHorizontalLine(parent, 48f);

        Button resetBtn = CreateOutlineButton(parent, "ResetDefaults", "恢复默认",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 12f), new Vector2(96f, 30f));
        resetBtn.onClick.AddListener(() => settingsManager.ResetToDefaults());

        Button saveBtn = CreateOutlineButton(parent, "SaveSettings", "保存配置",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-48f, 12f), new Vector2(96f, 30f));
        saveBtn.onClick.AddListener(() => settingsManager.SaveToPlayerPrefs());

        Button sceneBtn = CreateOutlineButton(parent, "ResetScene", "重置场景",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 12f), new Vector2(96f, 30f));
        sceneBtn.onClick.AddListener(() => settingsManager.ResetScene());

        tooltipText = CreateText(parent, "TooltipText", SettingsDescriptions.DefaultHint, 12, TextAnchor.UpperLeft,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 68f), new Vector2(-20f, 52f));
        tooltipText.color = SectionText;
        tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tooltipText.verticalOverflow = VerticalWrapMode.Overflow;

        infoText = CreateText(parent, "InfoText", "", 11, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(640f, 20f));
        infoText.color = AccentText;
    }

    void CreateToggleButton(Transform canvas)
    {
        Button btn = CreateOutlineButton(canvas, "OpenSettingsBtn", "配置 [Tab]",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(100f, 32f));
        btn.onClick.AddListener(() => SetPanelVisible(!overlayRoot.activeSelf));
    }

    void ShowTab(SettingsTab tab)
    {
        currentTab = tab;
        UpdateNavHighlight((int)tab);
        UpdateCategoryTitle(tab);

        ClearPage(rightContentPage);
        activeSliders.Clear();
        activeToggles.Clear();
        tooltipHoverCount = 0;
        UpdateTooltipDisplay();

        switch (tab)
        {
            case SettingsTab.Driving: BuildDrivingTab(rightContentPage); break;
            case SettingsTab.Deformation: BuildDeformationTab(rightContentPage); break;
            case SettingsTab.Feedback: BuildFeedbackTab(rightContentPage); break;
            case SettingsTab.Environment: BuildEnvironmentTab(rightContentPage); break;
            case SettingsTab.Session: BuildSessionTab(rightContentPage); break;
        }

        RefreshAllControls();
    }

    void UpdateCategoryTitle(SettingsTab tab)
    {
        if (categoryTitleText == null) return;
        categoryTitleText.text = tab switch
        {
            SettingsTab.Driving => "驾驶物理",
            SettingsTab.Deformation => "形变与损伤",
            SettingsTab.Feedback => "视听反馈",
            SettingsTab.Environment => "环境道具",
            SettingsTab.Session => "场景控制",
            _ => "配置"
        };
    }

    void UpdateNavHighlight(int activeIndex)
    {
        for (int i = 0; i < navButtons.Count; i++)
        {
            Image bg = navButtons[i].GetComponent<Image>();
            Text label = navButtons[i].GetComponentInChildren<Text>();
            if (bg == null || label == null) continue;

            bool active = i == activeIndex;
            bg.color = active ? NavActive : NavNormal;
            label.color = active ? NavTextActive : NavTextNormal;
            label.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    public void RefreshNavHighlight() => UpdateNavHighlight((int)currentTab);

    void BuildDrivingTab(Transform parent)
    {
        CreateSectionHeader(parent, "车辆参数");

        AddSliderRow(parent, "驱动力矩 (N·m)", 500f, 8000f, false, "F0",
            () => settingsManager.Settings.motorTorque,
            v => settingsManager.Settings.motorTorque = v);
        AddSliderRow(parent, "最大转向角 (°)", 5f, 45f, false, "F1",
            () => settingsManager.Settings.maxSteerAngle,
            v => settingsManager.Settings.maxSteerAngle = v);
        AddSliderRow(parent, "刹车力矩", 500f, 15000f, false, "F0",
            () => settingsManager.Settings.brakeTorque,
            v => settingsManager.Settings.brakeTorque = v);
        AddSliderRow(parent, "手刹力矩", 1000f, 35000f, false, "F0",
            () => settingsManager.Settings.handbrakeTorque,
            v => settingsManager.Settings.handbrakeTorque = v);
        AddSliderRow(parent, "抓地系数 μ", 0.1f, 3f, false, "F2",
            () => settingsManager.Settings.gripCoefficient,
            v => settingsManager.Settings.gripCoefficient = v);
        AddSliderRow(parent, "滚动阻力系数", 0.005f, 0.1f, false, "F3",
            () => settingsManager.Settings.rollingResistanceCoeff,
            v => settingsManager.Settings.rollingResistanceCoeff = v);
        AddSliderRow(parent, "空气阻力系数", 0.1f, 2f, false, "F2",
            () => settingsManager.Settings.airDragCoefficient,
            v => settingsManager.Settings.airDragCoefficient = v);
        AddSliderRow(parent, "车身质量 (kg)", 500f, 5000f, false, "F0",
            () => settingsManager.Settings.mass,
            v => settingsManager.Settings.mass = v);
        AddSliderRow(parent, "重心高度 Y", -2f, 0.5f, false, "F2",
            () => settingsManager.Settings.centerOfMassY,
            v => settingsManager.Settings.centerOfMassY = v);
        AddSliderRow(parent, "损伤敏感度", 0.1f, 2f, false, "F2",
            () => settingsManager.Settings.damageSensitivity,
            v => settingsManager.Settings.damageSensitivity = v);

        CreateSectionHeader(parent, "快速预设");
        CreatePresetRow(parent);
    }

    void BuildDeformationTab(Transform parent)
    {
        CreateSectionHeader(parent, "形变参数");

        AddSliderRow(parent, "形变冲量阈值", 100f, 3000f, false, "F0",
            () => settingsManager.Settings.deformThreshold,
            v => settingsManager.Settings.deformThreshold = v);
        AddSliderRow(parent, "最大凹陷深度 (m)", 0.05f, 1f, false, "F2",
            () => settingsManager.Settings.maxDeformDepth,
            v => settingsManager.Settings.maxDeformDepth = v);
        AddSliderRow(parent, "形变半径 (m)", 0.2f, 2f, false, "F2",
            () => settingsManager.Settings.deformRadius,
            v => settingsManager.Settings.deformRadius = v);
        AddSliderRow(parent, "衰减指数 falloff", 0.5f, 3f, false, "F2",
            () => settingsManager.Settings.falloff,
            v => settingsManager.Settings.falloff = v);
        AddSliderRow(parent, "累积比例", 0.5f, 1f, false, "F2",
            () => settingsManager.Settings.accumulateRatio,
            v => settingsManager.Settings.accumulateRatio = v);
        AddSliderRow(parent, "深度倍率", 1f, 10f, false, "F1",
            () => settingsManager.Settings.deformDepthMultiplier,
            v => settingsManager.Settings.deformDepthMultiplier = v);

        CreateSectionHeader(parent, "损伤计算");

        AddSliderRow(parent, "损伤冲量缩放", 0.05f, 1f, false, "F2",
            () => settingsManager.Settings.damageImpulseScale,
            v => settingsManager.Settings.damageImpulseScale = v);
        AddSliderRow(parent, "深度损伤权重", 0f, 1f, false, "F2",
            () => settingsManager.Settings.depthDamageWeight,
            v => settingsManager.Settings.depthDamageWeight = v);
        AddSliderRow(parent, "报废冲量阈值", 10000f, 100000f, false, "F0",
            () => settingsManager.Settings.totaledThreshold,
            v => settingsManager.Settings.totaledThreshold = v);
        AddSliderRow(parent, "每帧最大顶点数", 100f, 2000f, true, "F0",
            () => settingsManager.Settings.maxVerticesPerFrame,
            v => settingsManager.Settings.maxVerticesPerFrame = Mathf.RoundToInt(v));

        CreateSectionHeader(parent, "碰撞过滤");

        AddSliderRow(parent, "最小碰撞冲量", 50f, 2000f, false, "F0",
            () => settingsManager.Settings.minCollisionImpulse,
            v => settingsManager.Settings.minCollisionImpulse = v);
        AddSliderRow(parent, "最小上报速度 (m/s)", 0.1f, 5f, false, "F2",
            () => settingsManager.Settings.minReportSpeed,
            v => settingsManager.Settings.minReportSpeed = v);
    }

    void BuildFeedbackTab(Transform parent)
    {
        CreateSectionHeader(parent, "镜头抖动");

        AddToggleRow(parent, "启用镜头抖动",
            () => settingsManager.Settings.cameraShakeEnabled,
            v => settingsManager.Settings.cameraShakeEnabled = v);
        AddSliderRow(parent, "抖动最小冲量", 500f, 5000f, false, "F0",
            () => settingsManager.Settings.cameraShakeMinImpulse,
            v => settingsManager.Settings.cameraShakeMinImpulse = v);
        AddSliderRow(parent, "抖动幅度", 0f, 1.5f, false, "F2",
            () => settingsManager.Settings.cameraShakeAmplitude,
            v => settingsManager.Settings.cameraShakeAmplitude = v);

        CreateSectionHeader(parent, "碰撞音效");

        AddToggleRow(parent, "启用碰撞音效",
            () => settingsManager.Settings.audioEnabled,
            v => settingsManager.Settings.audioEnabled = v);
        AddSliderRow(parent, "主音量", 0f, 1f, false, "F2",
            () => settingsManager.Settings.audioMasterVolume,
            v => settingsManager.Settings.audioMasterVolume = v);
        AddSliderRow(parent, "轻撞阈值", 100f, 2000f, false, "F0",
            () => settingsManager.Settings.audioLightThreshold,
            v => settingsManager.Settings.audioLightThreshold = v);
        AddSliderRow(parent, "重撞阈值", 1000f, 10000f, false, "F0",
            () => settingsManager.Settings.audioHeavyThreshold,
            v => settingsManager.Settings.audioHeavyThreshold = v);

        CreateSectionHeader(parent, "碰撞粒子");

        AddToggleRow(parent, "启用碰撞粒子",
            () => settingsManager.Settings.vfxEnabled,
            v => settingsManager.Settings.vfxEnabled = v);
        AddSliderRow(parent, "粒子最小冲量", 50f, 2000f, false, "F0",
            () => settingsManager.Settings.vfxMinImpulse,
            v => settingsManager.Settings.vfxMinImpulse = v);
    }

    void BuildEnvironmentTab(Transform parent)
    {
        CreateSectionHeader(parent, "可倾倒道具");

        AddSliderRow(parent, "倾倒阈值倍率", 0.2f, 3f, false, "F2",
            () => settingsManager.Settings.propToppleThresholdScale,
            v => settingsManager.Settings.propToppleThresholdScale = v);
        AddSliderRow(parent, "倾倒力度倍率", 0.2f, 2f, false, "F2",
            () => settingsManager.Settings.propToppleForceScale,
            v => settingsManager.Settings.propToppleForceScale = v);

        CreateSectionHeader(parent, "说明");
        AddHintRow(parent, "阈值倍率越高，路灯/树木越难被撞倒；力度倍率越高，越容易倾倒。");
    }

    void BuildSessionTab(Transform parent)
    {
        CreateSectionHeader(parent, "场景");

        AddSliderRow(parent, "时间缩放 (慢动作)", 0.1f, 2f, false, "F2",
            () => settingsManager.Settings.timeScale,
            v => settingsManager.Settings.timeScale = v);
        AddToggleRow(parent, "显示 HUD",
            () => settingsManager.Settings.showHud,
            v => settingsManager.Settings.showHud = v);

        CreateSectionHeader(parent, "切换车辆");
        CreateVehicleButtons(parent);

        CreateSectionHeader(parent, "同步");
        AddActionRow(parent, "读取当前车辆参数", () =>
        {
            settingsManager.SyncFromActiveVehicle();
            ShowTab(SettingsTab.Driving);
        });
    }

    void CreatePresetRow(Transform parent)
    {
        AddActionRow(parent, "经济型预设", () => { settingsManager.ApplyPresetEconomy(); ShowTab(currentTab); });
        AddActionRow(parent, "运动型预设", () => { settingsManager.ApplyPresetSport(); ShowTab(currentTab); });
        AddActionRow(parent, "警用型预设", () => { settingsManager.ApplyPresetPolice(); ShowTab(currentTab); });
    }

    void CreateVehicleButtons(Transform parent)
    {
        if (uiManager == null || uiManager.VehicleCount == 0)
        {
            AddHintRow(parent, "未配置车辆列表");
            return;
        }

        for (int i = 0; i < uiManager.VehicleCount; i++)
        {
            int index = i;
            string name = uiManager.GetVehicleDisplayName(i);
            AddActionRow(parent, $"切换到 {name}", () =>
            {
                uiManager.SelectVehicle(index);
                settingsManager.SyncFromActiveVehicle();
            });
        }
    }

    void AddSliderRow(Transform parent, string label, float min, float max, bool wholeNumbers, string format,
        System.Func<float> getter, System.Action<float> setter)
    {
        SliderBinding binding = new SliderBinding
        {
            min = min,
            max = max,
            wholeNumbers = wholeNumbers,
            format = format,
            getter = getter,
            setter = setter
        };
        activeSliders.Add(binding);
        int index = activeSliders.Count - 1;

        GameObject row = CreateSettingRow(parent, label, out Text valueText);
        valueText.text = FormatValue(getter(), format);

        Slider slider = CreateInlineSlider(row.transform, min, max, wholeNumbers);
        AttachRowHover(slider.gameObject, label, false);
        SliderRowRefs refs = slider.gameObject.GetComponent<SliderRowRefs>();
        refs.bindingIndex = index;
        refs.valueText = valueText;
        refs.format = format;

        slider.SetValueWithoutNotify(Mathf.InverseLerp(min, max, getter()));
        slider.onValueChanged.AddListener(t =>
        {
            float v = wholeNumbers ? Mathf.Round(Mathf.Lerp(min, max, t)) : Mathf.Lerp(min, max, t);
            activeSliders[index].setter(v);
            settingsManager.ApplyAll();
            valueText.text = FormatValue(v, format);
        });
    }

    void AddToggleRow(Transform parent, string label, System.Func<bool> getter, System.Action<bool> setter)
    {
        ToggleBinding binding = new ToggleBinding { getter = getter, setter = setter };
        activeToggles.Add(binding);
        int index = activeToggles.Count - 1;

        GameObject row = CreateSettingRow(parent, label, out Text valueText);
        binding.valueText = valueText;
        activeToggles[index] = binding;
        valueText.text = getter() ? "开启" : "关闭";

        Button toggleBtn = CreateOutlineButton(row.transform, "ToggleBtn", "切换",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(64f, 28f));
        AttachRowHover(toggleBtn.gameObject, label, false);
        toggleBtn.onClick.AddListener(() =>
        {
            bool next = !activeToggles[index].getter();
            activeToggles[index].setter(next);
            settingsManager.ApplyAll();
            valueText.text = next ? "开启" : "关闭";
        });
    }

    void AddActionRow(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject row = CreateSettingRow(parent, label, out Text valueText);
        valueText.text = "—";

        Button btn = CreateOutlineButton(row.transform, "ActionBtn", "应用",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(64f, 28f));
        AttachRowHover(btn.gameObject, label, false);
        btn.onClick.AddListener(action);
    }

    void AddHintRow(Transform parent, string text)
    {
        GameObject row = new GameObject("HintRow", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 48f;

        Text hint = CreateText(row.transform, "Hint", text, 12, TextAnchor.UpperLeft,
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        hint.color = SectionText;
        hint.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    GameObject CreateSettingRow(Transform parent, string label, out Text valueText)
    {
        GameObject row = new GameObject($"Row_{label}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 44f;
        row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

        CreateText(row.transform, "Label", label, 13, TextAnchor.MiddleLeft,
            new Vector2(0f, 0f), new Vector2(0.52f, 1f), Vector2.zero, Vector2.zero);

        GameObject valueBox = new GameObject("ValueBox", typeof(RectTransform), typeof(Image));
        valueBox.transform.SetParent(row.transform, false);
        RectTransform boxRect = valueBox.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.52f, 0.5f);
        boxRect.anchorMax = new Vector2(0.52f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = new Vector2(0f, 0f);
        boxRect.sizeDelta = new Vector2(72f, 28f);
        valueBox.GetComponent<Image>().color = ValueBoxBg;
        AddBorder(valueBox, ValueBoxBorder);

        valueText = CreateText(valueBox.transform, "Value", "0", 12, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        valueText.color = AccentText;

        AttachRowHover(row, label);
        CreateDividerRow(parent);
        return row;
    }

    void AttachRowHover(GameObject row, string descriptionKey, bool highlightBackground = true)
    {
        SettingsRowHover hover = row.AddComponent<SettingsRowHover>();
        hover.descriptionKey = descriptionKey;
        hover.controller = this;
        hover.highlightBackground = highlightBackground;

        Image bg = row.GetComponent<Image>();
        if (bg != null)
            bg.raycastTarget = true;
    }

    void AttachNavHover(GameObject row, string descriptionKey)
    {
        AttachRowHover(row, descriptionKey, false);
        SettingsRowHover hover = row.GetComponent<SettingsRowHover>();
        if (hover != null)
            hover.restoreNavHighlight = true;
    }

    void CreateSectionHeader(Transform parent, string title)
    {
        GameObject header = new GameObject($"Section_{title}", typeof(RectTransform), typeof(LayoutElement));
        header.transform.SetParent(parent, false);
        header.GetComponent<LayoutElement>().preferredHeight = 32f;

        Text label = CreateText(header.transform, "Label", title.ToUpperInvariant(), 11, TextAnchor.LowerLeft,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-20f, -4f));
        label.color = SectionText;
        label.fontStyle = FontStyle.Bold;

        GameObject line = new GameObject("Line", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(header.transform, false);
        RectTransform lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 0f);
        lineRect.pivot = new Vector2(0.5f, 0f);
        lineRect.offsetMin = new Vector2(20f, 0f);
        lineRect.offsetMax = new Vector2(-20f, 1f);
        line.GetComponent<Image>().color = RowDivider;
    }

    void CreateDividerRow(Transform parent)
    {
        GameObject div = new GameObject("Divider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        div.transform.SetParent(parent, false);
        div.GetComponent<LayoutElement>().preferredHeight = 1f;
        div.GetComponent<Image>().color = RowDivider;
    }

    void RefreshAllControls()
    {
        if (rightContentPage == null) return;

        foreach (Slider slider in rightContentPage.GetComponentsInChildren<Slider>(true))
        {
            SliderRowRefs refs = slider.GetComponent<SliderRowRefs>();
            if (refs == null || refs.bindingIndex < 0 || refs.bindingIndex >= activeSliders.Count)
                continue;

            SliderBinding binding = activeSliders[refs.bindingIndex];
            float value = binding.getter();
            slider.SetValueWithoutNotify(Mathf.InverseLerp(binding.min, binding.max, value));
            if (refs.valueText != null)
                refs.valueText.text = FormatValue(value, refs.format);
        }

        for (int i = 0; i < activeToggles.Count; i++)
        {
            ToggleBinding binding = activeToggles[i];
            if (binding.valueText != null)
                binding.valueText.text = binding.getter() ? "开启" : "关闭";
        }
    }

    void RefreshInfoText()
    {
        if (infoText == null || settingsManager == null || overlayRoot == null || !overlayRoot.activeSelf)
            return;

        CarController car = settingsManager.GetActiveCar();
        VehicleState state = car != null ? car.GetComponent<VehicleState>() : null;
        CollisionEventRecorder recorder = settingsManager.GetRecorder();

        string vehicleName = car != null ? car.gameObject.name : "无";
        float speed = car != null ? car.GetSpeed() : 0f;
        int collisions = state != null ? state.CollisionCount : 0;
        float lastImpulse = recorder != null && recorder.LastEvent.Impulse > 0f
            ? recorder.LastEvent.Impulse
            : state != null ? state.LastImpulse : 0f;

        infoText.text = $"{vehicleName}  ·  {speed:F0} km/h  ·  碰撞 {collisions} 次  ·  最近冲量 {lastImpulse:F0}";
    }

    static string FormatValue(float value, string format)
    {
        return format switch
        {
            "F0" => value.ToString("F0"),
            "F1" => value.ToString("F1"),
            "F2" => value.ToString("F2"),
            "F3" => value.ToString("F3"),
            _ => value.ToString("F2")
        };
    }

    static void ClearPage(Transform page)
    {
        for (int i = page.childCount - 1; i >= 0; i--)
            Destroy(page.GetChild(i).gameObject);
    }

    static GameObject CreateStretchPanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateCenterPanel(Transform parent, string name, float width, float height, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = color;
        AddBorder(go, NavBorder);
        return go;
    }

    static void CreateHorizontalLine(Transform parent, float bottomOffset)
    {
        GameObject line = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(16f, bottomOffset);
        rect.offsetMax = new Vector2(-16f, bottomOffset + 1f);
        line.GetComponent<Image>().color = RowDivider;
    }

    static void CreateDivider(Transform parent, Vector2 start, Vector2 end)
    {
        GameObject line = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = start;
        rect.sizeDelta = new Vector2(end.x - start.x, 1f);
        line.GetComponent<Image>().color = RowDivider;
    }

    static void AddBorder(GameObject go, Color color)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Text text = obj.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = NavTextActive;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static Button CreateOutlineButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        Image image = go.GetComponent<Image>();
        image.color = NavNormal;

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = NavNormal;
        colors.highlightedColor = NavActive;
        colors.pressedColor = new Color(0.18f, 0.2f, 0.24f);
        colors.selectedColor = NavActive;
        button.colors = colors;

        AddBorder(go, NavBorder);

        Text text = CreateText(go.transform, "Text", label, 12, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.color = NavTextActive;

        return button;
    }

    static Button CreateNavButton(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        Button btn = CreateOutlineButton(parent, $"Nav_{label}", label,
            new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
        Text text = btn.GetComponentInChildren<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 13;
        return btn;
    }

    static Slider CreateInlineSlider(Transform row, float min, float max, bool wholeNumbers)
    {
        GameObject go = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(Image));
        go.transform.SetParent(row, false);
        Image hitArea = go.GetComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0.01f);
        hitArea.raycastTarget = true;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.62f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-8f, 0f);
        rect.sizeDelta = new Vector2(-32f, 14f);

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f);
        bg.GetComponent<Image>().raycastTarget = false;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        StretchFull(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        StretchFull(fillRect);
        fill.GetComponent<Image>().color = new Color(0.55f, 0.62f, 0.72f, 0.85f);
        fill.GetComponent<Image>().raycastTarget = false;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        StretchFull(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(4f, 0f);
        handleAreaRect.offsetMax = new Vector2(-4f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(10f, 16f);
        handle.GetComponent<Image>().color = NavTextActive;

        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        go.AddComponent<SliderRowRefs>();
        return slider;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    class SliderRowRefs : MonoBehaviour
    {
        public int bindingIndex = -1;
        public Text valueText;
        public string format = "F2";
    }
}
