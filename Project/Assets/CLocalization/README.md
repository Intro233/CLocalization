# CLocalization

一个轻量级、易用的 Unity 多语言（本地化）插件。

支持文本、图片、音频、字体的本地化，提供完整的编辑器工具与 Excel/CSV 协作工作流。
零第三方依赖（运行时仅依赖 Newtonsoft.Json），适合中小型到中大型项目。

---

## ✨ 特性

- **文本本地化**：TextMeshPro 与传统 UI.Text 双支持，参数插值（`{0}`/`{name}`），日期/货币/数字按语言区域格式化。
- **资源本地化**：Sprite、AudioClip、Font / TMP_FontAsset 可按语言切换。
- **UI 自动刷新**：挂载 `Localize*` 组件即可，切换语言时自动更新，无需手动调接口。
- **静态 API**：`Localization.Get("key")` 全局访问，调用极简。
- **资源加载抽象**：默认 `Resources` 加载，预留 `Addressables`/热更新接口，运行时核心不绑定具体加载方式。
- **完整编辑器工具**：
  - 多语言编辑窗口（`Tools > CLocalization > Localization Window`）
  - 词条编辑表（搜索、增删、单元格编辑、未翻译高亮）
  - 翻译完整性诊断（各语言覆盖率、缺失明细）
  - CSV 导入导出（与翻译人员用 Excel 协作）
  - Localize 组件 Inspector 增强（key 下拉选择 + 预览）
- **持久化**：自动记住用户上次选择的语言，支持系统语言自动检测。

---

## 📦 安装

本插件以源码形式集成到工程，无单独安装步骤。依赖项：

- Unity **2022.3 LTS** 及以上
- `com.unity.textmeshpro` 3.0.x（UI 文本本地化）
- `com.unity.nuget.newtonsoft-json` 3.2.x（已写入 manifest.json，自动引入）

> 首次打开工程时，Unity 会自动下载 Newtonsoft.Json 包。

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
│   ├── Loader/                  # ILocalizationLoader 抽象 + Resources 实现
│   ├── Text/                    # LocalizationFormatter（插值 + 格式化）
│   ├── Components/              # LocalizeText/Sprite/AudioSource/Font + LocalizeBase
│   ├── Persistence/             # PlayerPrefs 持久化 + 系统语言检测
│   └── Util/                    # 日志 + 路径约定
├── Editor/                      # 编辑器工具（CLocalization.Editor 程序集）
│   ├── Services/                # 数据服务 + 导入导出
│   ├── Importer/                # CSV 读写
│   ├── Window/                  # 主窗口 + 4 个 Tab
│   ├── Settings/                # ProjectSettings 面板
│   ├── Inspector/               # LocalizeText Inspector 增强
│   └── Setup/                   # 配置生成 + Demo 场景生成
├── Resources/CLocalization/     # 运行时数据
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

### 资源加载层（可扩展）

默认 `ResourcesLocalizationLoader` 从 `Resources/CLocalization/Locales/{code}.json` 加载。
若需接入 Addressables 或热更新，实现 `ILocalizationLoader` 接口后传入 `Initialize`：

```csharp
ILocalizationLoader myLoader = new MyAddressablesLoader();
Localization.Initialize(settings, myLoader);
```

运行时核心代码无需任何修改。

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

## 📂 资源本地化目录约定

本地化资源（Sprite/Audio/Font）按以下结构放置（Resources 加载）：

```
Resources/CLocalization/Assets/
├── zh-CN/
│   └── ui/logo          ← key 为 ui/logo 的中文版 Sprite/...
├── en-US/
│   └── ui/logo
└── ...
```

---

## ⚙️ 配置项（Project Settings > CLocalization）

| 项 | 说明 |
|---|---|
| 语言列表 | 所有可切换的语言 |
| 默认语言代码 | 缺失 key 的回退语言 |
| 持久化语言选择 | 切换后是否记住到 PlayerPrefs |
| 默认使用系统语言 | 初始化时尝试匹配操作系统语言 |
| 回退到默认语言 | 缺失 key 时是否查默认语言 |
| 输出缺失 key 警告 | 查询失败是否打日志 |

---

## 🛠️ 编辑器工具与菜单速查

| 菜单 | 功能 |
|---|---|
| `Tools > CLocalization > Create Settings Asset` | 生成配置资源（首次必做） |
| `Tools > CLocalization > Localization Window` | 主编辑窗口（词条/语言/导入导出/诊断） |
| `Tools > CLocalization > Rename Key` | **key 重命名重构**（迁移 JSON + 批量更新组件引用，支持 Undo） |
| `Tools > CLocalization > Demo > Create Demo Scene` | 生成演示场景 |
| `Edit > Project Settings > CLocalization` | 配置面板（语言列表/默认语言/回退策略） |
| Add Component > CLocalization > ... | 添加 Localize 组件 |

**诊断 Tab 能力**：
- 各语言翻译覆盖率（进度条 + 缺失明细）
- **未使用 key 检测**（JSON 有但无组件/源码引用的废 key）

**自动同步**：向 `Resources/CLocalization/Locales/` 拖入新 JSON 文件时，Settings 的语言列表会自动更新（merge 策略，不删除已有配置）。

---

## 📜 许可证

见仓库根 LICENSE。
