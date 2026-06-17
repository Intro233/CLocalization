# CLocalization 开发进度与剩余任务

> **用途**：本文件记录 CLocalization 多语言插件的开发进度、已确认决策、剩余迭代任务。
> 下次会话从本文件同步进度，可直接继续。
>
> **最后更新**：迭代 2 完成（编辑器缺口补全）

---

## 一、项目概况

- **工程**：Unity 多语言插件，位于 `D:\UGit\CLocalization\Project\Assets\CLocalization\`
- **Unity 版本**：2022.3.62f2 LTS（.NET Standard 2.1）
- **命名空间**：`CLocalization`（运行时）/ `CLocalization.Editor`（编辑器）/ `CLocalization.Demo`（示例）
- **程序集**：`CLocalization.Runtime.asmdef`（根目录）+ `CLocalization.Editor.asmdef`（Editor 子目录）
- **依赖**：`com.unity.nuget.newtonsoft-json` 3.2.1（运行时 JSON，支持 Dictionary）、TMP 3.0.7
- **仓库结构**：仓库根 `D:\UGit\CLocalization`，Unity 工程在其 `Project/` 子目录下

---

## 二、已确认的关键决策（勿改）

| 维度 | 决策 |
|---|---|
| 数据格式 | JSON 运行时存储 + CSV 导入导出（**仅 CSV，不引入 Excel 库**） |
| 资源加载 | `ILocalizationLoader` 抽象 + Resources 默认实现 + **UniTask 异步**（迭代 5 做） |
| 本地化范围 | 文本(Text/TMP)、Sprite、AudioClip、字体(Font/TMP_FontAsset) |
| 文本进阶 | 参数插值 `{0}` + 日期/货币/数字格式化 + **命名占位符 `{name}`**（迭代 3 做）；复数/RTL 暂不做，仅修文档对齐 |
| 默认语言 | zh-CN、en-US、ja-JP、ko-KR |
| API 风格 | 静态 API（`Localization.Get("key")`） |
| 代码规范 | 中文注释、asmdef 划分 Runtime/Editor |
| Excel | 仅 CSV 保持现状 |
| 异步加载 | UniTask（迭代 5，引入 UniTask 包） |

---

## 三、迭代进度总览

| 迭代 | 主题 | 状态 |
|---|---|---|
| 阶段 0 | 工程基础（Newtonsoft 包 / .gitignore / asmdef） | ✅ 已完成 |
| 阶段 1 | 运行时核心（静态 API / 配置 / 加载抽象 / 格式化 / 持久化） | ✅ 已完成 |
| 阶段 2 | UI 组件（Text/Sprite/Audio/Font + 自动刷新基类） | ✅ 已完成 |
| 阶段 3 | 编辑器工具（编辑窗口 / CSV 导入导出 / 诊断 / Inspector） | ✅ 已完成 |
| 阶段 4 | Demo 场景 + 文档 | ✅ 已完成 |
| **迭代 1** | **修 Bug + 数据安全** | ✅ 已完成 |
| **迭代 2** | **编辑器缺口补全** | ✅ 已完成 |
| **迭代 3** | 功能真实性（命名占位符 + 修文档） | ⬜ 待做 |
| **迭代 4** | 性能优化 | ⬜ 待做 |
| **迭代 5** | UniTask 异步加载架构升级 | ⬜ 待做 |

---

## 四、已完成迭代详情

### 阶段 0-4（初始交付）—— 全部完成
- 引入 Newtonsoft 包、修正 .gitignore 路径错位、创建 Runtime/Editor asmdef
- 运行时核心：`Localization` 静态入口、`LocalizationSettings`、`LocaleData`/`LanguageInfo`、`ILocalizationLoader`+`ResourcesLocalizationLoader`、`LocalizationFormatter`、`LocalizationPrefs`、`LocalizationInitializer`
- UI 组件：`LocalizeBase`、`LocalizeText`(TMP+UI.Text)、`LocalizeSprite`、`LocalizeAudioSource`、`LocalizeFont`
- 编辑器：`LocalizationWindow` 主窗口 + KeysTab/LanguagesTab/ImportExportTab/DiagnosticsTab、`CsvUtil`、`LocalizationImportExport`、`LocalizationSettingsProvider`、`LocalizeTextDrawer`、`LocalizationSetup`、`DemoSceneSetup`
- 4 语言 JSON 样例 + DemoController + README/CHANGELOG

### 迭代 1：修 Bug + 数据安全 —— 已完成
- **A3** 加载失败不持久化坏语言代码（`ApplyLanguage` 失败直接 return）+ `Initialize` 兜底链（初始→默认→首个可用）
- **A4** 导入空字符串不覆盖已有翻译 + `AddedKeys` 按 key 去重（`LocalizationImportExport.cs`）
- **A5** 删除语言延迟删盘（`_pendingDeleteCodes`）+ 工具栏「* 未保存」标记 + Reload 确认丢弃
- **E1** 新增 `Get(key, out bool found)` + 完善 `TryGet`/`Get` 文档语义说明
- **E2** 资源 miss 时保留旧引用不清空（`LocalizeAudioSource` 状态记录移到成功分支）
- **附** `.json` 精确后缀过滤 + 文件排序（`LocalizationEditorData.cs`）
- **附** KeysTab 编辑后标记 dirty
- **连带** `SyncSettings` 改用内存 `_locales` + `RefreshCaches()`（避免清待删集合）；新增 `GetCurrentLocales()`/`RefreshCaches()`

### 迭代 2：编辑器缺口补全 —— 已完成
- **D-ref** 共用引用扫描工具 `LocalizationReferenceScanner`（扫场景/Prefab 组件 + 源码字面量）
- **D1** key 重命名重构窗口 `KeyRenameWindow`（迁移 JSON + 批量更新组件引用 + Undo）—— 菜单 `Tools > CLocalization > Rename Key`
- **D2** 未使用 key 检测（加到 `DiagnosticsTab`，`DiagnosticsResult.UnusedKeys`）
- **D3** `LocalizationAssetPostprocessor`（JSON 增删自动同步 Settings，merge 不删策略）+ `LocalizationSetup.SyncSettingsFromLocales`
- **D4** KeysTab 搜索匹配翻译内容（不只 key）
- **D5** 三个组件 Drawer（`LocalizeSpriteDrawer`/`LocalizeAudioSourceDrawer`/`LocalizeFontDrawer`）+ 共用 `LocalizeKeyFieldDrawer`

---

## 五、剩余迭代任务（详细）

### 迭代 3：功能真实性 + 命名占位符（优先级：中）

**目标**：让注释/文档声称支持的功能真正可用，或修正文档对齐实现。

| 编号 | 任务 | 技术点 | 涉及文件 |
|---|---|---|---|
| **B1** | 实现命名占位符 `{name}` | 当前 `LocalizationFormatter.Format` 只用 `string.Format`（只支持 `{0}` 位置占位，遇 `{name}` 抛异常被吞）。需新增正则替换逻辑：匹配 `{name}` 用 `Dictionary<string,object>` 或命名参数填充。建议新增重载 `Get(string key, Dictionary<string,object> args)` 和 `Get(string key, object args)`（反射属性名）。 | `Runtime/Text/LocalizationFormatter.cs`、`Runtime/Core/Localization.cs`、`Runtime/Components/LocalizeText.cs`（formatArgs 可能需扩展为支持命名） |
| **B2-doc** | 修正复数相关注释/文档 | CHANGELOG/README 写了「复数预留扩展点」，实际**无任何抽象**。需改为「暂不支持，规划中」。或预留 `IPluralRuleProvider` 空接口。 | `CHANGELOG.md`、`README.md`、可选 `Runtime/Text/IPluralRuleProvider.cs` |
| **B3-doc** | 修正 RTL 相关注释/文档 | `LanguageInfo.IsRightToLeft` 字段被定义但**全仓库无代码消费**。需在 `LocalizeText.ApplyToTarget` 里根据 `IsRightToLeft` 设置 `tmpText.isRightToLeft = true`（TMP 支持），或明确文档「RTL 仅标记，需调用方处理」。 | `Runtime/Components/LocalizeText.cs`、`README.md` |
| **E3** | 补全 `SystemLanguageToCode` 映射 | `LocalizationPrefs.SystemLanguageToCode` 葡语只映射 pt-PT（巴西需 pt-BR），越南语/泰语/土耳其语/印地语回退 en-US。补全常见语言。 | `Runtime/Persistence/LocalizationPrefs.cs` |

**B1 命名占位符实现建议**：
```csharp
// LocalizationFormatter 新增方法
public static string FormatNamed(string template, CultureInfo culture, IDictionary<string,object> args)
{
    // 正则匹配 {name}，用 args[name].ToString(culture) 替换
    // 同时保留对 {0} 位置占位的兼容（可混合）
}
// Localization 新增
public static string Get(string key, IDictionary<string,object> namedArgs)
```
注意：Demo 和现有 JSON 用的都是 `{0}` 位置占位，实现命名占位符时要**向后兼容**，不破坏现有 `{0}` 用法。

---

### 迭代 4：性能优化（优先级：中低）

**目标**：减少热路径 GC 分配与内存累积。

| 编号 | 任务 | 技术点 | 涉及文件 |
|---|---|---|---|
| **C1** | 资源加载加缓存 | `LocalizeSprite/AudioSource/Font` 每次 `ApplyLocalization` 都重新 `Resources.Load`，无缓存。建议在 `ResourcesLocalizationLoader` 内加 `Dictionary<(code,key), Object>` 缓存（注意 Resources.Load 本身有内部缓存，主要优化点是路径拼接分配）。 | `Runtime/Loader/ResourcesLocalizationLoader.cs` |
| **C2** | TextAsset 加载后卸载 | `ResourcesLocalizationLoader.LoadLocale` 反序列化后没 `Resources.UnloadAsset(textAsset)`，原始字节驻留。建议反序列化后立即 `Resources.UnloadAsset`。 | `Runtime/Loader/ResourcesLocalizationLoader.cs` |
| **C3** | 路径字符串拼接优化 | `LocalizationPaths.GetAssetPath` 每次拼接 3 次分配。改用 `string.Concat`；高频 code 的 locale path 可缓存。 | `Runtime/Util/LocalizationPaths.cs` |
| **C4** | 缓存当前 LanguageInfo | `CurrentLanguage` getter 每次 O(n) 调 `FindLanguage`。在 `ApplyLanguage` 缓存 `_currentLanguageInfo` 静态字段。 | `Runtime/Core/Localization.cs` |
| **C5** | Resolve 局部变量捕获 | `Resolve` 把 `_currentLocale` 先读到局部变量再 `TryGetEntry`，避免读半途被换（健壮性 + 性能）。 | `Runtime/Core/Localization.cs` |
| **D7** | KeysTab 表格虚拟滚动 | OnGUI 手绘所有 key，上万 key 会卡。考虑用 `TreeView`（Unity 内置虚拟滚动）。 | `Editor/Window/KeysTab.cs` |

**优先级建议**：C2（内存泄漏风险）> C4（简单收益明确）> C3 > C1 > C5 > D7（工作量大，key 不多时可不做）。

---

### 迭代 5：UniTask 异步加载架构升级（优先级：按需，改动最大）

**目标**：真正接通 Addressables/热更新能力。

> ⚠️ 这是架构性改动，影响运行时核心，需谨慎。前置：通过 Package Manager 引入 UniTask 包。

| 步骤 | 任务 | 技术点 |
|---|---|---|
| 1 | 引入 UniTask 包 | manifest.json 加 `com.cysharp.unitask`，asmdef 加引用 |
| 2 | `ILocalizationLoader` 加异步接口 | 新增 `UniTask<LocaleData> LoadLocaleAsync(string code)` 和 `UniTask<T> LoadAssetAsync<T>(...)`，保留同步方法 |
| 3 | `ResourcesLocalizationLoader` 实现异步 | 用 `UniTask.FromResult` 包装同步结果，满足新接口 |
| 4 | `Localization` 加 `ApplyLanguageAsync` | `ApplyLanguage` 的异步版本，`await loader.LoadLocaleAsync`；同步 `SetLanguage` 保留（内部可调异步并 fire-and-forget 或阻塞） |
| 5 | 多线程安全（A2） | 异步回调可能在后台线程写 `_currentLocale`。需把状态写入收敛到主线程（UniTask 主线程切换），或加同步。见审查 #A2 |
| 6 | （可选）提供 Addressables 实现示例 | 新增 `AddressablesLocalizationLoader` 作为可选实现，证明接口可用 |
| 7 | 文档更新 | README 说明异步用法、Addressables 接入方式 |

**关键风险**：
- `ApplyLanguage` 现在是同步的，`Initialize` 和 `SetLanguage` 都依赖它。改异步后，`SetLanguage` 的调用方若不 await，可能出现「切换中」的中间态。需设计好同步/异步双 API 并明确文档。
- 多线程写静态状态（#A2）——必须保证 `Localization` 的可变字段写入在主线程（用 `UniTask.SwitchToMainThread` 或把异步加载结果通过主线程回调应用）。

---

## 六、当前文件清单（迭代 2 结束时）

### Runtime（16 个 .cs）
```
Runtime/Core/      Localization.cs, LocalizationSettings.cs, LocaleData.cs, LanguageInfo.cs, LocalizationInitializer.cs
Runtime/Loader/    ILocalizationLoader.cs, ResourcesLocalizationLoader.cs
Runtime/Text/      LocalizationFormatter.cs
Runtime/Components/ LocalizeBase.cs, LocalizeText.cs, LocalizeSprite.cs, LocalizeAudioSource.cs, LocalizeFont.cs
Runtime/Persistence/ LocalizationPrefs.cs
Runtime/Util/      LocalizationLog.cs, LocalizationPaths.cs
```

### Editor（19 个 .cs）
```
Editor/Window/     LocalizationWindow.cs, KeysTab.cs, LanguagesTab.cs, ImportExportTab.cs, DiagnosticsTab.cs, KeyRenameWindow.cs(新)
Editor/Services/   LocalizationEditorData.cs, LocalizationImportExport.cs, LocalizationReferenceScanner.cs(新)
Editor/Importer/   CsvUtil.cs
Editor/Settings/   LocalizationSettingsProvider.cs
Editor/Inspector/  LocalizeTextDrawer.cs, LocalizeKeyFieldDrawer.cs(新), LocalizeSpriteDrawer.cs(新), LocalizeAudioSourceDrawer.cs(新), LocalizeFontDrawer.cs(新)
Editor/Setup/      LocalizationSetup.cs, DemoSceneSetup.cs, LocalizationAssetPostprocessor.cs(新)
```

### 数据与 Demo
```
Resources/CLocalization/Locales/ zh-CN.json, en-US.json, ja-JP.json, ko-KR.json
Resources/CLocalization/         AlibabaPuHuiTi-2-75-SemiBold.ttf + .asset(TMP字体)
Demo/                            DemoController.cs, DemoScene.unity
```

### 文档
- `README.md`、`CHANGELOG.md`（位于 `Assets/CLocalization/`，内容为迭代 1 之前，迭代 3 时需更新）

---

## 七、已知技术债 / 待修小项（可穿插处理）

1. **`LocalizeTextDrawer` 未复用 `LocalizeKeyFieldDrawer`**：迭代 2 抽取了共用 Drawer，但旧的 `LocalizeTextDrawer` 仍是独立实现。为一致性可重构，但不紧急（已工作）。
2. **`KeyRenameWindow` 的 `Confirm`/`ConfirmAndRename` 方法冗余**：D1 实现里有两个几乎相同的方法（`ConfirmAndRename` 只调 `Confirm`），可合并清理。
3. **`LocalizationReferenceScanner` 对 Scene 的扫描可靠性**：用 `AssetDatabase.LoadAllAssetsAtPath` 扫 Scene，对复杂场景可能漏组件。若需要可靠 Scene 扫描，应改用 `EditorSceneManager.OpenScene` + `GetComponentsInChildren`（开销大）。
4. **DiagnosticsTab 未使用 key 的源码字面量扫描可能误报**：动态拼接的 key（如 `Localization.Get("ui." + name)`）无法被静态扫描识别，会误报为未使用。文档已注明此限制。
5. **`LanguagesTab` 同步用 `ApplyModifiedPropertiesWithoutUndo`**：增删语言不可 Ctrl+Z。审查建议改 `ApplyModifiedProperties` 保留 Undo（迭代 1 的 A5 上下文）。

---

## 八、下次会话快速继续指引

1. **读本文档**了解进度与决策
2. **下一个任务是迭代 3**（命名占位符 + 修文档）
3. 关键文件优先读：
   - `Runtime/Text/LocalizationFormatter.cs`（B1 命名占位符实现位置）
   - `Runtime/Core/Localization.cs`（Get 重载新增）
   - `CHANGELOG.md` / `README.md`（B2/B3 文档修正）
4. 用户已验证：阶段 0-4 + 迭代 1 + 迭代 2 功能 OK，Demo 字体用 AlibabaPuHuiTi-2-75-SemiBold，按钮点击/语言切换正常
5. **不要重复已完成的工作**；迭代 1/2 的 bug 修复已落地，勿回退

---

## 九、菜单入口速查

| 菜单 | 功能 |
|---|---|
| `Tools > CLocalization > Create Settings Asset` | 生成配置资源 |
| `Tools > CLocalization > Localization Window` | 主编辑窗口 |
| `Tools > CLocalization > Rename Key` | key 重命名重构（迭代 2 新增） |
| `Tools > CLocalization > Demo > Create Demo Scene` | 生成演示场景 |
| `Edit > Project Settings > CLocalization` | 配置面板 |
