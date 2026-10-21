using System;
using System.Collections.Generic;
using UnityEngine;

public class ServerFriendManager : MonoSingleton<ServerFriendManager>
{
    public void FetchFriends(Action<bool, string, FriendListResponse> onComplete)
    {
        StartCoroutine(FriendApi.GetFriends(r => Complete(r, "친구 목록을 불러오지 못했습니다.", onComplete)));
    }

    public void FetchRequests(Action<bool, string, List<FriendRequestResponse>> onComplete)
    {
        StartCoroutine(FriendApi.GetRequests(r => Complete(r, "받은 요청을 불러오지 못했습니다.", onComplete)));
    }

    public void SendRequest(long targetPlayerId, Action<bool, string, FriendRequestResultResponse> onComplete)
    {
        StartCoroutine(FriendApi.SendRequest(targetPlayerId, r => Complete(r, "친구 요청을 보내지 못했습니다.", onComplete)));
    }

    public void Accept(long requestId, Action<bool, string, FriendResponse> onComplete)
    {
        StartCoroutine(FriendApi.Accept(requestId, r => Complete(r, "요청을 수락하지 못했습니다.", onComplete)));
    }

    public void Reject(long requestId, Action<bool, string> onComplete)
    {
        StartCoroutine(FriendApi.Reject(requestId, r =>
            onComplete?.Invoke(r.IsSuccess, r.IsSuccess ? null : r.ToUserMessage("요청을 거절하지 못했습니다."))));
    }

    public void Remove(long friendPlayerId, Action<bool, string> onComplete)
    {
        StartCoroutine(FriendApi.Remove(friendPlayerId, r =>
            onComplete?.Invoke(r.IsSuccess, r.IsSuccess ? null : r.ToUserMessage("친구를 삭제하지 못했습니다."))));
    }

    public void Search(string keyword, Action<bool, string, List<FriendSearchResponse>> onComplete)
    {
        StartCoroutine(FriendApi.Search(keyword, r => Complete(r, "검색에 실패했습니다.", onComplete)));
    }

    // 성공이면 (true, null, 데이터), 실패면 (false, 사용자용 메시지, null)
    private static void Complete<T>(ApiResult<T> result, string fallback, Action<bool, string, T> onComplete)
    {
        if (!result.IsSuccess)
        {
            onComplete?.Invoke(false, result.ToUserMessage(fallback), default);
            return;
        }
        onComplete?.Invoke(true, null, result.Data);
    }
}