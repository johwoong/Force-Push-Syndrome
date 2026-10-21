using UnityEngine;
using UnityEngine.InputSystem;

public class DropItemTestDriver : MonoBehaviour, ILootReceiver
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lootRadius = 2f;
    [SerializeField] private float radiusStep = 0.5f;
    [SerializeField] private float spawnRange = 8f;
    [SerializeField] private float massSpawnRange = 30f;
    [SerializeField] private int massSpawnCount = 5000;
    [SerializeField] private bool logLoot = false;

    private int lootCount;

    public Vector2 Position => transform.position;
    public float LootRadius => lootRadius;

    private void OnEnable()
    {
        GameManager.DropItem.RegisterReceiver(this);
    }

    public void OnLoot(DropItemType type)
    {
        lootCount++;
        if (logLoot) Debug.Log($"[DropItemTest] OnLoot {type} (total {lootCount})");

        if (type == DropItemType.Magnet) GameManager.DropItem.AttractAllExpGems();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        Vector2 move = Vector2.zero;
        if (keyboard.wKey.isPressed) move.y += 1f;
        if (keyboard.sKey.isPressed) move.y -= 1f;
        if (keyboard.aKey.isPressed) move.x -= 1f;
        if (keyboard.dKey.isPressed) move.x += 1f;
        transform.Translate(move.normalized * (speed * Time.deltaTime));

        if (keyboard.eKey.wasPressedThisFrame)
        {
            lootRadius += radiusStep;
            Debug.Log($"[DropItemTest] LootRadius {lootRadius}");
        }
        if (keyboard.qKey.wasPressedThisFrame)
        {
            lootRadius = Mathf.Max(0f, lootRadius - radiusStep);
            Debug.Log($"[DropItemTest] LootRadius {lootRadius}");
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
            for (int i = 0; i <= (int)DropItemType.Bomb; i++)
                GameManager.DropItem.Spawn((DropItemType)i, Position + Random.insideUnitCircle * spawnRange);

        if (keyboard.digit2Key.wasPressedThisFrame)
            for (int i = 0; i < massSpawnCount; i++)
                GameManager.DropItem.Spawn((DropItemType)Random.Range(0, (int)DropItemType.Gold4 + 1),
                    Position + Random.insideUnitCircle * massSpawnRange);

        if (keyboard.digit3Key.wasPressedThisFrame)
            GameManager.DropItem.Spawn(DropItemType.ExpGem1, Position);

        if (keyboard.mKey.wasPressedThisFrame)
            GameManager.DropItem.AttractAllExpGems();

        if (keyboard.uKey.wasPressedThisFrame)
            GameManager.DropItem.UnregisterReceiver(this);
        if (keyboard.rKey.wasPressedThisFrame)
            GameManager.DropItem.RegisterReceiver(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, lootRadius);
    }
}
