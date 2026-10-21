using System;
using System.Collections;

// 인벤토리 서버 API 호출부. 응답 반영과 요청 중 상태는 호출하는 쪽(ServerLoadManager, ServerInventoryManager)이 맡는다
public static class InventoryApi
{
    public const string InventoryPath = "/api/inventory";
    private const string ActionPathFormat = "/api/inventory/{0}/{1}";   // {0}: inventoryId, {1}: 동작

    // 서버 에러 code (InventoryErrorCode)
    public const string ErrorInventoryNotFound = "INVENTORY_NOT_FOUND";
    public const string ErrorNotEnoughGold = "NOT_ENOUGH_GOLD";
    public const string ErrorNotEnoughMaterial = "NOT_ENOUGH_MATERIAL";

    public static IEnumerator GetInventory(Action<ApiResult<InventoryResponse>> onComplete)
        => ApiClient.Get(InventoryPath, onComplete);

    public static IEnumerator Equip(long inventoryId, Action<ApiResult<EquipResponse>> onComplete)
        => PostAction(inventoryId, "equip", onComplete);

    public static IEnumerator Unequip(long inventoryId, Action<ApiResult<InventoryData>> onComplete)
        => PostAction(inventoryId, "unequip", onComplete);

    public static IEnumerator LevelUp(long inventoryId, Action<ApiResult<LevelUpResponse>> onComplete)
        => PostAction(inventoryId, "levelup", onComplete);

    public static IEnumerator BatchLevelUp(long inventoryId, Action<ApiResult<LevelUpBatchResponse>> onComplete)
        => PostAction(inventoryId, "levelup/batch", onComplete);

    public static IEnumerator Synthesize(long inventoryId, Action<ApiResult<SynthesizeResponse>> onComplete)
        => PostAction(inventoryId, "synthesize", onComplete);

    public static IEnumerator BatchSynthesize(long inventoryId, Action<ApiResult<SynthesizeResponse>> onComplete)
        => PostAction(inventoryId, "synthesize/batch", onComplete);

    // 본문 없는 POST: 어떤 장비에 무엇을 할지는 URL로 전달
    private static IEnumerator PostAction<T>(long inventoryId, string action, Action<ApiResult<T>> onComplete)
        => ApiClient.Post(string.Format(ActionPathFormat, inventoryId, action), null, onComplete);
}