using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// InventoryData 목록 → OwnedItem 목록 + 슬롯별 장착 장비
// PlayerInventory.Load(메인 씬)와 PlayerStats(전투 씬)가 같은 규칙으로 복원하도록 한 곳에 둔다
public static class InventoryRestorer
{
    public static List<(InventoryData saved, OwnedItem item)> Restore(
        IEnumerable<InventoryData> list, out Dictionary<EquipSlotType, OwnedItem> equipped)
    {
        var result = new List<(InventoryData, OwnedItem)>();
        var seenIds = new HashSet<long>();
        equipped = new Dictionary<EquipSlotType, OwnedItem>();

        if (list == null) return result;

        // 서버와 같게 inventoryId 오름차순으로 처리
        foreach (var saved in list.Where(s => s != null).OrderBy(s => s.inventoryId))
        {
            // 규칙 1: 아이템 데이터에 없는 itemId는 건너뜀
            if (!GameManager.JsonData.ItemDataDic.ContainsKey(saved.itemId))
            {
                Debug.LogWarning($"[InventoryRestorer] 아이템 데이터에 없는 itemId {saved.itemId}는 건너뜁니다.");
                continue;
            }

            // 규칙 2: inventoryId가 겹치면 뒤의 것을 버림 (예외를 던지지 않음)
            if (!seenIds.Add(saved.inventoryId))
            {
                Debug.LogWarning($"[InventoryRestorer] inventoryId {saved.inventoryId}가 중복되어 건너뜁니다.");
                continue;
            }

            var item = new OwnedItem(saved.itemId) { level = saved.inventoryItemLevel };
            if (saved.inventoryItemGrade.HasValue)
                item.grade = saved.inventoryItemGrade.Value;

            // 규칙 3: 슬롯당 장착 1개, 작은 inventoryId 우선 (서버와 동일)
            var slot = item.Data.SlotType;
            if (saved.inventoryEquipped && !equipped.ContainsKey(slot))
            {
                item.isEquipped = true;
                equipped[slot] = item;
            }

            result.Add((saved, item));
        }

        return result;
    }
}