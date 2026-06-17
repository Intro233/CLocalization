using CLocalization;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Localization.Get("ui.text.parameter", "玩家", "Unity")); // → "你好,玩家！欢迎来到 Unity。"
        Localization.SetLanguage("en-US");
        Debug.Log(Localization.Get("app.title")); // → "CLocalization Demo"
    }
}
