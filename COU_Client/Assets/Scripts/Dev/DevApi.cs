#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;

// 서버 개발용 API 호출부. 서버가 DEV_TOOLS_ENABLED=true로 켜져 있을 때만 열린다
// 사용 : StartCoroutine(DevApi.RefillStamina(result => { .... }));
public static class DevApi
{
    public const string StaminaPath = "/api/dev/stamina";
    public const string ItemsPath = "/api/dev/items";

    // 스태미나를 최대치로 채운다
    public static IEnumerator RefillStamina(Action<ApiResult<CurrencyData>> onComplete)
        => ApiClient.Post(StaminaPath, null, onComplete);

    // 장비를 count개 지급한다
    public static IEnumerator GrantItems(long itemId, int count, Action<ApiResult<List<InventoryData>>> onComplete)
        => ApiClient.Post(ItemsPath, new DevItemGrantRequest { itemId = itemId, count = count }, onComplete);
}

[Serializable]
public class DevItemGrantRequest
{
    public long itemId;
    public int count;
}
#endif
