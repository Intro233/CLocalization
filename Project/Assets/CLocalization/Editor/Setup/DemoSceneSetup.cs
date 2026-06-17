using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CLocalization.Demo;

namespace CLocalization.Editor
{
    /// <summary>
    /// Demo 场景创建辅助。一键新建一个挂好 DemoController 的场景，便于直接运行查看效果。
    /// 菜单：Tools/CLocalization/Demo > Create Demo Scene
    /// </summary>
    public static class DemoSceneSetup
    {
        /// <summary>默认 Demo 场景保存路径（相对 Assets）。</summary>
        private const string DemoScenePath = "Assets/CLocalization/Demo/DemoScene.unity";

        [MenuItem("Tools/CLocalization/Demo/Create Demo Scene", priority = 10)]
        public static void CreateDemoScene()
        {
            // 确保配置资源存在（Demo 运行需要）
            LocalizationSetup.LoadOrCreateSettings();

            // 新建场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 主摄像机（UGUI 仍需要一个摄像机作为 EventSystem 视线，ScreenSpaceOverlay 模式不强制，但保留以规范）
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            cam.orthographic = true;
            cam.orthographicSize = 5;

            // EventSystem（UGUI 交互必需）
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Demo 控制器（运行时构建 UI 并演示本地化）
            var demoGo = new GameObject("DemoController");
            demoGo.AddComponent<DemoController>();

            // 保存场景
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DemoScenePath));
            EditorSceneManager.SaveScene(scene, DemoScenePath);

            LocalizationLog.Info($"Demo 场景已创建: {DemoScenePath}。点击运行即可查看效果。");
            EditorUtility.DisplayDialog("Demo 场景已创建",
                $"场景已保存到:\n{DemoScenePath}\n\n点击顶部 ▶ 运行即可看到多语言切换演示。", "确定");
        }
    }
}
