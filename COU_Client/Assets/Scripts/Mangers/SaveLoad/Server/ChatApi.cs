using System;
using System.Collections;
using System.Collections.Generic;

public static class ChatApi
{
    public const string Path = "/api/chat";

    public static IEnumerator GetMessages(long friendPlayerId, long afterId, int limit,
        Action<ApiResult<List<ChatMessageResponse>>> onComplete)
        => ApiClient.Get(Path + "/" + friendPlayerId + "/messages?afterId=" + afterId + "&limit=" + limit, onComplete);

    public static IEnumerator Send(long friendPlayerId, string content,
        Action<ApiResult<ChatMessageResponse>> onComplete)
        => ApiClient.Post(Path + "/" + friendPlayerId + "/messages", new ChatSendRequest { content = content }, onComplete);
}
