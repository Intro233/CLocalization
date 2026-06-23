# 更新日志 (CHANGELOG)

本文件记录 CLocalization 插件的版本演进。

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/)。

---

## [3.0.0] - 2026-06-19

**打包为 Unity UPM 包。** 插件从工程内源码重组为标准 Unity Package Manager 包。

### 改进
- **UPM 包结构**：插件代码从 `Assets/CLocalization/` 重组为 UPM 包根目录（Runtime/Editor/Resources/Samples~）。
- **package.json**：包名 `com.clocalization.plugin`，version 3.0.0，声明依赖（Newtonsoft/TMP/UniTask）+ samples（Demo）。
- **Runtime asmdef 移入 Runtime/ 子目录**（UPM 惯例，与 Editor/ 对称）。
- **Demo 移入 Samples~/Demo/**（~后缀默认不导入，用户通过 Package Manager「Import samples」按需导入）。
- **manifest.json 本地引用**：测试工程通过 `file:../../` 引用包根。

### 安装方式
- 本地：`manifest.json` 加 `"com.clocalization.plugin": "file:../../"`
- Git：`"com.clocalization.plugin": "https://github.com/.../CLocalization.git"`
- Package Manager 界面导入

### 不变项
- 所有 .meta 文件 GUID 保持不变（脚本/资源引用不断裂）。
- Resources 路径不变（包内 Resources.Load 正常工作）。
- asmdef 引用不变（名称引用，无需改 GUID）。

---

## [2.5.0] - 2026-06-19

资源引用解耦（路径化）。**资源映射从强引用改为路径存储，支持打 AB 分包。**

### 改进
- **资源映射表存路径**：`AssetMapping` 从 Object 强引用改为 `assetPath`(string) + `pathType`(AssetPathType) 存储。编辑期拖拽体验不变（ObjectField 预览），存储的是路径。运行时按路径加载。
- **LanguageInfo 字体/国旗改路径**：`tmpFontPath`/`fallbackFontPath`/`flagPath` + 路径类型，全部解耦强引用。
- **路径类型双兼容**：`AssetPathType.Resources`（Resources 相对路径，运行时 Resources.Load）+ `AssetPathType.FullPath`（完整工程路径，支持任意位置资源，打 AB 友好）。
- **资源可放任意位置**：不再限于 Resources 目录。在 Resources 下自动识别为 Resources 路径，其他位置存完整路径。
- **重新启用 Loader 加载层**：`ILocalizationLoader` 加 `LoadAssetByPath`/`LoadAssetByPathAsync`，默认 Resources 实现，可替换为 AssetBundle/Addressables。
- **资源加载缓存**：`Localization` 加 `_assetCache`（路径→资源），避免重复 Load。`ClearAssetCache()` 可清除。
- **旧数据自动迁移**：打开资源 Tab / 语言 Tab 时，检测旧 `asset` 强引用字段，自动转换为 `assetPath`。

### 新增
- `AssetPathType` 枚举（Resources / FullPath）
- `AssetMapping.assetPath`/`pathType` 字段 + `AssetMapBase.LookupPath`/`SetAssetPath`
- `Localization.LoadAssetByPath`/`ClearAssetCache`
- `AssetsTab.ConvertToStoredPath`/`MigrateLegacyAssetRefs`
- `LanguagesTab.DrawPathObjectField`/`MigrateLegacyFont`

### 已知边界
- FullPath 类型在运行时（打包后）需自定义 Loader（AssetBundle/Addressables），默认 Resources 实现对 FullPath 告警返回 null。
- 旧的 `LoadAsset<T>(key, code)` 接口方法保留但废弃（向后兼容）。

---

## [2.4.0] - 2026-06-19

语言级全局字体配置。**字体本地化从组件级改为语言级全局控制。**

### 新增
- **LanguageInfo 加全局字体字段**：每语言可配置 `tmpFont`（TMP_FontAsset）和 `fallbackFont`（传统 Font），在编辑窗口「语言」Tab 的「字体」折叠区配置。
- **LocalizeText 自动应用全局字体**：`ApplyToTarget` 切换语言时，若当前语言配了字体则自动应用到所有 LocalizeText 文本（TMP 优先，传统 Text 次之）。字体留空则保持原字体不变。

### 改进
- **「语言」Tab 加字体折叠配置**：每语言行加「字体」按钮，展开后直接配置 TMP_FontAsset / Font（用 SerializedObject 写回 Settings）。
- LocalizeText Inspector 加提示：「字体由语言全局配置控制」，并指引用户去语言 Tab 配置。
- LocalizeFont Inspector 加提示：「此组件为单点覆盖，优先级高于语言全局字体配置」。

### 优先级设计
1. LocalizeFont 组件（fontKey + FontAssetMap）——单点覆盖，最高优先
2. LanguageInfo 全局字体（tmpFont/fallbackFont）——LocalizeText 自动应用
3. 保持原字体——LanguageInfo 字体为 null 时不覆盖

### 不变项
- LocalizeFont 组件保留，逻辑不变（仍走 FontAssetMap）
- FontAssetMap 保留（供 LocalizeFont 单点覆盖用）
- OnLanguageChanged 事件不变（LocalizeText 已订阅）

---

## [2.3.0] - 2026-06-18

资源本地化可视化配置（映射表）。**重大改进：废弃目录约定，改用映射表配置。**

### 新增
- **资源映射表**：`SpriteAssetMap` / `AudioClipAssetMap` / `FontAssetMap`（三张 ScriptableObject，按资源类型分离），存储 key × 语言 → 资源引用。
- **「资源」Tab**：编辑窗口新增资源 Tab，可视化配置各语言资源：类型切换（Sprite/Audio/Font）、key 列表（搜索/增删）、右侧按语言拖入资源。自动保存到映射表。
- **资源查询改走映射表**：`Localization.GetAsset<T>` 按 T 类型从对应映射表查询当前语言资源（原 `Resources.Load` 目录约定废弃）。
- **Inspector 状态提示**：Localize 组件 Inspector 显示该 key 的映射状态（已配置语言数），提示去「资源」Tab 编辑。
- `LocalizationSetup.LoadOrCreateAssetMaps`：首次打开资源 Tab 自动创建三张映射表到 `Resources/CLocalization/AssetMaps/`。

### 废弃
- **目录约定加载方式**：旧版本按 `Resources/{assetsPath}/{语言}/{key}` 自动加载资源，现废弃。Loader 的 `LoadAsset`/`LoadAssetAsync` 保留接口但返回 null（资源查询由映射表接管）。
- `LocalizationEditorData.GetAssetsHintMessage` 改为提示去资源 Tab 配置（不再提示目录约定）。

### 不变项
- **组件零改动**：LocalizeSprite/AudioSource/Font 仍调 `Localization.GetAsset<T>(key)`，内部实现变更对调用方透明。
- **文本本地化**：完全不受影响（走 LocaleData JSON）。
- **ILocalizationLoader 接口**：LoadAsset 保留（返回 null），自定义 Loader 不受影响。

### 已知边界
- 映射表是 ScriptableObject（Resources），打包后不可热更新。需热更新资源需另行实现自定义 Loader。
- 映射表 Lookup 线性查找（条目通常几十到几百，可接受）。

---

## [2.2.5] - 2026-06-18

诊断检测性能优化：进度条 + 结果缓存。

### 改进
- **扫描进度条**：`ScanAllReferences`（Prefab/Scene 引用扫描）和 `ScanSourceCodeKeyLiterals`（源码字面量扫描）加 `DisplayCancelableProgressBar`，显示当前进度与正在扫描的资源，支持取消。解决大项目扫描几十秒无反馈的问题。
- **引用扫描结果缓存**：`ScanAllReferences`/`ScanSourceCodeKeyLiterals` 结果缓存，重复「运行检测」直接命中缓存（瞬时完成）。场景扫描的固有开销只首次承担。
- **自动失效缓存**：`AssetPostprocessor` 检测到 Prefab/Scene/Script 变化时自动 `InvalidateCache`，确保改动后下次检测刷新引用。
- **手动重扫**：诊断 Tab 加「重新扫描引用」按钮，强制失效缓存后重新检测。
- 取消扫描时不缓存部分结果（避免缓存不完整数据），下次重新扫描。

---

## [2.2.4] - 2026-06-18

翻译编辑体验优化：长文本/多行编辑弹窗。

### 新增
- **翻译编辑弹窗**（`TranslationEditPopup`）：词条单元格旁加「⤢」展开按钮，点击弹出独立大文本框编辑。解决表格单元格高度受限、长文本挤在一行不好编辑的问题。
  - 弹窗显示 Key + 语言 + 大 TextArea（自适应高度、自动换行、带滚动）+ 字符数。
  - 实时回写：编辑过程中每次输入即同步到内存数据，关闭后配合「保存全部」落盘。
  - 单元格仍保留单行 TextField 用于快速编辑短文本。

### 改进
- 单元格统一为单行 TextField（短文本快速编辑），移除原"长文本切 TextArea"逻辑（TextArea 在固定行高内仍挤压）。

---

## [2.2.3] - 2026-06-18

语言排序持久化 + 空白点击取消输入框聚焦。

### 修复
- **语言排序未持久化**：LanguagesTab 的 ↑↓ 调整顺序后，重新打开窗口顺序丢失（回到文件名字母序）。根因：`LoadAllLocales` 按 JSON 文件名排序加载，忽略用户在 Settings 中调整的顺序。修复：`LoadAllLocales` 加载后调 `SortBySettingsOrder`，按 `Settings.languages` 列表顺序重排（Settings 顺序由 SyncSettings 持久化），Settings 没有的语言放最后。
- **点击窗口空白处输入框不取消聚焦**：Unity IMGUI 默认 TextField 聚焦后点空白保持聚焦，体验不佳。新增 `HandleBlankClickUnfocus`：OnGUI 末尾检测未被任何控件消费的 MouseDown（即空白点击），调 `GUI.FocusControl(null)` 失焦。

---

## [2.2.2] - 2026-06-18

紧急修复：Excel 导出的 CSV 导入不进来。

### 修复
- **【根因】导入后调 Reload 把内存数据覆盖**：`ImportCsv` 在 `ImportFromCsv` 写入内存后调了 `window.Reload()`，而 Reload 从磁盘重新加载，**覆盖了刚导入的修改**，导致用户看到数据"没导进来"。改为调 `RefreshCaches`（只刷新 Tab 缓存、不读盘）+ `MarkDirty`（提示保存）。
- **CSV 分隔符写死逗号导致中文/德语等区域 Excel 导出导入失败**：中文版 Excel（及德语/法语等区域设置）另存 CSV 默认用分号 `;` 分隔，而 CsvUtil 写死用逗号 `,`，导致整行被当成一个字段、表头只解析出 1 列、所有数据导不进来。新增 `DetectSeparator`：读取时统计第一行逗号与分号出现次数，自动选多的作为分隔符。
- **UTF-8 BOM 残留到首字段**：`Read` 开头剥离 `\uFEFF`，防止表头首字段变成 `\uFEFFkey` 导致匹配失败。

---

## [2.2.1] - 2026-06-18

审查修复：4 个阻断级 + 7 个重要级缺陷（共 11 项）。

### 阻断级修复
- **Android + StreamingAssets 无法初始化**：新增 `Localization.InitializeAsync` / `InitializeAsync(settings, loader)`（异步加载初始+默认语言，全程主线程应用状态）；`LocalizationInitializer` 加 `useAsyncInitialization` 开关 + `InitializeAsync()` 方法；`ApplyLanguageAsync` 内异步预加载默认语言（不再用同步 LoadLocale 在 Android 崩溃）。
- **批量填充默认语言逻辑错误**：`KeysTab.DoFillMissingFromDefault` 改用 `settings.DefaultLanguageCode` 查找默认 locale（原误用 `locales[0]`，列表顺序不可靠）。
- **localesPath 变更丢语言**：Settings 面板检测路径变化，显示迁移预览 + 「迁移文件并应用路径」按钮（同模式下路径间迁移）。
- **路径非法字符无校验**：新增 `IsPathSafe` 校验（拒绝绝对路径、盘符、反斜杠、非法字符），错误时显示红色提示。

### 重要级修复
- **异步竞态（最后完成者覆盖）**：引入 `_applyEpoch` 请求序号；`SetLanguageAsync`/`SetLanguage`/早返回分支均递增 epoch；`ApplyLanguageAsync` 加载完成后校验 epoch，过期则丢弃（彻底解决连续异步切换、异步中插同步、异步中选当前三种竞态）。
- **桌面/iOS 异步缓存非线程安全**：`StreamingAssetsLocalizationLoader.LoadLocaleAsync` 在 `RunOnThreadPool` 读完文件后 `SwitchToMainThread` 再写 `_localeCache`（Dictionary 非线程安全）。
- **Initialize 不重置静态状态**：新增 `ResetState`，`Initialize`/`InitializeAsync` 首先重置 `_currentLocale`/`_defaultLocale` 等，避免换 Loader 后旧数据残留。
- **FormatMixed 命名值未转义大括号**：`FormatNamed` 替换命名值时调用 `EscapeBraces`（`{`→`{{`、`}`→`}}`），防止值含 `{0}` 被 string.Format 误解析。
- **KeysTab 行选中点击判定不可靠**：命中判定移进 `HorizontalScope` 内用 `rowScope.rect`，加 `Use()` 消费事件，避免点按钮误触发行选中。
- **KeysTab 每帧全量过滤排序**：新增 `_visibleKeysCache` + 脏标记，仅在 search/onlyMissing/sortColumn/sortDescending/数据量变化时重算，大数据量不再每帧卡顿。
- **迁移资源未提示**：Resources→StreamingAssets 迁移确认对话框与结果提示明确告知本地化资源（Sprite/Audio/Font）不迁移且 StreamingAssets 不支持加载，需手动处理。
- **列宽所有语言列共享**：改为按语言代码独立存储（`Dictionary<string,float>`），拖拽某一语言列只影响该列（原拖一个全变）。
- **KeyPickerWindow 搜索每按键全量 ToLowerInvariant**：构造时预计算每个 key 的小写形式缓存，搜索用 `IndexOf(OrdinalIgnoreCase)` 免分配匹配，万 key 输入不再卡顿。

### 技术细节
- epoch 用 `System.Threading.Interlocked.Increment`（long 在 32 位平台非原子）。
- 异步路径默认语言预加载（`PreloadDefaultLocaleAsync`）与同步路径（`PreloadDefaultLocaleSync` 带 NotSupportedException catch）分离，各自容错。
- `ApplyLoadedLocale` 不再内联同步 LoadLocale 预加载，改由调用方负责（避免异步路径误用同步）。
- 列宽独立：`GetLangColumnWidth`/`SetLangColumnWidth` 按语言 code 读写 `_langColumnWidths`，未命中用默认值 200。

---

## [2.2.0] - 2026-06-18

可配置资源加载方式（Resources / StreamingAssets）+ 自动迁移。

### 新增
- **AssetLoadMode 枚举**：`Resources`（默认）/ `StreamingAssets`，在 LocalizationSettings 配置。
- **路径完全可配**：LocalizationSettings 新增 `LocalesPath`/`AssetsPath` 字段（子路径可自定义，默认 `CLocalization/Locales`/`CLocalization/Assets`）。
- **StreamingAssetsLocalizationLoader**：从 StreamingAssets 加载语言 JSON。文本跨平台（编辑器/桌面 File.ReadAllText，Android UnityWebRequest）；资源（Sprite/Audio/Font）不支持并告警。
- **Localization.Initialize 工厂分支**：根据 `settings.AssetLoadMode` 自动选择 Resources 或 StreamingAssets Loader。
- **Loader 构造注入路径**：两个内置 Loader 改为接收 localesPath/assetsPath（保留无参构造走默认，向后兼容）。
- **自动迁移工具**（`LocalizationAssetMigrator`）：切换加载方式时把语言 JSON 从旧目录复制到新目录，删除旧文件，刷新 AssetDatabase。
- **Settings 面板**：LoadMode 下拉、路径配置、当前目录显示、迁移预览与按钮、刷新语言列表按钮。
- **AssetPostprocessor 适配**：监控两种模式的目录前缀，迁移期间也能感知变化。
- **动态文案**：Inspector 资源提示、KeysTab 目录提示按当前模式动态显示。

### 限制（StreamingAssets 模式）
- 仅支持文本本地化；Unity 资源（Sprite/Audio/Font）需 Resources 或自定义 Loader。
- Android 平台必须用 `SetLanguageAsync`（同步 LoadLocale 抛 NotSupportedException）。
- Project 窗口拖入 JSON 不自动同步（StreamingAssets 原始文本不被导入），需手动「刷新语言列表」。

### 向后兼容
- 默认仍为 Resources 模式，旧配置零迁移。
- `ResourcesLocalizationLoader` 无参构造保留（向后兼容旧代码）。
- Settings 资产（LocalizationSettings.asset）始终留在 Resources（运行时 Resources.Load 加载它）。

---

## [2.1.0] - 2026-06-18

编辑器窗口交互优化（排序、批量操作、导航、快捷键）。

### 新增
- **语言可排序**：LanguagesTab 每行加上移/下移按钮，调整语言顺序（影响 KeysTab 列顺序与 CSV 导出顺序）。
- **KeysTab 表头点击排序**：点击 Key 列头切换升/降序；点击语言列头按该语言翻译内容排序（方便译者核对某一语言）。当前排序列高亮 + ▲/▼ 指示。
- **可拖拽列宽**：KeysTab 表头右边缘可拖拽调整 key 列与语言列宽度。
- **仅看未翻译过滤**：搜索栏旁加开关，译者一键定位所有缺失翻译的 key。
- **复制 key 到剪贴板**：每行 key 旁加 ⧉ 按钮，快速复制 key。
- **多行文本编辑**：含换行符或超长的翻译自动切换为 TextArea，支持换行编辑。
- **批量操作**：KeysTab 加「填充空值(默认语言)」「删除过滤结果」批量按钮（作用于当前过滤结果）。
- **诊断跳转编辑**：DiagnosticsTab 缺失项每行加「编辑」按钮，点击切到词条 Tab 并定位高亮该 key。
- **快捷键**：Ctrl/Cmd+S 保存全部、Ctrl/Cmd+F 聚焦搜索、Delete 删除选中 key。
- **行选中高亮**：点击行选中高亮（蓝色），配合 Delete 快捷键与诊断跳转定位。

### 改进（Inspector UX）
- LocalizeKeyFieldDrawer 重构：key 选择器改为「输入框 + 选择按钮」+ 弹出搜索窗口（TreeView 虚拟滚动，上万 key 不卡）。
- 预览语言可切换：Inspector 预览区加预览语言下拉（跨 Drawer 共享），预览应用 formatArgs 插值。
- LocalizeTextDrawer 重构复用 LocalizeKeyFieldDrawer（技术债清理，4 个 Drawer 统一逻辑）。

---

## [2.0.0] - 2026-06-17

迭代 5：UniTask 异步加载架构升级（重大能力升级，向后兼容）。

### 新增
- **异步加载接口**：`ILocalizationLoader` 新增 `LoadLocaleAsync` / `LoadAssetAsync`（基于 UniTask），支持 Addressables / 热更新 / 远端下载。同步方法保留（Resources 场景）。
- **异步语言切换**：`Localization.SetLanguageAsync(code)` / `SetDefaultLanguageAsync()`，返回 `UniTask<bool>`。加载在后台，状态写入与事件触发在主线程（`UniTask.SwitchToMainThread` 保证线程安全）。
- `ResourcesLocalizationLoader` 异步方法用 `UniTask.FromResult` 包装同步结果。
- 依赖：引入 `com.cysharp.unitask`（Git URL）。

### 主线程安全
- 异步路径状态写入（`_currentLocale` 等）与 `OnLanguageChanged` 事件触发统一在主线程执行，异步 Loader 可安全地在后台线程加载。

### 向后兼容
- 同步 API（`SetLanguage` / `Initialize` / Resources 加载）完全不变；不使用异步的项目无需改动。
- 旧的同步 `ILocalizationLoader` 实现需补两个异步方法（Resources 实现已内置，Addressables 实现按需）。

### 破坏性变更
- `ILocalizationLoader` 接口新增两个异步方法签名：自定义实现的 Loader 需补充 `LoadLocaleAsync` / `LoadAssetAsync`（仅影响自写 Loader 的用户，内置 Resources 实现已就绪）。
- 依赖新增 UniTask 包（首次打开 Unity 需联网拉取）。

---

## [1.2.0] - 2026-06-17

迭代 4：性能优化（减少热路径 GC 分配与内存累积）。

### 性能优化
- **TextAsset 即时卸载**：`ResourcesLocalizationLoader.LoadLocale` 反序列化后立即 `Resources.UnloadAsset` 释放原始文本字节，避免切换语言越多越累积（C2）。
- **LanguageInfo 缓存**：`Localization.CurrentLanguage` 改为 O(1) 返回缓存（新增 `_currentLanguageInfo` 字段，`ApplyLanguage` 成功时赋值），原每次 O(n) 扫描 `FindLanguage`（C4）。
- **路径拼接优化**：`LocalizationPaths.GetLocalePath` 按 languageCode 缓存结果；`GetAssetPath` 改用 `string.Concat` 减少 3 段拼接的中间字符串分配（C3）。
- **Resolve 局部捕获**：`Localization.Resolve` 把当前/默认 locale 先捕获到局部变量，避免解析过程中被并发切换读到半途替换的引用（C5，健壮性 + 一致性）。

### 编辑器
- **KeysTab 分页**：超过 200 行时分页显示并带翻页控件，避免上万 key 时 OnGUI 卡死（D7，未重构 TreeView 的低风险方案）。

### 评估不做
- C1 资源缓存：`Resources.Load` 已有引擎级缓存，应用层再加缓存收益小且增加内存管理负担，故不加。

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
