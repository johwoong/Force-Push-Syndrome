using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class SynthesisEntryButton : MonoBehaviour
{
    private void Awake()
        => GetComponent<Button>().onClick.AddListener(OnClicked);

    private void OnClicked()
    {
        var inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("[합성] PlayerInventory가 아직 준비되지 않았습니다.");
            return;
        }

        // 재료가 충분한 장비를 우선하고, 없으면 합성 가능한(레어가 아닌) 아무 장비나 연다
        var target = inventory.Items.FirstOrDefault(i => inventory.CanSynthesize(i))
                   ?? inventory.Items.FirstOrDefault(i => i.CanSynthesize);

        if (target == null)
        {
            UIManager.Instance.Get<MessagePopup>().ShowAlert("합성할 수 있는 장비가 없습니다.");
            return;
        }

        UIManager.Instance.Get<SynthesisWindow>().Show(target);
    }
}
