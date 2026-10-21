using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class FriendApi
{
    public const string Path = "/api/friends";

    public static IEnumerator GetFriends(Action<ApiResult<FriendListResponse>> onComplete)
        => ApiClient.Get(Path, onComplete);

    public static IEnumerator GetRequests(Action<ApiResult<List<FriendRequestResponse>>> onComplete)
        => ApiClient.Get(Path + "/requests", onComplete);

    public static IEnumerator SendRequest(long targetPlayerId, Action<ApiResult<FriendRequestResultResponse>> onComplete)
        => ApiClient.Post(Path + "/requests", new FriendRequestBody { targetPlayerId = targetPlayerId }, onComplete);

    public static IEnumerator Accept(long requestId, Action<ApiResult<FriendResponse>> onComplete)
        => ApiClient.Post<FriendResponse>(Path + "/requests/" + requestId + "/accept", null, onComplete);

    public static IEnumerator Reject(long requestId, Action<ApiResult<object>> onComplete)
        => ApiClient.Post<object>(Path + "/requests/" + requestId + "/reject", null, onComplete);

    public static IEnumerator Remove(long friendPlayerId, Action<ApiResult<object>> onComplete)
        => ApiClient.Delete<object>(Path + "/" + friendPlayerId, onComplete);

    public static IEnumerator Search(string keyword, Action<ApiResult<List<FriendSearchResponse>>> onComplete)
        => ApiClient.Get(Path + "/search?keyword=" + UnityWebRequest.EscapeURL(keyword), onComplete);
}