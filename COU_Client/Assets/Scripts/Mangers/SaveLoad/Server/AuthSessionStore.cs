using System;
using Newtonsoft.Json;
using UnityEngine;

// 자동 로그인용 세션 저장소 (PlayerPrefs)
// 로그인 후 게임 입장까지 성공한 계정의 토큰과 계정 정보를 남겨 두고, 다음 실행 때 사용한다
// 비밀번호는 저장X
public static class AuthSessionStore
{
    public static void Save(string accessToken, ServerAccountData account)
    {
        if (string.IsNullOrEmpty(accessToken) || account == null) return;
        
        PlayerPrefs.SetString(GameConstants.PrefsKeys.AUTH_ACCESS_TOKEN, accessToken);
        PlayerPrefs.SetString(GameConstants.PrefsKeys.AUTH_ACCOUNT, JsonConvert.SerializeObject(account));
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out string accessToken, out ServerAccountData account)
    {
        accessToken = PlayerPrefs.GetString(GameConstants.PrefsKeys.AUTH_ACCESS_TOKEN, "");
        account = null;

        if (string.IsNullOrEmpty(accessToken)) return false;

        try
        {
            account = JsonConvert.DeserializeObject<ServerAccountData>(PlayerPrefs.GetString(GameConstants.PrefsKeys.AUTH_ACCOUNT, ""));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AuthSession] 저장된 계정 정보를 읽지 못했습니다: " + e.Message);
        }
        
        // 계정 정보가 깨져 있으면 쓸 수 없는 세션이므로 지운다
        if (account == null)
        {
            Clear();
            return false;
        }

        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(GameConstants.PrefsKeys.AUTH_ACCESS_TOKEN);
        PlayerPrefs.DeleteKey(GameConstants.PrefsKeys.AUTH_ACCOUNT);
        PlayerPrefs.Save();
    }
}
