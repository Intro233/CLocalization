# CLocalization

> 一个轻量级、易用的 Unity 多语言（本地化）插件。

Unity 2022.3 LTS · C# · 中文文档 · 运行时依赖 Newtonsoft.Json + UniTask

---

## 简介

CLocalization 是一套从零搭建的 Unity 多语言插件，支持**文本 / 图片 / 音频 / 字体**的本地化，提供完整的编辑器工具与 CSV 协作工作流。核心采用静态 API（`Localization.Get("key")`），挂载 `Localize*` 组件即可在切换语言时自动刷新 UI，无需手动调接口。

**仓库结构**：本仓库根目录是 Git 仓库，Unity 工程位于 [`Project/`](./Project) 子目录下（`Assets/`、`Packages/`、`ProjectSettings/` 均在 `Project/` 内）。

---

## 核心特性

- **文本本地化**：TextMeshPro + 传统 UI.Text 双支持；参数插值支持位置占位 `{0}` 与命名占位 `{name}`（可混合）；日期/货币/数字按语言区域格式化；RTL 从右到左方向支持
- **资源本地化**：Sprite、AudioClip、Font / TMP_FontAsset 按语言切换
- **UI 自动刷新**：挂组件即可，语言切换时所有绑定 UI 自动更新
- **可配置资源加载方式**：内置 `Resources`（默认）/ `StreamingAssets`，路径完全可配，切换时**自动迁移**已有文件；支持异步加载（Addressables / 热更新可经 `ILocalizationLoader` 接入，主线程安全）
- **完整编辑器工具**：
  - 词条编辑表（表头排序、可拖拽列宽、多行编辑、批量填充/删除、仅看未翻译、搜索、分页）
  - 翻译诊断（覆盖率、缺失明细、未使用 key 检测、一键跳转编辑）
  - CSV 导入导出（与翻译人员用 Excel 协作）
  - Key 重命名重构（迁移 JSON + 批量更新场景/Prefab 引用 + Undo）
  - Inspector 增强（key 弹出搜索窗口、预览语言可切换、插值预览）
  - 快捷键（Ctrl/Cmd+S 保存、Ctrl/Cmd+F 聚焦搜索、Delete 删选中）
- **持久化**：自动记住用户上次选择的语言，系统语言自动检测（覆盖数十种语言）

---

## 快速开始

1. 用 Unity 2022.3 LTS 打开 [`Project/`](./Project) 目录（首次会自动拉取 Newtonsoft.Json 与 UniTask 包，需本机装有 Git）
2. 菜单 `Tools > CLocalization > Create Settings Asset` 生成配置（预置中/英/日/韩）
3. 菜单 `Tools > CLocalization > Demo > Create Demo Scene` 生成演示场景，运行即可看到语言切换效果

```csharp
// 初始化（挂 LocalizationInitializer 组件，或代码调用）
Localization.Initialize(settings);

// 文本查询（位置占位）
Localization.Get("ui.greet", "玩家", "Unity");

// 命名占位
Localization.Get("ui.greet", new { name = "Player", count = 3 });

// 切换语言（同步 / 异步）
Localization.SetLanguage("en-US");
await Localization.SetLanguageAsync("ja-JP");
```

---

## 文档

完整文档（特性、安装、架构、API、工作流、配置项）见工程内：

👉 **[`Project/Assets/CLocalization/README.md`](./Project/Assets/CLocalization/README.md)**

更新日志：[`Project/Assets/CLocalization/CHANGELOG.md`](./Project/Assets/CLocalization/CHANGELOG.md)

---

## 技术栈

| 项 | 说明 |
|---|---|
| Unity | 2022.3 LTS（.NET Standard 2.1） |
| Newtonsoft.Json | 3.2.x（运行时 JSON，支持 Dictionary） |
| UniTask | 异步加载（零 GC async/await） |
| TextMeshPro | 3.0.x（UI 文本本地化） |
| 程序集划分 | `CLocalization.Runtime` / `CLocalization.Editor`（asmdef） |

---

## 许可证

见 [LICENSE](./LICENSE)。
