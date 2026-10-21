using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(AttractMovementSystem))]
public partial struct LootDetectionSystem : ISystem
{
    private const float DetectInterval = 1f / 15f;

    private float elapsed;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LootTarget>();
        state.RequireForUpdate<DropItemConfig>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        elapsed += SystemAPI.Time.DeltaTime;
        if (elapsed < DetectInterval) return;
        elapsed %= DetectInterval;

        LootTarget target = SystemAPI.GetSingleton<LootTarget>();
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        new LootDetectionJob
        {
            TargetPosition = target.Position,
            RadiusSq = target.Radius * target.Radius,
            Ecb = ecb.AsParallelWriter()
        }.ScheduleParallel();
    }
}

[BurstCompile]
[WithAll(typeof(Grounded))]
public partial struct LootDetectionJob : IJobEntity
{
    public float2 TargetPosition;
    public float RadiusSq;
    public EntityCommandBuffer.ParallelWriter Ecb;

    private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, in DropPosition position)
    {
        if (math.distancesq(position.Value, TargetPosition) > RadiusSq) return;

        Ecb.RemoveComponent<Grounded>(sortKey, entity);
        Ecb.AddComponent<Attract>(sortKey, entity);
    }
}
