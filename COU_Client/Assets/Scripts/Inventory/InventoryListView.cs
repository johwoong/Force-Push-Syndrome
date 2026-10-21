using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryListView : MonoBehaviour
{
    public enum SortMode { Slot, Level, Grade }

    [SerializeField] private Transform content;
    [SerializeField] private ItemSlotView itemSlotPrefab;

    [SerializeField] private SynthesisWindow synthesisWindowRef;

    private readonly List<ItemSlotView> pool = new();

    public SortMode CurrentSortMode { get; private set; } = SortMode.Slot;

    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    // 로그인 직후에는 PlayerInventory.Awake보다 OnEnable이 먼저 실행될 수 있어서 Start에서 한 번 더 시도
    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed || PlayerInventory.Instance == null)
            return;

        subscribed = true;
        PlayerInventory.Instance.OnInventoryChanged += Refresh;
        if (synthesisWindowRef != null)
            synthesisWindowRef.OnShown += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (!subscribed)
            return;

        subscribed = false;
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= Refresh;
        if (synthesisWindowRef != null)
            synthesisWindowRef.OnShown -= Refresh;
    }

    public SortMode CycleSortMode()
    {
        CurrentSortMode = (SortMode)(((int)CurrentSortMode + 1) % 3);
        Refresh();
        UITween.Stagger(pool, 0.02f);   // 정렬이 바뀌면 슬롯이 차례로 다시 나온다
        return CurrentSortMode;
    }

    private void Refresh()
    {
        if (PlayerInventory.Instance == null)
            return;

        // 합성창 목록에서는 더 이상 합성할 수 없는(레어) 장비를 숨긴다
        var forSynthesis = synthesisWindowRef != null;
        IEnumerable<OwnedItem> items = PlayerInventory.Instance.Items
            .Where(i => !i.isEquipped && (!forSynthesis || i.CanSynthesize));

        items = CurrentSortMode switch
        {
            SortMode.Slot => items.OrderBy(i => i.Data.SlotType == EquipSlotType.Weapon ? 0 : 1),
            SortMode.Level => items.OrderByDescending(i => i.level),
            SortMode.Grade => items.OrderByDescending(i => (int)i.Grade),
            _ => items
        };

        var list = items.ToList();

        while (pool.Count < list.Count)
            pool.Add(Instantiate(itemSlotPrefab, content));

        for (var i = 0; i < pool.Count; i++)
        {
            var active = i < list.Count;
            pool[i].gameObject.SetActive(active);
            if (active)
                pool[i].Setup(list[i], synthesisWindowRef);
        }
    }
}
