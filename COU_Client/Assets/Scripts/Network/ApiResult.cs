using System;
using UnityEngine;

public enum ApiFailType
{
    None,           // 성공
    ServerError,    // 서버가 에러 형식으로 응답 (404, 500 등) -> Error.code로 분기
    Unauthorized,   // 401 (본문 없음)
    NetworkError,   // 서버 꺼짐, 타임아웃, 연결 실패
    ParseError      // 성공 응답이지만 JSON을 읽지 못함
}

public class ApiResult<T>
{
    public bool IsSuccess => FailType == ApiFailType.None;
    public ApiFailType FailType { get; }
    public T Data { get; }
    public ErrorResponse Error { get; } // ServerError일 때만 값이 있음.
    public long StatusCode { get; }     // HTTP 상태 코드 (연결 실패면 0)
    public string RawText { get; }      // 디버그용 원문

    private ApiResult(ApiFailType failType, T data, ErrorResponse error, long statusCode, string rawText)
    {
        FailType = failType;
        Data = data;
        Error = error;
        StatusCode = statusCode;
        RawText = rawText;
    }

    public static ApiResult<T> Success(T data, long statusCode, string rawText)
        => new(ApiFailType.None, data, null, statusCode, rawText);

    public static ApiResult<T> Fail(ApiFailType failType, long statusCode, ErrorResponse error, string rawText)
        => new(failType, default, error, statusCode, rawText);

    public string Describe()
    {
        if (IsSuccess) return "성공 (" + StatusCode + ")";
        if (FailType == ApiFailType.ServerError && Error != null)
            return FailType + " " + StatusCode + " " + Error.code + " - " + Error.message;

        return FailType + " (" + StatusCode + ")";
    }
}