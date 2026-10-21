using UnityEngine;

public static class Haptics
{
    // 설정 ON/OFF를 앱을 껐다 켜도 유지되게 PlayerPrefs에 저장(UITheme와 같은 방식)
    private const string EnabledKey = "Haptics.Enabled";

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;   // Android의 진동 서비스 객체 (한 번만 가져와서 재사용)
    private static int sdkInt;                   // Android API 레벨 (26 이상이면 새 방식)
    private static bool nativeFailed;            // 네이티브 진동이 한 번 실패했는지 기억

    private static void Init()
    {
        // Unity가 실행 중인 Android Activity → 시스템 서비스 "vibrator"
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

        using var version = new AndroidJavaClass("android.os.Build$VERSION");
        sdkInt = version.GetStatic<int>("SDK_INT");
    }
#endif

    public static void Vibrate(long milliseconds)
    {
        if (!Enabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (nativeFailed)
        {
            Handheld.Vibrate();
            return;
        }

        try
        {
            if (vibrator == null) Init();

            if (sdkInt >= 26)
            {
                // Android 8.0+ : VibrationEffect.createOneShot(길이ms, 세기). -1 = 기기 기본 세기
                using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);   // 구버전 방식
            }
        }
        catch (System.Exception e)
        {
            nativeFailed = true; 
            // 네이티브 호출이 실패해도 게임이 멈추지 않게 기본 진동으로 대체
            Debug.LogWarning($"[Haptics] 네이티브 진동 실패, 기본 진동 사용: {e.Message}");
            Handheld.Vibrate();   // 이 참조 덕분에 Unity가 VIBRATE 권한을 자동으로 추가함
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();       // iOS 짧은 햅틱은 네이티브 플러그인이 필요해서 우선 기본 진동
#else
        // 에디터/PC: 진동 없음 (원하면 로그로 확인)
#endif
    }
}
