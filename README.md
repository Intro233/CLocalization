# CLocalization

一个轻量级、易用的 Unity 多语言（本地化）插件。

支持文本、图片、音频、字体的本地化，提供完整的编辑器工具与 Excel/CSV 协作工作流。
运行时依赖 Newtonsoft.Json（JSON 序列化）与 UniTask（异步加载），适合中小型到中大型项目。

---

## 📦 安装（UPM 包）

本插件已打包为标准 Unity Package Manager (UPM) 包。

### 方式一：本地引用（开发期）

在测试工程的 `Packages/manifest.json` 加：

```json
"com.clocalization.plugin": "file:../../"
```

（路径指向包根，即包含 `package.json` 的目录）

### 方式二：Git 引用（发布后）

```json
"com.clocalization.plugin": "https://github.com/<your-repo>/CLocalization.git"
```

### 方式三：Package Manager 界面

`Window > Package Manager > + > Add package from git URL`，输入仓库地址。

### 依赖

安装后 Unity 自动解析依赖：`com.unity.nuget.newtonsoft-json`、`com.unity.textmeshpro`、`com.cysharp.unitask`。

### 导入 Demo 示例

Package Manager 窗口选中 CLocalization → 「Samples」→ 「Import」导入 Demo 示例场景。

---

## ✨ 特性

- **文本本地化**：TextMeshPro 与传统 UI.Text 双支持，参数插值（`{0}` 位置占位 / `{name}` 命名占位，可混合），日期/货币/数字按语言区域格式化，RTL（从右到左）方向支持。
- **资源本地化**：Sprite、AudioClip 可按语言切换（映射表配置）。
- **字体本地化（全局）**：每种语言可配置全局 TMP 字体 / 传统 Font，切换语言时所有文本自动应用。留空保持原字体。少数特殊文本可用 LocalizeFont 组件单点覆盖。
- **UI 自动刷新**：挂载 `Localize*` 组件即可，切换语言时自动更新，无需手动调接口。
- **静态 API**：`Localization.Get("key")` 全局访问，调用极简；命名/位置/混合占位符多重重载。
- **可配置资源加载方式**：内置 `Resources`（默认）/ `StreamingAssets` 两种模式，路径完全可配，切换时自动迁移已有文件；支持异步加载（Addressables / 热更新可通过实现 `ILocalizationLoader` 接入，主线程安全）。
- **完整编辑器工具**：
  - 多语言编辑窗口（`Tools > CLocalization > Localization Window`）
  - 词条编辑表（搜索、增删、单元格编辑、未翻译高亮、表头点击排序、可拖拽列宽、多行编辑、批量填充/删除、仅看未翻译、复制 key、分页）
  - 翻译完整性诊断（各语言覆盖率、缺失明细、未使用 key 检测、缺失项一键跳转编辑）
  - CSV 导入导出（与翻译人员用 Excel 协作）
  - Key 重命名重构（迁移 JSON + 批量更新场景/Prefab 引用，支持 Undo）
  - Localize 组件 Inspector 增强（key 弹出搜索窗口 + 预览语言可切换 + 插值参数预览）
  - 快捷键（Ctrl/Cmd+S 保存、Ctrl/Cmd+F 聚焦搜索、Delete 删选中）
- **持久化**：自动记住用户上次选择的语言，支持系统语言自动检测（覆盖中/英/日/韩/越/泰/土耳其/印地等数十种）。

---

## 📦 安装

本插件以源码形式集成到工程，无单独安装步骤。依赖项：

- Unity **2022.3 LTS** 及以上
- `com.unity.textmeshpro` 3.0.x（UI 文本本地化）
- `com.unity.nuget.newtonsoft-json` 3.2.x（运行时 JSON 序列化，支持 Dictionary）
- `com.cysharp.unitask`（异步加载，UniTask，Git 引用，已写入 manifest.json）

> 首次打开工程时，Unity 会自动下载 Newtonsoft.Json 与 UniTask 包（需本机装有 Git 且网络可达 GitHub）。

---

## 🚀 快速开始

### 1. 生成配置资源

打开 Unity 后，运行菜单：**`Tools > CLocalization > Create Settings Asset`**

这会在 `Assets/CLocalization/Resources/CLocalization/` 下创建 `LocalizationSettings.asset`，预置中/英/日/韩四种语言。

### 2. 运行 Demo

菜单：**`Tools > CLocalization > Demo > Create Demo Scene`** → 创建演示场景 → 点击运行 ▶

你会看到语言切换按钮，点击后标题、插值文本、数字/货币/日期格式都随之变化。

### 3. 在自己的项目里使用

**初始化**（二选一）：

- **方式 A（推荐）**：在场景新建 GameObject，挂 `LocalizationInitializer` 组件（Settings 留空会自动从 Resources 加载）。
- **方式 B（代码）**：在启动逻辑里调用：
  ```csharp
  var settings = Resources.Load<LocalizationSettings>("CLocalization/LocalizationSettings");
  Localization.Initialize(settings);
  ```

**查询文本**：
```csharp
// 简单查询
string title = Localization.Get("demo.title");

// 带参数插值（对应 "你好,{0}！欢迎来到 {1}。"）
string greet = Localization.Get("ui.text.parameter", "玩家", "Unity");

// 切换语言
Localization.SetLanguage("en-US");
```

**UI 自动本地化**：给任意 Text/TMP_Text 挂 `Localize Text` 组件，填入 key（如 `demo.title`），切换语言时自动刷新。

---

## 📐 架构总览

```
CLocalization/
├── Runtime/                     # 运行时核心（CLocalization.Runtime 程序集）
│   ├── Core/                    # Localization 静态入口 / Settings / 数据结构 / 初始化器
│   ├── Loader/                  # ILocalizationLoader 抽象 + AssetLoadMode 枚举
│   │   ├── ResourcesLocalizationLoader.cs        # Resources 加载（默认）
│   │   └── StreamingAssetsLocalizationLoader.cs  # StreamingAssets 加载（跨平台异步）
│   ├── Text/                    # LocalizationFormatter（位置/命名占位插值 + 格式化）
│   ├── Components/              # LocalizeText/Sprite/AudioSource/Font + LocalizeBase（含 RTL）
│   ├── Persistence/             # PlayerPrefs 持久化 + 系统语言检测（数十种语言映射）
│   └── Util/                    # 日志 + 路径约定
├── Editor/                      # 编辑器工具（CLocalization.Editor 程序集）
│   ├── Services/                # 数据服务 + 导入导出 + 引用扫描器
│   ├── Importer/                # CSV 读写（零依赖，RFC 4180）
│   ├── Window/                  # 主窗口 + 词条/语言/导入导出/诊断 Tab + Key 重命名窗口
│   ├── Settings/                # ProjectSettings 面板（含资源加载方式配置）
│   ├── Inspector/               # LocalizeKeyFieldDrawer 共用 + 4 组件 Drawer + KeyPickerWindow
│   └── Setup/                   # 配置生成 + 资源迁移工具 + Demo 场景 + AssetPostprocessor
├── Resources/CLocalization/     # 运行时数据（Resources 模式）
│   ├── Locales/*.json           # 各语言词条（zh-CN/en-US/ja-JP/ko-KR）
│   └── LocalizationSettings.asset
└── Demo/                        # DemoController（运行时构建演示 UI）
```

### 核心 API

| API | 说明 |
|---|---|
| `Localization.Get(key)` | 获取本地化文本，缺失回退到默认语言，再回退返回 key 本身 |
| `Localization.Get(key, out bool found)` | 同上，并返回是否真正命中（非回退） |
| `Localization.Get(key, params object[] args)` | 位置占位符插值（`{0}` `{1}` ...） |
| `Localization.Get(key, IDictionary<string,object>)` | 命名占位符插值（`{name}` `{count}` ...） |
| `Localization.Get(key, object argsObject)` | 命名占位符插值，参数用匿名对象（`new { name="x" }`） |
| `Localization.Get(key, IDictionary, params object[])` | 混合占位符（命名 + 位置共存） |
| `Localization.TryGet(key, out value)` | 仅查当前语言，不回退，返回是否存在 |
| `Localization.GetAsset<T>(key)` | 加载当前语言的本地化资源（Sprite/Audio/Font） |
| `Localization.SetLanguage(code)` | 切换语言并触发 OnLanguageChanged 事件 |
| `Localization.CurrentLanguage` | 当前语言信息（含 IsRightToLeft，LocalizeText 据此设置 RTL） |
| `Localization.OnLanguageChanged` | 语言切换事件（Localize 组件已自动订阅） |

### 占位符插值

支持两类占位符，可混合使用：

```csharp
// 位置占位 {0}/{1}：模板 "你好,{0}！欢迎来到 {1}。"
Localization.Get("greet", "玩家", "Unity");

// 命名占位 {name}/{count}：模板 "Hello {name}, you have {count} items."
Localization.Get("msg", new { name = "Player", count = 3 });

// 命名占位（字典形式）
var dict = new Dictionary<string, object> { { "name", "Player" }, { "count", 3 } };
Localization.Get("msg", dict);

// 混合：模板 "Hello {name}, you have {0} items."
Localization.Get("msg", new { name = "Player" }, 3);

// 组件方式：给 LocalizeText 设命名参数后自动刷新
localizeText.SetNamedArgs(new { name = "Player", count = 3 });
```

### 资源加载方式（可配置 + 自动迁移）

插件内置两种加载方式，在 `Project Settings > CLocalization` 的「资源加载方式」中切换。切换时**自动迁移**已有的语言 JSON 文件到新目录。

| 模式 | 容器 | 文本 | 资源(Sprite/Audio/Font) | 热更新 |
|---|---|---|---|---|
| **Resources**（默认） | `Resources/{localesPath}/` | ✅ | ✅ | ❌ |
| **StreamingAssets** | `StreamingAssets/{localesPath}/` | ✅ | ❌ | ✅（可外部修改） |

**路径完全可配**：`LocalesPath`（如 `CLocalization/Locales`）和 `AssetsPath`（如 `CLocalization/Assets`）均可在 Settings 修改。

**切换方式**：Project Settings 面板改 LoadMode 下拉 → 自动弹出迁移预览（源/目标目录、文件数）→ 点「迁移资源并应用」即可。

**StreamingAssets 模式限制**（重要）：
- 仅支持**文本**本地化；Sprite/Audio/Font 需用 Resources 或自定义 Loader
- **Android 平台必须用异步** `SetLanguageAsync`（同步 `LoadLocale` 抛 NotSupportedException）
- Project 窗口拖入 JSON 不会自动同步 Settings，需点「刷新语言列表」按钮

> 需要 Addressables / 远端加载？实现 `ILocalizationLoader` 接口，用 `Initialize(settings, customLoader)` 注入（见下节）。

### 资源加载层（可扩展，支持异步）

默认 `ResourcesLocalizationLoader` 从 `Resources/CLocalization/Locales/{code}.json` 加载（同步瞬时）。
若需接入 Addressables 或热更新，实现 `ILocalizationLoader` 接口的异步方法后传入 `Initialize`：

```csharp
ILocalizationLoader myLoader = new MyAddressablesLoader();
Localization.Initialize(settings, myLoader);
```

**异步切换语言**（适用于 Addressables / 热更新 / 远端下载）：

```csharp
// 异步切换，加载在后台进行，状态更新与事件触发在主线程完成（线程安全）
bool ok = await Localization.SetLanguageAsync("en-US");
if (ok) { /* 切换成功，UI 已自动刷新 */ }
```

**自定义 Addressables Loader 示例**：

```csharp
public class AddressablesLoader : ILocalizationLoader
{
    public async UniTask<LocaleData> LoadLocaleAsync(string languageCode)
    {
        // 用 Addressables 异步加载语言 JSON
        var handle = Addressables.LoadAssetAsync<TextAsset>($"Locales/{languageCode}");
        var text = await handle;
        return LocaleData.FromJson(text.text);
    }
    // LoadAssetAsync<T> 同理用 Addressables.LoadAssetAsync
    // 同步方法（LoadLocale/LoadAsset）可抛 NotSupportedException，因为 Addressables 不支持同步
}
```

> 主线程安全：`SetLanguageAsync` 内部用 `UniTask.SwitchToMainThread()` 保证状态写入与事件触发在主线程执行，异步 Loader 可安全地在后台线程加载。

---

## 📝 数据格式

每个语言一个 JSON 文件，结构如下：

```json
{
  "meta": { "code": "zh-CN", "displayName": "中文", "version": "1.0.0" },
  "entries": {
    "demo.title": "标题",
    "ui.text.parameter": "你好,{0}！欢迎来到 {1}。"
  }
}
```

---

## 🔁 Excel/CSV 协作工作流

1. 打开 `Tools > CLocalization > Localization Window` → 切到「导入/导出」Tab。
2. 点「导出为 CSV」生成一个扁平表格（key 列 + 每语言一列）。
3. 用 Excel/WPS 打开编辑翻译（中文 CSV 带 UTF-8 BOM，不会乱码）。
4. 点「从 CSV 导入」把翻译合并回各语言。
5. 点窗口右上角「保存全部」写入磁盘 JSON。

---

## 🖼️ 资源本地化配置（映射表）

Sprite/AudioClip/Font 等资源通过**资源映射表**配置，不再依赖目录约定。

### 配置方式

1. 打开 `Tools > CLocalization > Localization Window` → 切到**「资源」Tab**
2. 顶部切换资源类型：`Sprite` / `AudioClip` / `Font`
3. 左侧点「+」输入 key 新增映射（如 `ui/logo`），或选择已有 key
4. 右侧为每个语言拖入对应资源（Sprite/AudioClip/TMP_FontAsset/Font）
5. 自动保存到映射表 ScriptableObject（`Resources/CLocalization/AssetMaps/`）

### 工作原理

- 三张映射表：`SpriteAssetMap` / `AudioClipAssetMap` / `FontAssetMap`（按类型分离）
- 组件（`LocalizeSprite`/`LocalizeAudioSource`/`LocalizeFont`）只配 key，运行时 `Localization.GetAsset<T>(key)` 按当前语言从映射表查资源
- 切换语言时自动重新查询并刷新

```csharp
// 运行时获取资源（自动按当前语言查映射表）
Sprite logo = Localization.GetAsset<Sprite>("ui/logo");
```

### Inspector 状态提示

Localize 组件的 Inspector 会显示该 key 的映射状态（如「3/4 语言已配置」），并提示去「资源」Tab 编辑。

> 旧版本曾用「按目录放文件」的路径约定加载资源，现已废弃，统一改用映射表。
>
> **路径存储（支持打 AB）**：映射表存储资源路径而非强引用，资源可在工程任意位置。在 Resources 目录下的资源运行时可直接加载；其他位置的资源支持打 AssetBundle 分包，配合自定义 Loader 实现按需加载/热更新。
> 打开资源 Tab 时会自动把旧强引用数据迁移为路径存储。

---

## 🔤 字体本地化（语言级全局配置）

字体本地化采用**语言级全局配置**：每种语言配置一个全局字体，切换语言时所有文本自动应用。

### 配置方式

1. 打开 `Tools > CLocalization > Localization Window` → 切到**「语言」Tab**
2. 在目标语言行点**「字体」**按钮展开配置区
3. 设置该语言的 **TMP 字体**（TMP_FontAsset）和/或 **传统 Font**
4. 留空则保持文本组件原有字体（不强制覆盖）

### 工作原理

- `LanguageInfo` 承载每语言的字体配置（`tmpFont` / `fallbackFont`）
- `LocalizeText` 组件切换语言时自动应用：TMP 文本用 `tmpFont`，传统 UI.Text 用 `fallbackFont`
- 所有挂 LocalizeText 的文本无需额外配置字体，切语言即自动切换

### 优先级（高 → 低）

1. **LocalizeFont 组件**（fontKey → FontAssetMap）——单点覆盖，用于个别特殊文本
2. **LanguageInfo 全局字体**——LocalizeText 自动应用，覆盖大部分场景
3. **保持原字体**——LanguageInfo 字体为 null 时不覆盖

> 典型用法：CJK 语言（中日韩）配专用中文字体，拉丁语言配西文字体。切换语言时文本字体自动跟随。

---

## ⚙️ 配置项（Project Settings > CLocalization）

| 项 | 说明 |
|---|---|
| 语言列表 | 所有可切换的语言（可上移/下移排序） |
| 默认语言代码 | 缺失 key 的回退语言 |
| 持久化语言选择 | 切换后是否记住到 PlayerPrefs |
| 默认使用系统语言 | 初始化时尝试匹配操作系统语言 |
| 回退到默认语言 | 缺失 key 时是否查默认语言 |
| 输出缺失 key 警告 | 查询失败是否打日志 |
| **资源加载方式** | `Resources` / `StreamingAssets`（切换时自动迁移文件） |
| **语言路径** | `LocalesPath`，默认 `CLocalization/Locales` |
| **资源路径** | `AssetsPath`，默认 `CLocalization/Assets`（仅 Resources 模式生效） |

---

## 🛠️ 编辑器工具与菜单速查

| 菜单 | 功能 |
|---|---|
| `Tools > CLocalization > Create Settings Asset` | 生成配置资源（首次必做） |
| `Tools > CLocalization > Localization Window` | 主编辑窗口（词条/语言/导入导出/诊断） |
| `Tools > CLocalization > Rename Key` | **key 重命名重构**（迁移 JSON + 批量更新组件引用，支持 Undo） |
| `Tools > CLocalization > Demo > Create Demo Scene` | 生成演示场景 |
| `Edit > Project Settings > CLocalization` | 配置面板（语言/回退策略/资源加载方式/迁移） |
| Add Component > CLocalization > ... | 添加 Localize 组件 |

**词条 Tab 能力**：
- 表头点击排序（Key 列升降序 / 各语言列按翻译排序，带 ▲/▼ 指示）
- 可拖拽列宽、多行文本编辑、仅看未翻译过滤、复制 key 到剪贴板
- 批量填充（从默认语言）、批量删除（作用于当前过滤结果）
- 分页（每页 200 行，万 key 不卡）
- 快捷键：Ctrl/Cmd+S 保存、Ctrl/Cmd+F 聚焦搜索、Delete 删选中行

**诊断 Tab 能力**：
- 各语言翻译覆盖率（进度条 + 缺失明细）
- **未使用 key 检测**（JSON 有但无组件/源码引用的废 key）
- 缺失项一键「编辑」跳转定位到词条 Tab

**Inspector 能力**（Localize 组件）：
- key 选择器：输入框 + 「选择...」弹出搜索窗口（TreeView 虚拟滚动，上万 key 不卡，搜索过滤）
- 预览语言可切换下拉 + 预览应用插值参数（所见即所得）
- key 存在性校验提示

**自动同步**：向当前模式的语言目录拖入新 JSON 文件时，Settings 的语言列表会自动更新（Resources 模式；StreamingAssets 模式需手动点「刷新语言列表」）。

---

## 📜 许可证

见仓库根 LICENSE。
