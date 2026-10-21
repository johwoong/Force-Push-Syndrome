#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

// 모든 씬에서 같은 방식으로 여는 디버그 패널. 섹션은 조작 대상이 있을 때만 그린다 (DevPanel.Battle, DevPanel.Main)
public partial class DevPanel : MonoBehaviour
{
    private const float ReferenceHeight = 960f;   // 세로 1920 화면에서 2배
    private const float WindowWidth = 360f;
    private const float WindowHeight = 600f;

    private bool open;
    private Vector2 scroll;
    private string message = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("DevPanel");
        DontDestroyOnLoad(go);
        go.AddComponent<DevPanel>();
    }

    private void OnGUI()
    {
        bool battle = CanDrawBattle;
        bool main = CanDrawMain;
        if (!battle && !main) return;

        float scale = Screen.height / ReferenceHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        if (GUI.Button(new Rect(width - 70f, 10f, 60f, 30f), "DEV"))
            open = !open;

        if (!open) return;

        float windowWidth = Mathf.Min(WindowWidth, width - 20f);
        float windowHeight = Mathf.Min(WindowHeight, ReferenceHeight - 60f);
        GUILayout.BeginArea(new Rect(width - windowWidth - 10f, 50f, windowWidth, windowHeight), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);

        if (battle) DrawBattle();
        if (main) DrawMain();

        GUILayout.EndScrollView();
        GUILayout.Label(message);
        GUILayout.EndArea();
    }
}
#endif
