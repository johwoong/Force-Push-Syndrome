using System;
using System.Collections;

// 장비 장착/해제/레벨업/합성을 InventoryApi로 요청하고, 응답을 PlayerInventory에 반영한다
// 코루틴은 이 매니저에서 실행한다 (UI 팝업에서 실행하면 팝업이 닫힐 때 멈춰서 콜백이 오지 않음)
public class ServerInventoryManager : MonoSingleton<ServerInventoryManager>
{
    private bool isRequesting;
    public bool IsRequesting => isRequesting;

    // ===== 장착 / 해제 =====
    public void Equip(OwnedItem item, Action<bool, string> onComplete)
        => Run(item, onComplete, id => InventoryApi.Equip(id,
            result => Finish(result, PlayerInventory.Instance.ApplyEquip, "장착하지 못했습니다.", onComplete)));

    public void Unequip(OwnedItem item, Action<bool, string> onComplete)
        => Run(item, onComplete, id => InventoryApi.Unequip(id,
            result => Finish(result, PlayerInventory.Instance.ApplyUnequip, "장착을 해제하지 못했습니다.", onComplete)));

    // ===== 레벨업 =====
    public void LevelUp(OwnedItem item, Action<bool, string> onComplete)
        => Run(item, onComplete, id => InventoryApi.LevelUp(id,
            result => Finish(result,
                r => PlayerInventory.Instance.ApplyLevelUp(r.inventoryId, r.inventoryItemLevel, r.currencyGold),
                "레벨업하지 못했습니다.", onComplete)));

    public void BatchLevelUp(OwnedItem item, Action<bool, string> onComplete)
        => Run(item, onComplete, id => InventoryApi.BatchLevelUp(id,
            result => Finish(result,
                r => PlayerInventory.Instance.ApplyLevelUp(r.inventoryId, r.inventoryItemLevel, r.currencyGold),
                "레벨업하지 못했습니다.", onComplete)));

    // ===== 합성 =====
    public void Synthesize(OwnedItem item, Action<bool, string> onComplete)
        => Run(item, onComplete, id => InventoryApi.Synthesize(id,
            result => Finish(result, PlayerInventory.Instance.ApplySynthesis, "합성하지 못했습니다.", onComplete)));

    public void BatchSynthesize(OwnedItem item, Action<bool, string> onComplete)
        => Run(item, onComplete, id => InventoryApi.BatchSynthesize(id,
            result => Finish(result, PlayerInventory.Instance.ApplySynthesis, "합성하지 못했습니다.", onComplete)));

    // 요청 전 검사 → 잠금 → 코루틴 시작
    private void Run(OwnedItem item, Action<bool, string> onComplete, Func<long, IEnumerator> request)
    {
        if (isRequesting)
        {
            onComplete?.Invoke(false, "요청을 처리하고 있습니다. 잠시만 기다려 주세요.");
            return;
        }

        long inventoryId = PlayerInventory.Instance.GetInventoryId(item);
        if (inventoryId <= 0)
        {
            onComplete?.Invoke(false, "서버에 없는 장비입니다.");
            return;
        }

        isRequesting = true;
        StartCoroutine(request(inventoryId));
    }

    // 응답 처리 → 잠금 해제 → 성공이면 PlayerInventory에 반영
    private void Finish<T>(ApiResult<T> result, Action<T> apply, string fallback, Action<bool, string> onComplete)
    {
        isRequesting = false;

        if (!result.IsSuccess)
        {
            onComplete?.Invoke(false, result.ToUserMessage(fallback));
            return;
        }

        apply(result.Data);
        onComplete?.Invoke(true, null);
    }
}