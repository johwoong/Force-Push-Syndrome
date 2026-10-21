using Unity.Entities;
using Unity.Mathematics;

public struct DropItem : IComponentData
{
    public DropItemType Type;
}

public struct DropPosition : IComponentData
{
    public float2 Value;
}

public struct DropView : IComponentData
{
    public int Slot;
}

public struct Grounded : IComponentData { }

public struct ExpGem : IComponentData { }

public enum AttractPhase : byte
{
    Init = 0,
    Recoil,
    Chase
}

public struct Attract : IComponentData
{
    public AttractPhase Phase;
    public float2 Origin;
    public float2 Dir;
    public float Elapsed;
    public float Speed;
}

public struct Arrived : IComponentData { }

public struct LootTarget : IComponentData
{
    public float2 Position;
    public float Radius;
}

public struct DropItemConfig : IComponentData
{
    public float RecoilDistance;
    public float RecoilDuration;
    public float ChaseStartSpeed;
    public float ChaseAcceleration;
    public float ArriveRadius;
}
