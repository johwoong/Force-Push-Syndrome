using UnityEngine;
using UnityEngine.UI;

// 장비창의 스탯 상세 아이콘 버튼: 누르면 상세 팝업을 연다
[RequireComponent(typeof(Button))]
public class PlayerStatDetailButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Open);
    }

    private void Open()
    {
        UIManager.Instance.Get<PlayerStatDetailPopup>().Open();
    }
}