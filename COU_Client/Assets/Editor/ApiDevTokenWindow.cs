using UnityEditor;
using UnityEngine;

public class ApiDevTokenWindow : EditorWindow
{
    private string token;

    [MenuItem("COU/API 개발용 토큰")]
    private static void Open()
    {
        GetWindow<ApiDevTokenWindow>("API 개발용 토큰");
    }

    private void OnEnable()
    {
        token = EditorPrefs.GetString(ApiClient.DevTokenPrefsKey, "");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("서버 주소", ApiClient.BaseUrl);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Access Token");
        token = EditorGUILayout.TextArea(token, GUILayout.MinHeight(60));

        EditorGUILayout.HelpBox("이 값은 이 PC의 Unity 에디터에만 저장되고, 프로젝트/GIT에는 저장되지 않습니다. Play를 시작할 때 자동으로 적용됩니다.", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("저장"))
            {
                EditorPrefs.SetString(ApiClient.DevTokenPrefsKey, token.Trim());
                if (Application.isPlaying) ApiClient.SetAccessToken(token.Trim());
                Debug.Log("[ApiDevTokenWindow] 개발용 토큰 저장");
            }

            if (GUILayout.Button("지우기"))
            {
                token = "";
                EditorPrefs.DeleteKey(ApiClient.DevTokenPrefsKey);
                if (Application.isPlaying) ApiClient.ClearAccessToken();
                Debug.Log("[ApiDevTokenWindow] 개발용 토큰 삭제");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("현재 Play 중 토큰 적용", Application.isPlaying ? (ApiClient.HasToken ? "적용됨" : "없음") : "-");
    }
}
