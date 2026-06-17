using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CLocalization.Demo
{
    /// <summary>
    /// Demo 演示控制器。运行时动态构建一套 UI，演示 CLocalization 插件的核心能力：
    /// 1. 语言切换（中/英/日/韩 4 个按钮）
    /// 2. 文本本地化（标题、按钮、提示）
    /// 3. 参数插值（含数字/货币/日期格式化）
    /// 4. 组件自动刷新（切换语言无需手动刷新）
    ///
    /// 使用方式：
    ///   - 在空场景新建 GameObject，挂上本组件即可（会自动初始化本地化系统）
    ///   - 或运行菜单 Tools/CLocalization/Demo > Create Demo Scene 辅助创建
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("本地化配置资源。留空则从 Resources/CLocalization/LocalizationSettings 加载。")]
        [SerializeField] private LocalizationSettings settings;

        /// <summary>显示当前语言的标签。</summary>
        private TMP_Text _currentLangLabel;
        /// <summary>显示插值文本的标签。</summary>
        private TMP_Text _parameterText;
        /// <summary>显示格式化（数字/货币/日期）的标签。</summary>
        private TMP_Text _formatText;
        /// <summary>语言按钮映射：语言代码 → 按钮 Image，用于切换时更新选中态高亮。</summary>
        private readonly System.Collections.Generic.Dictionary<string, Image> _langButtons = new System.Collections.Generic.Dictionary<string, Image>();

        /// <summary>Demo 使用的 TMP 字体（阿里巴巴普惠体）。加载失败则回退 TMP 默认字体。</summary>
        private TMP_FontAsset _demoFont;

        /// <summary>未选中按钮底色。</summary>
        private static readonly Color BtnNormalColor = new Color(0.20f, 0.50f, 0.90f, 1f);
        /// <summary>当前选中按钮底色（橙色高亮）。</summary>
        private static readonly Color BtnSelectedColor = new Color(0.95f, 0.55f, 0.15f, 1f);

        private void Awake()
        {
            // 确保本地化系统已初始化
            if (!Localization.IsInitialized)
            {
                LocalizationSettings s = settings;
                if (s == null)
                {
                    s = Resources.Load<LocalizationSettings>("CLocalization/LocalizationSettings");
                }
                if (s != null) Localization.Initialize(s);
            }

            // 加载 Demo 字体（阿里巴巴普惠体），失败回退 null（TMP 用默认字体）
            _demoFont = Resources.Load<TMP_FontAsset>("AlibabaPuHuiTi-2-75-SemiBold");
            if (_demoFont == null)
            {
                Debug.LogWarning("[CLoc] 未找到 AlibabaPuHuiTi-2-75-SemiBold 字体，Demo 将使用 TMP 默认字体。");
            }

            BuildUI();
        }

        private void OnEnable()
        {
            Localization.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= HandleLanguageChanged;
        }

        /// <summary>语言切换回调：刷新动态内容（插值文本、格式化文本、按钮选中态）。</summary>
        private void HandleLanguageChanged(LanguageInfo lang)
        {
            RefreshDynamic();
            UpdateLangLabel();
            UpdateButtonStates();
        }

        private void Start()
        {
            UpdateLangLabel();
            RefreshDynamic();
        }

        /// <summary>刷新依赖运行时参数的内容（插值与格式化示例）。</summary>
        private void RefreshDynamic()
        {
            if (!Localization.IsInitialized) return;

            // 参数插值示例：你好,{0}！欢迎来到 {1}。
            _parameterText.text = Localization.Get("demo.text.parameter", "Player", "CLocalization");

            // 格式化示例：数字/货币/日期 按当前语言区域显示
            int number = 1234567;
            double price = 88.5;
            var today = System.DateTime.Now;
            CultureInfo culture = Localization.CurrentCulture;

            string numStr = number.ToString("N0", culture);
            string priceStr = price.ToString("C", culture);
            string dateStr = today.ToString("D", culture);
            _formatText.text = $"{numStr}\n{priceStr}\n{dateStr}";
        }

        /// <summary>更新当前语言标签。</summary>
        private void UpdateLangLabel()
        {
            if (_currentLangLabel == null) return;
            string label = Localization.CurrentLanguage?.DisplayName ?? Localization.CurrentLanguageCode;
            _currentLangLabel.text = $"{Localization.Get("demo.current")}: {label} ({Localization.CurrentLanguageCode})";
        }

        // ---------- UI 构建 ----------

        /// <summary>动态构建整套演示 UI。</summary>
        private void BuildUI()
        {
            // Canvas
            var canvasGo = new GameObject("DemoCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            // 背景（半透明深色，便于阅读）
            var bgGo = CreateUIObject("Background", canvasGo.transform);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            bgImage.raycastTarget = false; // 背景不接收点击，避免任何潜在拦截
            StretchToParent(bgImage.rectTransform);

            // 内容垂直布局容器
            var contentGo = CreateUIObject("Content", canvasGo.transform);
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(60, 60, 50, 50);
            contentLayout.spacing = 24;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            StretchToParent(contentGo.GetComponent<RectTransform>());

            // 标题（使用 LocalizeText 组件演示自动刷新）
            var title = CreateTMPText("Title", contentGo.transform, 42, FontStyles.Bold, Color.white);
            var titleLoc = title.gameObject.AddComponent<LocalizeText>();
            titleLoc.SetKey("demo.title");
            SetHeight(title.rectTransform, 56);

            // 副标题
            var subtitle = CreateTMPText("Subtitle", contentGo.transform, 22, FontStyles.Normal, new Color(0.8f, 0.85f, 0.9f));
            var subLoc = subtitle.gameObject.AddComponent<LocalizeText>();
            subLoc.SetKey("demo.subtitle");
            SetHeight(subtitle.rectTransform, 32);

            // 当前语言标签（动态更新，不挂 LocalizeText）
            _currentLangLabel = CreateTMPText("CurrentLang", contentGo.transform, 24, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
            SetHeight(_currentLangLabel.rectTransform, 34);

            // 语言切换按钮组
            var buttonRow = CreateUIObject("ButtonRow", contentGo.transform);
            var btnLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 16;
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.childControlWidth = true;
            btnLayout.childControlHeight = true;
            btnLayout.childForceExpandWidth = true;
            btnLayout.childForceExpandHeight = false;
            SetHeight(buttonRow.GetComponent<RectTransform>(), 56);

            CreateLangButton(buttonRow.transform, "中文", "zh-CN");
            CreateLangButton(buttonRow.transform, "English", "en-US");
            CreateLangButton(buttonRow.transform, "日本語", "ja-JP");
            CreateLangButton(buttonRow.transform, "한국어", "ko-KR");

            // 参数插值示例
            _parameterText = CreateTMPText("ParameterDemo", contentGo.transform, 28, FontStyles.Normal, Color.white);
            SetHeight(_parameterText.rectTransform, 40);

            // 格式化示例标签
            var formatLabel = CreateTMPText("FormatLabel", contentGo.transform, 18, FontStyles.Bold, new Color(0.6f, 0.8f, 1f));
            var formatLabelLoc = formatLabel.gameObject.AddComponent<LocalizeText>();
            formatLabelLoc.SetKey("demo.format");
            SetHeight(formatLabel.rectTransform, 26);

            // 格式化内容（动态更新）
            _formatText = CreateTMPText("FormatDemo", contentGo.transform, 24, FontStyles.Normal, Color.white);
            SetHeight(_formatText.rectTransform, 100);

            // 操作提示
            var hint = CreateTMPText("Hint", contentGo.transform, 18, FontStyles.Italic, new Color(0.7f, 0.7f, 0.7f));
            var hintLoc = hint.gameObject.AddComponent<LocalizeText>();
            hintLoc.SetKey("demo.hint");
            SetHeight(hint.rectTransform, 60);
        }

        /// <summary>创建一个语言切换按钮。</summary>
        private void CreateLangButton(Transform parent, string displayText, string langCode)
        {
            var go = CreateUIObject("Btn_" + langCode, parent);
            var image = go.AddComponent<Image>();
            image.color = Localization.CurrentLanguageCode == langCode ? BtnSelectedColor : BtnNormalColor;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = BtnNormalColor;
            colors.highlightedColor = new Color(0.35f, 0.65f, 1.0f, 1f);
            colors.pressedColor = new Color(0.12f, 0.32f, 0.62f, 1f);
            colors.selectedColor = BtnNormalColor;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            // 关键：给按钮自身设置 LayoutElement 的 preferredHeight，
            // 否则父级 HorizontalLayoutGroup（childForceExpandHeight=false）会把按钮高度算成 0，导致点不到。
            SetHeight(go.GetComponent<RectTransform>(), 56);

            // 按钮文字（关键：关闭 raycastTarget，避免子物体拦截点击导致 Button 收不到事件）
            var tmp = CreateTMPText("Text", go.transform, 24, FontStyles.Bold, Color.white);
            tmp.text = displayText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // 让点击穿透到按钮 Image，UGUI Button 才能响应
            StretchToParent(tmp.rectTransform);

            // 注册到映射，供切换语言时更新选中态
            _langButtons[langCode] = image;

            btn.onClick.AddListener(() =>
            {
                Localization.SetLanguage(langCode);
            });
        }

        /// <summary>更新所有语言按钮的选中态高亮。</summary>
        private void UpdateButtonStates()
        {
            string current = Localization.CurrentLanguageCode;
            foreach (var kv in _langButtons)
            {
                if (kv.Value == null) continue;
                kv.Value.color = kv.Key == current ? BtnSelectedColor : BtnNormalColor;
            }
        }

        // ---------- UI 辅助方法 ----------

        /// <summary>创建带 RectTransform 的 UI 子物体。</summary>
        private GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>创建一个 TMP 文本。</summary>
        private TMP_Text CreateTMPText(string name, Transform parent, float fontSize, FontStyles style, Color color)
        {
            var go = CreateUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            // 应用阿里巴巴普惠体（加载失败时 _demoFont 为 null，TMP 自动用默认字体）
            if (_demoFont != null) tmp.font = _demoFont;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = true;
            return tmp;
        }

        /// <summary>使 RectTransform 拉伸铺满父节点。</summary>
        private void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>设置布局元素的最小高度。</summary>
        private void SetHeight(RectTransform rt, float height)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }
    }
}
