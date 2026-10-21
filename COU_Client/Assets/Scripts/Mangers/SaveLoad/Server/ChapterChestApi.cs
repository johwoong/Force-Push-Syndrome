using System;
using System.Collections;

// 챕터 보물상자 서버 API 호출부
public static class ChapterChestApi
{
    public const string ClaimPathFormat = "/api/chapter-chests/{0}/claim";   // {0}: stageId

    // 서버 에러 code
    public const string ErrorStageNotCleared = "STAGE_NOT_CLEARED";
    public const string ErrorAlreadyClaimed = "ALREADY_CLAIMED";

    // 클리어한 스테이지의 보물상자를 한 번 받는다. 장비는 서버가 뽑아 지급한다
    public static IEnumerator Claim(int stageId, Action<ApiResult<ChapterChestClaimResponse>> onComplete)
        => ApiClient.Post(string.Format(ClaimPathFormat, stageId), null, onComplete);
}
