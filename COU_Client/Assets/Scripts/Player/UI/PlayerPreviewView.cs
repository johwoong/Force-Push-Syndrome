using UnityEngine;
using UnityEngine.UI;

// 장비창 UI에 붙여서 플레이어 미리보기를 표시한다
// 장착 장비가 바뀌면 무기 외형을 갱신한다
[RequireComponent(typeof(RawImage))]
public class PlayerPreviewView : MonoBehaviour
{
    [SerializeField] private PlayerPreview previewPrefab;
    [SerializeField] private Vector3 spawnPosition = new(10000f, 0f, 0f);   // 메인 카메라에 잡히지 않는 먼 위치

    private RawImage rawImage;
    private PlayerPreview preview;
    private bool isSubscribed;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    private void Start()
    {
        if (previewPrefab == null)
        {
            Debug.LogError("[PlayerPreviewView] Preview Prefab이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        preview = Instantiate(previewPrefab, spawnPosition, Quaternion.identity);
        rawImage.texture = preview.Texture;

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += RefreshWeapon;
            isSubscribed = true;
        }

        RefreshWeapon();
        preview.SetVisible(isActiveAndEnabled);
    }

    private void OnEnable()
    {
        if (preview != null)
        {
            preview.SetVisible(true);
            RefreshWeapon();   // 닫혀 있는 동안 바뀐 장비 반영
        }
    }

    private void OnDisable()
    {
        if (preview != null)
            preview.SetVisible(false);
    }

    private void OnDestroy()
    {
        if (isSubscribed && PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= RefreshWeapon;

        if (preview != null)
            Destroy(preview.gameObject);
    }

    private void RefreshWeapon()
    {
        if (preview == null) return;

        var weapon = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetEquipped(EquipSlotType.Weapon)
            : null;

        preview.SetWeapon(weapon != null ? weapon.itemId : (long?)null);
    }
}