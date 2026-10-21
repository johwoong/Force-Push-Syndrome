// ApiResult를 화면에 보여줄 문구로 바꾼다
// 사용: popup.ShowAlert(result.ToUserMessage("장착하지 못했습니다."));
public static class ApiResultExtensions
{
    public static string ToUserMessage<T>(this ApiResult<T> result, string fallback)
    {
        switch (result.FailType)
        {
            case ApiFailType.NetworkError:
                return "서버에 연결할 수 없습니다. 잠시 후 다시 시도해 주세요.";

            case ApiFailType.Unauthorized:
                return "로그인 정보가 만료되었습니다. 다시 로그인해 주세요.";

            case ApiFailType.ParseError:
                return "서버 응답을 읽지 못했습니다.";

            case ApiFailType.ServerError:
                // 서버 ErrorResponse.message (예: "골드가 부족합니다.", "이미 사용 중인 아이디입니다.")
                return string.IsNullOrEmpty(result.Error?.message) ? fallback : result.Error.message;

            default:
                return fallback;
        }
    }
}
