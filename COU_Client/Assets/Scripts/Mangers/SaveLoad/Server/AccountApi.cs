using System;
using System.Collections;

// 계정·플레이어 데이터 서버 API 호출부. 응답 반영과 요청 중 상태는 호출하는 쪽(ServerLoginManager, ServerLoadManager)이 맡는다
public static class AccountApi
{
    public const string SignupPath = "/api/accounts/signup";
    public const string LoginPath = "/api/accounts/login";
    public const string SavePath = "/api/players/me/save";

    // 회원가입·로그인은 토큰을 붙이지 않는다 (auth: false)
    // 남아 있는 만료 토큰이 붙으면 서버 JWT 필터가 permitAll 경로라도 401로 막기 때문
    public static IEnumerator Signup(SignupRequest body, Action<ApiResult<ServerAccountData>> onComplete)
        => ApiClient.Post(SignupPath, body, onComplete, auth: false);

    public static IEnumerator Login(LoginRequest body, Action<ApiResult<LoginResponse>> onComplete)
        => ApiClient.Post(LoginPath, body, onComplete, auth: false);

    // 프로필·재화·스테이지·스탯
    public static IEnumerator GetSave(Action<ApiResult<PlayerSaveData>> onComplete)
        => ApiClient.Get(SavePath, onComplete);
}