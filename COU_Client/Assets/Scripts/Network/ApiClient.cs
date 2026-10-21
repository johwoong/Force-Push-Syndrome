using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class ApiClient
{
    public const int DefaultTimeoutSeconds = 10;
    public const string DevTokenPrefsKey = "COU.ApiClient.DevAccessToken";

    public static string BaseUrl { get; set; } = GameConstants.Server.BASE_URL;
    public static int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;
    public static string AccessToken { get; private set; }
    public static bool HasToken => !string.IsNullOrEmpty(AccessToken);

    // 401을 받았을 때 (토큰 없음/만료). 로그인 화면 이동 등은 구독하는 쪽에서 처리
    public static event Action OnUnauthorized;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Ignore
    };

    public static void SetAccessToken(string token) => AccessToken = token;
    public static void ClearAccessToken() => AccessToken = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        BaseUrl = GameConstants.Server.BASE_URL;
        TimeoutSeconds = DefaultTimeoutSeconds;
        AccessToken = null;
        OnUnauthorized = null;

#if UNITY_EDITOR
        string devToken = UnityEditor.EditorPrefs.GetString(DevTokenPrefsKey, "");
        if (!string.IsNullOrEmpty(devToken))
        {
            AccessToken = devToken;
            Debug.Log("[ApiClient] 개발용 토큰 적용");
        }
#endif
    }

    // ===== 요청 함수 =====
    // auth = false : 토큰을 붙이지 않음 (정적 데이터처럼 인증이 필요없는 API. 만료 토큰으로 인한 401 방지)

    public static IEnumerator Get<T>(string path, Action<ApiResult<T>> onComplete, bool auth = true)
        => Send("GET", path, null, onComplete, auth);

    public static IEnumerator Post<T>(string path, object body, Action<ApiResult<T>> onComplete, bool auth = true)
        => Send("POST", path, body, onComplete, auth);

    public static IEnumerator Put<T>(string path, object body, Action<ApiResult<T>> onComplete, bool auth = true)
        => Send("PUT", path, body, onComplete, auth);

    public static IEnumerator Delete<T>(string path, Action<ApiResult<T>> onComplete, bool auth = true)
        => Send("DELETE", path, null, onComplete, auth);

    // ===== 내부 =====
    private static IEnumerator Send<T>(string method, string path, object body, Action<ApiResult<T>> onComplete, bool auth)
    {
        string url = BaseUrl.TrimEnd('/') + path;

        using (var request = new UnityWebRequest(url, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");

            if (body != null)
            {
                string json = JsonConvert.SerializeObject(body, JsonSettings);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            bool sentToken = auth && HasToken;   // 로그인/회원가입(auth:false)의 401은 비밀번호 오류라 제외
            if (sentToken)
                request.SetRequestHeader("Authorization", "Bearer " + AccessToken);

            yield return request.SendWebRequest();

            var result = BuildResult<T>(request);

            if (!result.IsSuccess)
                Debug.LogWarning("[ApiClient] " + method + " " + path + " 실패: " + result.Describe());

            if (result.FailType == ApiFailType.Unauthorized && sentToken)
                OnUnauthorized?.Invoke();

            onComplete?.Invoke(result);
        }
    }

    private static ApiResult<T> BuildResult<T>(UnityWebRequest request)
    {
        long status = request.responseCode;
        string text = request.downloadHandler?.text;

        // 서버에 닿지 못함 (꺼짐, 타임아웃, 주소 오류)
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.DataProcessingError)
        {
            return ApiResult<T>.Fail(ApiFailType.NetworkError, status, null, request.error);
        }

        // 401은 본문이 비어 있어서 ErrorResponse로 읽지 않는다.
        if (status == 401)
            return ApiResult<T>.Fail(ApiFailType.Unauthorized, status, null, text);

        // 그 밖의 HTTP 에러 : 서버 공통 에러 형식으로 읽기
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            var error = TryDeserialize<ErrorResponse>(text) ?? new ErrorResponse
            {
                status = (int)status,
                code = "UNKNOWN",
                message = string.IsNullOrEmpty(text) ? request.error : text
            };

            return ApiResult<T>.Fail(ApiFailType.ServerError, status, error, text);
        }

        // 성공: 본문이 없으면 기본 값
        if (string.IsNullOrEmpty(text))
            return ApiResult<T>.Success(default, status, text);

        try
        {
            var data = JsonConvert.DeserializeObject<T>(text, JsonSettings);
            return ApiResult<T>.Success(data, status, text);
        }
        catch(Exception e)
        {
            Debug.LogWarning("[ApiClient] 응답 JSON 변환 실패 : " + e.Message + "\n" + Truncate(text, 300));
            return ApiResult<T>.Fail(ApiFailType.ParseError, status, null, text);
        }
    }


    private static TData TryDeserialize<TData>(string text) where TData : class
    {
        if (string.IsNullOrEmpty(text)) return null;
        try { return JsonConvert.DeserializeObject<TData>(text, JsonSettings); }
        catch { return null; }
    }


    private static string Truncate(string text, int max)
        => text == null || text.Length <= max ? text : text.Substring(0, max) + "...";
}
