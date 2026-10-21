using UnityEngine;

public interface ILootReceiver
{
    Vector2 Position { get; }

    float LootRadius { get; }

    void OnLoot(DropItemType type);
}
