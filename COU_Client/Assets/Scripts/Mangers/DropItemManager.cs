using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class DropItemManager : MonoSingleton<DropItemManager>
{
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private float dropSpreadRadius = 1f;
    [SerializeField] private float recoilDistance = 1f;
    [SerializeField] private float recoilDuration = 0.15f;
    [SerializeField] private float chaseStartSpeed = 5f;
    [SerializeField] private float chaseAcceleration = 40f;
    [SerializeField] private float arriveRadius = 0.2f;

    private readonly List<Transform> views = new();
    private readonly Stack<int> freeSlots = new();

    private ILootReceiver receiver;
    private EntityManager entityManager;
    private EntityArchetype expGemArchetype;
    private EntityArchetype itemArchetype;
    private EntityQuery arrivedQuery;
    private EntityQuery attractQuery;
    private EntityQuery groundedExpGemQuery;
    private EntityQuery dropItemQuery;
    private Entity configEntity;
    private Entity lootTargetEntity;
    private TransformAccessArray attractViews;
    private int attractOrderVersion = -1;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        expGemArchetype = entityManager.CreateArchetype(
            typeof(DropItem), typeof(DropPosition), typeof(DropView), typeof(Grounded), typeof(ExpGem));
        itemArchetype = entityManager.CreateArchetype(
            typeof(DropItem), typeof(DropPosition), typeof(DropView), typeof(Grounded));

        arrivedQuery = entityManager.CreateEntityQuery(typeof(Arrived), typeof(DropItem), typeof(DropView));
        attractQuery = entityManager.CreateEntityQuery(typeof(Attract), typeof(DropPosition), typeof(DropView));
        groundedExpGemQuery = entityManager.CreateEntityQuery(typeof(Grounded), typeof(ExpGem));
        dropItemQuery = entityManager.CreateEntityQuery(typeof(DropItem));

        configEntity = entityManager.CreateEntity(typeof(DropItemConfig));
        attractViews = new TransformAccessArray(256);
    }

    public void RegisterReceiver(ILootReceiver lootReceiver)
    {
        receiver = lootReceiver;
    }

    public void UnregisterReceiver(ILootReceiver lootReceiver)
    {
        if (receiver == lootReceiver) receiver = null;
    }

    public void Spawn(DropItemType type, Vector2 position)
    {
        Transform view = GameManager.ObjectPool.GetObject(prefabs[(int)type], position, Quaternion.identity).transform;

        int slot;
        if (freeSlots.Count > 0)
        {
            slot = freeSlots.Pop();
            views[slot] = view;
        }
        else
        {
            slot = views.Count;
            views.Add(view);
        }

        Entity entity = entityManager.CreateEntity(type <= DropItemType.ExpGem4 ? expGemArchetype : itemArchetype);
        entityManager.SetComponentData(entity, new DropItem { Type = type });
        entityManager.SetComponentData(entity, new DropPosition { Value = position });
        entityManager.SetComponentData(entity, new DropView { Slot = slot });
    }

    // 드롭 테이블 판정: 몬스터 역할의 테이블에서 그룹마다 가중치로 1행을 뽑아 count개 생성한다. None이면 건너뜀
    public void SpawnTable(MonsterType type, Vector2 position)
    {
        IReadOnlyList<DropTableEntryData> entries = GameManager.JsonData.GetDropTableFromJson(type);
        if (entries == null) return;

        int dropIndex = 0;
        int start = 0;
        while (start < entries.Count)
        {
            int end = start;
            int totalWeight = 0;
            while (end < entries.Count && entries[end].dropGroup == entries[start].dropGroup)
            {
                totalWeight += entries[end].weight;
                end++;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            for (int i = start; i < end; i++)
            {
                roll -= entries[i].weight;
                if (roll >= 0) continue;

                if (!entries[i].IsNone)
                {
                    // 1번째는 정위치, 2~9번째는 8각형 꼭짓점, 10번째부터는 다시 정위치
                    for (int n = 0; n < entries[i].count; n++, dropIndex++)
                    {
                        Vector2 offset = Vector2.zero;
                        if (dropIndex >= 1 && dropIndex <= 8)
                        {
                            float angle = (dropIndex - 1) * 45f * Mathf.Deg2Rad;
                            offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dropSpreadRadius;
                        }
                        Spawn(entries[i].Type, position + offset);
                    }
                }
                break;
            }

            start = end;
        }
    }

    public void AttractAllExpGems()
    {
        entityManager.AddComponent<Attract>(groundedExpGemQuery);
        entityManager.RemoveComponent<Grounded>(groundedExpGemQuery);
    }

    private void Update()
    {
        if (receiver is Object receiverObject && receiverObject == null) receiver = null;

        if (receiver != null)
        {
            if (!entityManager.Exists(lootTargetEntity))
                lootTargetEntity = entityManager.CreateEntity(typeof(LootTarget));

            entityManager.SetComponentData(lootTargetEntity, new LootTarget
            {
                Position = receiver.Position,
                Radius = receiver.LootRadius
            });
        }
        else if (entityManager.Exists(lootTargetEntity))
        {
            entityManager.DestroyEntity(lootTargetEntity);
        }

        entityManager.SetComponentData(configEntity, new DropItemConfig
        {
            RecoilDistance = recoilDistance,
            RecoilDuration = recoilDuration,
            ChaseStartSpeed = chaseStartSpeed,
            ChaseAcceleration = chaseAcceleration,
            ArriveRadius = arriveRadius
        });
    }

    private void LateUpdate()
    {
        ProcessArrived();
        SyncViews();
    }

    private void ProcessArrived()
    {
        if (arrivedQuery.IsEmpty) return;

        using NativeArray<DropItem> items = arrivedQuery.ToComponentDataArray<DropItem>(Allocator.Temp);
        using NativeArray<DropView> arrivedViews = arrivedQuery.ToComponentDataArray<DropView>(Allocator.Temp);

        for (int i = 0; i < items.Length; i++)
        {
            receiver?.OnLoot(items[i].Type);

            int slot = arrivedViews[i].Slot;
            GameManager.ObjectPool.ReturnObject(views[slot].gameObject);
            views[slot] = null;
            freeSlots.Push(slot);
        }

        entityManager.DestroyEntity(arrivedQuery);
    }

    private void SyncViews()
    {
        int version = entityManager.GetComponentOrderVersion<Attract>();
        if (version != attractOrderVersion)
        {
            attractOrderVersion = version;

            for (int i = attractViews.length - 1; i >= 0; i--)
                attractViews.RemoveAtSwapBack(i);

            using NativeArray<DropView> attractSlots = attractQuery.ToComponentDataArray<DropView>(Allocator.Temp);
            for (int i = 0; i < attractSlots.Length; i++)
                attractViews.Add(views[attractSlots[i].Slot]);
        }

        if (attractViews.length == 0) return;

        NativeArray<DropPosition> positions = attractQuery.ToComponentDataArray<DropPosition>(Allocator.TempJob);
        new ViewSyncJob { Positions = positions }.Schedule(attractViews).Complete();
        positions.Dispose();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (!attractViews.isCreated) return;

        attractViews.Dispose();

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        entityManager.DestroyEntity(dropItemQuery);
        entityManager.DestroyEntity(configEntity);
        if (entityManager.Exists(lootTargetEntity)) entityManager.DestroyEntity(lootTargetEntity);
    }

    [BurstCompile]
    private struct ViewSyncJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<DropPosition> Positions;

        public void Execute(int index, TransformAccess transform)
        {
            float2 position = Positions[index].Value;
            transform.position = new Vector3(position.x, position.y, 0f);
        }
    }
}
