using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 가장 먼저 실행되게 고정
[DefaultExecutionOrder(-100)]
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private readonly List<OwnedItem> items = new();
    private readonly Dictionary<EquipSlotType, OwnedItem> equipped = new();

    // OwnedItem.instanceId(메모리용 GUID) → 서버 inventoryId
    private readonly Dictionary<string, long> inventoryIds = new();
    private readonly Dictionary<string, DateTime> acquiredAts = new();

    private static PlayerSaveData Data => GameManager.PlayerData.currentData;

    // 재화를 PlayerSaveData.currency에서 읽어오도록 구현
    public int Gold => Data.currency.currencyGold;
    public int Gem => Data.currency.currencyGem;
    public int Stamina => Data.currency.currencyEnergy;

    public event Action OnInventoryChanged;

    public IReadOnlyList<OwnedItem> Items => items;

    private void Awake()
    {
        Instance = this;

        if (!GameManager.PlayerData.isPlayerDataLoaded)
        {
            Debug.LogWarning("[PlayerInventory] 로그인 데이터가 없는 상태에서 실행되었습니다. "
                             + "로그인 씬을 거치지 않은 테스트 씬인지 확인하세요.");
            return;
        }

        Load();
    }

    private void Start()
    {
        ShowBattleRewards();
    }

    // 직전 전투의 보상상자 장비를 팝업으로 보여주고 비운다. 지급은 전투 씬에서 서버 응답으로 이미 반영됐다
    private void ShowBattleRewards()
    {
        var rewards = BattleResult.Rewards;
        BattleResult.Rewards = null;
        if (rewards == null || rewards.Count == 0)
            return;

        var rewardIds = rewards.Select(reward => reward.inventoryId).ToHashSet();
        var rewardItems = items.Where(item => rewardIds.Contains(inventoryIds[item.instanceId])).ToList();

        if (rewardItems.Count > 0)
            UIManager.Instance.Get<RewardBoxResultPopup>().Show(rewardItems);
    }

    public OwnedItem AddItem(long itemId)
    {
        var item = CreateNewItem(itemId);
        items.Add(item);
        PersistAndNotify();
        return item;
    }

    public void AddGem(int amount)
    {
        Data.currency.currencyGem += amount;
        PersistAndNotify();
    }

    public void AddGold(int amount)
    {
        Data.currency.currencyGold += amount;
        PersistAndNotify();
    }

    public bool TrySpendGem(int amount)
    {
        if (Gem < amount)
            return false;

        // 기존 코드는 gold를 차감하던 버그가 있어 gem 차감으로 수정
        Data.currency.currencyGem -= amount;
        PersistAndNotify();
        return true;
    }

    public OwnedItem GetEquipped(EquipSlotType slot)
        => equipped.GetValueOrDefault(slot);

    public void Equip(OwnedItem item)
    {
        var slot = item.Data.SlotType;

        if (equipped.TryGetValue(slot, out var current))
            current.isEquipped = false;

        item.isEquipped = true;
        equipped[slot] = item;
        PersistAndNotify();
    }

    public void Unequip(EquipSlotType slot)
    {
        if (!equipped.TryGetValue(slot, out var item))
            return;

        item.isEquipped = false;
        equipped.Remove(slot);
        PersistAndNotify();
    }

    //단일 레벨업
    public bool TryLevelUp(OwnedItem item)
    {
        if (item == null || !item.CanLevelUp)
            return false;

        var cost = item.NextLevelUpCost;
        if (Gold < cost)
            return false;

        Data.currency.currencyGold -= cost;
        item.level++;
        PersistAndNotify();
        return true;
    }

    // 일괄 레벨업
    public int BatchLevelUp(OwnedItem item)
    {
        var levelsGained = 0;
        while (TryLevelUp(item))
            levelsGained++;
        return levelsGained;
    }

    // 합성 재료: 같은 아이템, 같은 등급, 장착하지 않은 장비 (서버 findMaterials와 동일)
    public int CountSynthesisMaterials(OwnedItem item)
        => items.Count(i => i != item && !i.isEquipped && i.itemId == item.itemId && i.Grade == item.Grade);

    public bool CanSynthesize(OwnedItem item)
        => item != null
        && item.CanSynthesize
        && CountSynthesisMaterials(item) >= ItemLevelConfig.SynthesisMaterialCount;

    // 단일 합성 (같은 재료 두개)
    public bool TrySynthesize(OwnedItem item)
    {
        if (!CanSynthesize(item))
            return false;

        var materials = items
            .Where(i => i != item && i.itemId == item.itemId && i.Grade == item.Grade)
            .Take(ItemLevelConfig.SynthesisMaterialCount)
            .ToList();

        if (materials.Count < ItemLevelConfig.SynthesisMaterialCount)
            return false;

        foreach (var material in materials)
        {
            if (material.isEquipped)
                Unequip(material.Data.SlotType);

            items.Remove(material);
            inventoryIds.Remove(material.instanceId);
            acquiredAts.Remove(material.instanceId);
        }

        // 주의: Inventory에 등급 필드가 없어 합성 등급은 재로그인 시 기본 등급으로 돌아간다 (팀 결정 대기)
        item.grade = item.NextGrade;
        OnInventoryChanged?.Invoke();
        Save();
        return true;
    }

    // 일괄 합성
    public int BatchSynthesize(OwnedItem item)
    {
        var tiersGained = 0;
        while (TrySynthesize(item))
            tiersGained++;
        return tiersGained;
    }

    public int GetTotalStat(Func<OwnedItem, int> selector)
        => equipped.Values.Sum(selector);

    // OwnedItem 리스트 → PlayerSaveData.inventoryList (메모리만. 전투 씬 PlayerStats가 읽음)
    public void Save()
    {
        long playerId = Data.profile.playerId;

        Data.inventoryList = items.Select(item => new InventoryData
        {
            inventoryId = inventoryIds[item.instanceId],
            playerId = playerId,
            itemId = item.itemId,
            inventoryItemLevel = item.level,
            inventoryAcquiredAt = acquiredAts[item.instanceId],
            inventoryItemGrade = item.grade,
            inventoryEquipped = item.isEquipped
        }).ToList();
    }

    // PlayerSaveData.inventoryList → OwnedItem 리스트 (규칙은 InventoryRestorer)
    public void Load()
    {
        items.Clear();
        equipped.Clear();
        inventoryIds.Clear();
        acquiredAts.Clear();

        var restored = InventoryRestorer.Restore(Data.inventoryList, out var restoredEquipped);
        
        foreach (var (saved, item) in restored)
        {
            items.Add(item);
            inventoryIds[item.instanceId] = saved.inventoryId;
            acquiredAts[item.instanceId] = saved.inventoryAcquiredAt;
        }
        
        foreach (var pair in restoredEquipped)
            equipped[pair.Key] = pair.Value;
    }

    [ContextMenu("인벤토리 초기화")]
    public void ClearSave()
    {
        items.Clear();
        equipped.Clear();
        inventoryIds.Clear();
        acquiredAts.Clear();
        PersistAndNotify();
    }

    [ContextMenu("골드/보석 초기화")]
    public void ResetCurrency()
    {
        Data.currency.currencyGold = 0;
        Data.currency.currencyGem = 0;
        PersistAndNotify();
    }

    private void PersistAndNotify()
    {
        Save();
        OnInventoryChanged?.Invoke();
        GameManager.PlayerData.NotifyPlayerDataChanged();
    }

    // 새로 얻은 장비에 로컬 inventoryId를 부여 (서버 연동 시 서버의 Auto Increment로 대체)
    private OwnedItem CreateNewItem(long itemId)
    {
        var item = new OwnedItem(itemId);
        long nextId = inventoryIds.Count == 0 ? 1 : inventoryIds.Values.Max() + 1;

        inventoryIds[item.instanceId] = nextId;
        acquiredAts[item.instanceId] = DateTime.Now;
        return item;
    }

    private static bool IsKnownItem(long itemId)
        => GameManager.JsonData.ItemDataDic.ContainsKey(itemId);
    
    // ===== 서버 응답 반영: 서버가 계산·저장한 결과로 덮어쓴다 (클라에서 계산하지 않음) =====

    public long GetInventoryId(OwnedItem item)
        => item != null && inventoryIds.TryGetValue(item.instanceId, out var id) ? id : 0;
    
    public void ApplyEquip(EquipResponse response)
    {
        if (response.unequipped != null)
            SetEquippedState(response.unequipped.inventoryId, false);

        if (response.equipped != null)
            SetEquippedState(response.equipped.inventoryId, response.equipped.inventoryEquipped);

        SyncAndNotify();
    }

    public void ApplyUnequip(InventoryData response)
    {
        SetEquippedState(response.inventoryId, response.inventoryEquipped);
        SyncAndNotify();
    }
    
    public void ApplyLevelUp(long inventoryId, int level, int currencyGold)
    {
        var item = FindByInventoryId(inventoryId);
        if (item != null)
            item.level = level;
    
        Data.currency.currencyGold = currencyGold;   // 빼기가 아니라 서버 값으로 덮어쓰기
        SyncAndNotify();
    }
    
    public void ApplySynthesis(SynthesizeResponse response)
    {
        // 재료는 서버가 고른 것만 제거
        if (response.consumedInventoryIds != null)
        {
            foreach (var consumedId in response.consumedInventoryIds)
                RemoveByInventoryId(consumedId);
        }
    
        var item = FindByInventoryId(response.inventoryId);
        if (item != null && response.inventoryItemGrade.HasValue)
            item.grade = response.inventoryItemGrade.Value;
    
        SetEquippedState(response.inventoryId, response.isEquipped);
        SyncAndNotify();
    }
    
    private OwnedItem FindByInventoryId(long inventoryId)
        => items.FirstOrDefault(i => inventoryIds.TryGetValue(i.instanceId, out var id) && id == inventoryId);
    
    private void SetEquippedState(long inventoryId, bool isEquipped)
    {
        var item = FindByInventoryId(inventoryId);
        if (item == null)
        {
            Debug.LogWarning($"[PlayerInventory] inventoryId {inventoryId} 장비를 찾지 못했습니다.");
            return;
        }
    
        var slot = item.Data.SlotType;
    
        if (isEquipped)
        {
            if (equipped.TryGetValue(slot, out var current) && current != item)
                current.isEquipped = false;
    
            item.isEquipped = true;
            equipped[slot] = item;
        }
        else
        {
            item.isEquipped = false;
            if (equipped.TryGetValue(slot, out var current) && current == item)
                equipped.Remove(slot);
        }
    }
    
    private void RemoveByInventoryId(long inventoryId)
    {
        var item = FindByInventoryId(inventoryId);
        if (item == null) return;
    
        if (item.isEquipped)
            SetEquippedState(inventoryId, false);
    
        items.Remove(item);
        inventoryIds.Remove(item.instanceId);
        acquiredAts.Remove(item.instanceId);
    }

    public List<OwnedItem> ApplyPurchase(PurchaseResponse response)
    {
        Data.currency.currencyGold = response.currencyGold;   // 서버 값으로 덮어쓰기
        Data.currency.currencyGem = response.currencyGem;

        var added = new List<OwnedItem>();
        foreach (var reward in response.rewardedItems ?? new List<InventoryData>())
        {
            var item = AddServerItem(reward);
            if (item != null) added.Add(item);
        }

        SyncAndNotify();
        return added;
    }

    // 서버가 지급한 장비 목록을 추가한다 (개발용 장비 지급)
    public List<OwnedItem> AddServerItems(List<InventoryData> saved)
    {
        var added = new List<OwnedItem>();
        foreach (var data in saved)
        {
            var item = AddServerItem(data);
            if (item != null) added.Add(item);
        }

        SyncAndNotify();
        return added;
    }

    // 서버가 만든 장비를 추가한다 (ID는 서버 값 그대로, 이미 있으면 무시)
    private OwnedItem AddServerItem(InventoryData saved)
    {
        if (FindByInventoryId(saved.inventoryId) != null)
        {
            Debug.LogWarning($"[PlayerInventory] inventoryId {saved.inventoryId}는 이미 있어서 건너뜁니다.");
            return null;
        }

        if (!IsKnownItem(saved.itemId))
        {
            Debug.LogWarning($"[PlayerInventory] 아이템 데이터에 없는 itemId {saved.itemId}는 건너뜁니다.");
            return null;
        }

        var item = new OwnedItem(saved.itemId) { level = saved.inventoryItemLevel };
        if (saved.inventoryItemGrade.HasValue)
            item.grade = saved.inventoryItemGrade.Value;

        items.Add(item);
        inventoryIds[item.instanceId] = saved.inventoryId;
        acquiredAts[item.instanceId] = saved.inventoryAcquiredAt;
        return item;
    }
    
    private void SyncAndNotify()
    {
        Save();   
        OnInventoryChanged?.Invoke();
        GameManager.PlayerData.NotifyPlayerDataChanged();
    }
}