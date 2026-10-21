using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct AttractMovementSystem : ISystem
{
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
        LootTarget target = SystemAPI.GetSingleton<LootTarget>();
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        new AttractMovementJob
        {
            TargetPosition = target.Position,
            Config = SystemAPI.GetSingleton<DropItemConfig>(),
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = ecb.AsParallelWriter()
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct AttractMovementJob : IJobEntity
{
    public float2 TargetPosition;
    public DropItemConfig Config;
    public float DeltaTime;
    public EntityCommandBuffer.ParallelWriter Ecb;

    private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, ref DropPosition position, ref Attract attract)
    {
        if (attract.Phase == AttractPhase.Init)
        {
            attract.Origin = position.Value;
            attract.Dir = math.normalizesafe(position.Value - TargetPosition, new float2(0f, 1f));
            attract.Elapsed = 0f;
            attract.Phase = AttractPhase.Recoil;
        }

        if (attract.Phase == AttractPhase.Recoil)
        {
            attract.Elapsed += DeltaTime;
            float t = math.saturate(attract.Elapsed / Config.RecoilDuration);
            float easeOut = 1f - (1f - t) * (1f - t);
            position.Value = attract.Origin + attract.Dir * (Config.RecoilDistance * easeOut);

            if (t < 1f) return;

            attract.Phase = AttractPhase.Chase;
            attract.Speed = Config.ChaseStartSpeed;
            return;
        }

        attract.Speed += Config.ChaseAcceleration * DeltaTime;

        float2 toTarget = TargetPosition - position.Value;
        float distSq = math.lengthsq(toTarget);
        float step = attract.Speed * DeltaTime;

        if (step * step >= distSq)
            position.Value = TargetPosition;
        else
            position.Value += toTarget * (step / math.sqrt(distSq));

        if (math.distancesq(position.Value, TargetPosition) <= Config.ArriveRadius * Config.ArriveRadius)
            Ecb.AddComponent<Arrived>(sortKey, entity);
    }
}
