# CLocalization 开发进度与剩余任务

> **用途**：本文件记录 CLocalization 多语言插件的开发进度、已确认决策、剩余迭代任务。
> 下次会话从本文件同步进度，可直接继续。
>
> **最后更新**：迭代 5 完成（UniTask 异步加载）—— **所有规划迭代全部完成**

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
| **迭代 3** | **功能真实性（命名占位符 + RTL + 修文档）** | ✅ 已完成 |
| **迭代 4** | **性能优化** | ✅ 已完成 |
| **迭代 5** | **UniTask 异步加载架构升级** | ✅ 已完成 |

> 🎉 所有规划迭代（阶段 0-4 + 迭代 1-5）全部完成。

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

### 迭代 3：功能真实性 + 命名占位符 —— 已完成
- **B1 命名占位符**：`LocalizationFormatter` 新增 `FormatNamed`/`FormatMixed`（正则匹配 `{name}`，向后兼容 `{0}`）；`Localization` 新增 3 个命名重载（IDictionary / 匿名对象 / 混合）；`LocalizeText` 新增 `SetNamedArgs(IDictionary)` 和 `SetNamedArgs(object)`，`ApplyLocalization` 支持命名/位置混合插值
- **B3 RTL 接入**：`LocalizeText.ApplyToTarget` 根据当前语言 `IsRightToLeft` 设置 TMP 的 `isRightToLeft`（传统 UI.Text 无原生 RTL，注释说明）
- **B2 文档修正**：CHANGELOG 明确复数"未实现且无抽象（非预留）"、RTL"仅设文本方向，不含 bidi 整形"
- **E3 映射补全**：`SystemLanguageToCode` 补全越南语/泰语/土耳其语/印地语/印尼语/波兰语等，葡语改 pt-BR
- **文档更新**：README 加占位符插值示例、命名重载 API 表、编辑器工具菜单速查；CHANGELOG 新增 [1.1.0] 版本块

> 已知限制：`Get(key, null)` 调用因多重载可能歧义，应避免（语义不明）。

### 迭代 4：性能优化 —— 已完成
- **C2** `ResourcesLocalizationLoader.LoadLocale` 反序列化后立即 `Resources.UnloadAsset(textAsset)` 释放原始文本字节（LocaleData 是独立托管对象，不依赖底层字节）
- **C4** `Localization` 新增 `_currentLanguageInfo` 缓存字段，`CurrentLanguage` getter 改为 O(1) 直接返回缓存（原每次 O(n) 调 `FindLanguage`），`ApplyLanguage` 成功时赋值
- **C3** `LocalizationPaths.GetLocalePath` 按 languageCode 缓存路径；`GetAssetPath` 改用 `string.Concat` 减少 3 段拼接的中间分配
- **C5** `Localization.Resolve` 把 `_currentLocale`/`_defaultLocale` 先捕获到局部变量，避免解析过程中被并发切换读到半途替换的引用（健壮性 + 一致性）
- **C1（评估不做）** Resources.Load 已有引擎级缓存，应用层再加缓存收益小且增加内存管理负担，故不加
- **D7** KeysTab 加分页（每页 200 行），超过时显示翻页控件，避免上万 key 时 OnGUI 卡死（未重构 TreeView，低风险方案）

---

## 五、剩余迭代任务（详细）

### 迭代 3：功能真实性 + 命名占位符（优先级：中）

### 迭代 3：功能真实性 + 命名占位符 —— ✅ 已完成

> 本节为历史记录。已交付：B1 命名占位符（`{name}`，正则实现，向后兼容 `{0}`）、B3 RTL 接入（TMP isRightToLeft）、B2 复数文档修正、E3 SystemLanguageToCode 映射补全。详见上方「已完成迭代详情」。

---

### 迭代 4：性能优化 —— ✅ 已完成

> 本节为历史记录。已交付：C2 TextAsset 卸载、C4 LanguageInfo 缓存、C3 路径拼接优化、C5 Resolve 局部捕获、D7 KeysTab 分页。C1 经评估不加（依赖引擎缓存）。详见上方「已完成迭代详情」。

---

### 迭代 5：UniTask 异步加载架构升级 —— ✅ 已完成

> 本节为历史记录。已交付：引入 UniTask 包、`ILocalizationLoader` 加异步方法、`ResourcesLocalizationLoader` 用 FromResult 实现、`Localization.SetLanguageAsync`/`ApplyLanguageAsync`、`UniTask.SwitchToMainThread` 保证主线程安全、README 异步用法文档。同步 API 完全向后兼容。

**已实现的关键点**：
- `ILocalizationLoader` 新增 `LoadLocaleAsync`/`LoadAssetAsync`（保留同步方法）
- `Localization` 抽取共享 `ApplyLoadedLocale`，同步/异步路径复用状态应用逻辑
- 异步路径：`await loader.LoadLocaleAsync` → `await UniTask.SwitchToMainThread()` → 主线程应用状态
- `ResourcesLocalizationLoader` 异步用 `UniTask.FromResult` 包装（Resources 瞬时操作）

**破坏性变更**：自写 `ILocalizationLoader` 实现需补两个异步方法（内置 Resources 实现已就绪）。

---

## 六、当前文件清单（迭代 5 结束时）

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

1. **所有规划迭代（阶段 0-4 + 迭代 1-5）已全部完成** 🎉
2. **下一步可选方向**（非规划内，按需）：
   - 真正接入 Addressables（提供 `AddressablesLocalizationLoader` 具体实现，需项目已装 Addressables 包）
   - 复数（Pluralization）/ 完整 RTL（bidi 文本整形）
   - LocalizeTextDrawer 复用迭代 2 抽取的 `LocalizeKeyFieldDrawer`（技术债 #1）
   - 验证 UniTask 异步路径（需用户打开 Unity 拉取 UniTask 包后测试 `SetLanguageAsync`）
3. **⚠️ 重要：用户首次打开 Unity 时**，需联网拉取 UniTask 包（manifest.json 已加 Git URL），等待解析完成。若 Git 未安装或网络不通，UniTask 包会安装失败，此时异步 API 不可用——可回退去掉 manifest 的 UniTask 行恢复纯同步。
4. 用户已验证：阶段 0-4 + 迭代 1-4 功能 OK，Demo 字体用 AlibabaPuHuiTi-2-75-SemiBold。迭代 5（异步）需用户打开 Unity 拉取 UniTask 后验证编译。
5. **不要重复已完成的工作**；迭代 1-5 的修复/功能/优化已落地，勿回退。
6. 已知限制：`Get(key, null)` 多重载歧义（迭代 3 引入），调用方应避免传 null。

---

## 九、菜单入口速查

| 菜单 | 功能 |
|---|---|
| `Tools > CLocalization > Create Settings Asset` | 生成配置资源 |
| `Tools > CLocalization > Localization Window` | 主编辑窗口 |
| `Tools > CLocalization > Rename Key` | key 重命名重构（迭代 2 新增） |
| `Tools > CLocalization > Demo > Create Demo Scene` | 生成演示场景 |
| `Edit > Project Settings > CLocalization` | 配置面板 |
