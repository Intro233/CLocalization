# 更新日志 (CHANGELOG)

本文件记录 CLocalization 插件的版本演进。

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/)。

---

## [1.0.0] - 2026-06-17

首个完整版本。

### 新增

#### 运行时核心
- `Localization` 静态入口：文本查询（`Get`/`TryGet`）、资源加载（`GetAsset<T>`）、语言切换（`SetLanguage`）、`OnLanguageChanged` 事件。
- `LocalizationSettings` ScriptableObject：语言列表、默认语言、回退策略、持久化等配置。
- `LocaleData` / `LanguageInfo`：基于 `Dictionary<string,string>` 的词条容器与语言元数据。
- `ILocalizationLoader` 抽象 + `ResourcesLocalizationLoader` 默认实现（带缓存）。
- `LocalizationFormatter`：`{0}` 参数插值 + 按 `CultureInfo` 的数字/货币/日期格式化。
- `LocalizationPrefs`：PlayerPrefs 持久化 + 系统语言检测 + 初始化语言优先级解析。
- `LocalizationInitializer`：挂场景即可自动初始化的 MonoBehaviour。

#### UI 组件
- `LocalizeBase` 抽象基类：统一「订阅语言切换事件 → 自动刷新」生命周期。
- `LocalizeText`：TextMeshPro 与 UI.Text 双支持，支持参数插值。
- `LocalizeSprite`：Image 图片按语言切换。
- `LocalizeAudioSource`：AudioSource 按语言切换，智能恢复播放状态。
- `LocalizeFont`：Font / TMP_FontAsset 字体按语言切换。

#### 编辑器工具
- `LocalizationWindow` 主窗口：词条 / 语言 / 导入导出 / 诊断 四个 Tab。
- `KeysTab`：词条编辑表（搜索、增删 key、单元格编辑、空值高亮）。
- `LanguagesTab`：语言增删，自动同步 Settings 资源。
- `ImportExportTab`：CSV 导入导出 + 预览。
- `DiagnosticsTab`：翻译完整性检测（各语言覆盖率进度条 + 缺失明细）。
- `CsvUtil`：零依赖 CSV 读写（RFC 4180 兼容，UTF-8 BOM，Excel 中文友好）。
- `LocalizationImportExport`：CSV ↔ JSON 转换服务。
- `LocalizationSettingsProvider`：Project Settings > CLocalization 配置面板。
- `LocalizeTextDrawer`：LocalizeText Inspector 增强（key 下拉选择 + 存在性校验 + 预览）。
- `LocalizationSetup`：一键生成 Settings 资源。
- `DemoSceneSetup`：一键生成 Demo 场景。

#### 示例与数据
- 4 语言预置词条：`zh-CN.json` / `en-US.json` / `ja-JP.json` / `ko-KR.json`。
- `DemoController`：运行时动态构建演示 UI（语言切换 + 文本 + 插值 + 格式化示例）。
- README.md 使用文档（中文）。

### 依赖
- `com.unity.nuget.newtonsoft-json` 3.2.1（运行时 JSON 序列化，支持 Dictionary）。

### 已知限制
- `KeysTab` 搜索目前仅匹配 key 文本，未匹配翻译内容。
- 复数（Pluralization）与 RTL（从右到左排版）未实现，预留扩展位。
- Addressables 加载实现为接口预留，尚未提供具体实现。
