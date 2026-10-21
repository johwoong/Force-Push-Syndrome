using UnityEngine;

[RequireComponent(typeof(PlayerLootReceiver))]
public class BattleSfx : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float goldVolume = 0.5f;   // 자주 먹으므로 작게
    [SerializeField][Range(0f, 1f)] private float itemVolume = 1f;     // 포션·자석·폭탄은 드물고 중요하므로 크게

    private PlayerLootReceiver lootReceiver;

    private void Awake()
    {
        lootReceiver = GetComponent<PlayerLootReceiver>();
    }

    // 구독/해제는 OnEnable/OnDisable 쌍으로 (HitFeedback과 같은 규칙)
    private void OnEnable() => lootReceiver.OnPickedUp += HandlePickedUp;
    private void OnDisable() => lootReceiver.OnPickedUp -= HandlePickedUp;

    private void HandlePickedUp(DropItemType type)
    {
        switch (type)
        {
            // Gold1~Gold4 한 번에 (enum이 Gold1..Gold4 순서로 연속이라 범위 비교 가능)
            case >= DropItemType.Gold1 and <= DropItemType.Gold4:
                Play(GameConstants.Sfx.GOLD, goldVolume, Random.Range(0.95f, 1.1f));
                break;

            case DropItemType.Potion:
                Play(GameConstants.Sfx.POTION, itemVolume);
                break;

            case DropItemType.Magnet:
                Play(GameConstants.Sfx.MAGNET, itemVolume);
                break;

            case DropItemType.Bomb:
                Play(GameConstants.Sfx.BOMB, itemVolume);
                break;

                // 경험치 보석·상자·행운상자는 지금은 소리 없음 (필요하면 case 추가)
        }
    }

    private static void Play(string sfx, float volume, float pitch = 1f)
        => GameManager.Sound.PlaySFX(sfx, volume, pitch);
}