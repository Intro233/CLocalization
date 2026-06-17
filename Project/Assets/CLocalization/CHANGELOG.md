# 更新日志 (CHANGELOG)

本文件记录 CLocalization 插件的版本演进。

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/)。

---

## [1.1.0] - 2026-06-17

迭代 1-3 的累计更新：修 Bug、编辑器缺口补全、功能真实性。

### 新增

#### 文本进阶（迭代 3）
- **命名占位符 `{name}`**：`LocalizationFormatter` 新增 `FormatNamed`/`FormatMixed`（正则匹配非数字占位，向后兼容 `{0}` 位置占位）。
- `Localization` 新增命名重载：`Get(key, IDictionary<string,object>)`、`Get(key, object)`（匿名对象属性反射）、`Get(key, IDictionary, params object[])`（混合）。
- `LocalizeText` 新增 `SetNamedArgs(IDictionary)` / `SetNamedArgs(object)`，支持命名/位置混合插值。
- **RTL 接入**：`LocalizeText.ApplyToTarget` 根据当前语言 `IsRightToLeft` 设置 TMP 的 `isRightToLeft`（传统 UI.Text 无原生 RTL，文档说明）。
- `SystemLanguageToCode` 补全越南语/泰语/土耳其语/印地语等映射（迭代 3 E3）。

#### 编辑器工具（迭代 2）
- **Key 重命名重构**：`Tools > CLocalization > Rename Key`，迁移 JSON + 批量更新组件引用 + Undo。
- **未使用 key 检测**：DiagnosticsTab 增加「未使用 Key」检测（扫组件字段 + 源码字面量）。
- **AssetPostprocessor 自动同步 Settings**：拖 JSON 进 Locales 目录自动更新 Settings.languages（merge 不删策略）。
- **KeysTab 搜索匹配翻译内容**（原仅匹配 key）。
- **三组件 Inspector 增强**：LocalizeSprite/AudioSource/Font 的 key 下拉选择器 + 校验（复用 `LocalizeKeyFieldDrawer`）。
- 共用 `LocalizationReferenceScanner`（扫场景/Prefab 组件引用 + 源码字面量）。

### 修复（迭代 1）
- **加载失败死循环**：`ApplyLanguage` 加载失败时不再持久化坏语言代码，`Initialize` 增加回退兜底链。
- **导入空字符串覆盖**：CSV 导入空单元格不再覆盖已有翻译；`AddedKeys` 按 key 去重（原每语言重复计数）。
- **删除语言即时删盘**：改为延迟删盘，`SaveAll` 才真删；工具栏显示「* 未保存」标记；重载前确认丢弃。
- **`Get(key, out bool found)` 新增**：区分命中与回退；统一 `TryGet`/`Get` 回退语义文档。
- **资源 miss 保留旧值**：LocalizeAudioSource/Sprite/Font 资源缺失时不清空，保留旧引用。
- **`.json` 通配符误匹配**：精确后缀过滤 + 文件排序（稳定 CSV 语言列顺序）。
- KeysTab 编辑后标记 dirty；SyncSettings 改用内存数据避免清待删集合。

### 已知限制（更新）
- 复数（Pluralization）**未实现且无抽象**（规划中，非"预留"）。
- RTL 仅设置 TMP 的 `isRightToLeft` 文本方向，不含 bidi 文本整形（阿拉伯语字符连写需 TMP 字体支持或调用方处理）。
- 未使用 key 检测对动态拼接的 key 可能误报。
- Addressables 加载为同步接口，异步（UniTask）规划在后续迭代。

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
