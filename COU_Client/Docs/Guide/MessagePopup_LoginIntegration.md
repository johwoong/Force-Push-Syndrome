# MessagePopup 로그인 연동 가이드

로그인 결과 알림과 확인 팝업은 `MessagePopup`으로 띄운다. 모든 씬에서 같은 방법으로 호출한다.

```csharp
public class MessagePopup : UIPopup
{
    public void ShowAlert(string message, Action onConfirm = null);                    // [확인] 버튼만
    public void ShowConfirm(string message, Action onConfirm, Action onCancel = null); // [취소] [확인]
}
```

## 준비
- 영속 UIManager는 첫 씬이 로드되기 전에 자동으로 생성되므로 LogInScene에 추가로 둘 것이 없다.
- 씬에 EventSystem만 있으면 된다. LogInScene에는 이미 있다.

## 호출
- `GameManager.UI.Get<MessagePopup>().ShowAlert(message);`
- 콜백은 팝업이 닫힐 때 정확히 한 번 호출된다.

| 모드 | [확인] | [취소] | Esc |
|---|---|---|---|
| ShowAlert | `onConfirm` | - | `onConfirm` |
| ShowConfirm | `onConfirm` | `onCancel` | `onCancel` |

- 콜백 안에서 다시 `ShowAlert`/`ShowConfirm`을 호출해도 된다. 새 메시지가 열린 채로 남는다.
- 팝업이 열려 있는 동안 다시 호출하면 메시지와 콜백을 덮어쓴다. 이전 콜백은 호출되지 않는다.
- 팝업이 떠 있는 동안에는 아래 UI를 클릭할 수 없다.

## 예시: LogInMainCanvas
`Debug.Log(message)`를 `ShowAlert(message)`로 바꾼다. 회원가입에 성공하면 [확인]을 누를 때 자동으로 로그인한다.

```csharp
public void OnClickLogin()
{
    bool success = GameManager.LocalLogin.Login(
        playerIDInputField.text, passwordInputField.text, out string message
    );

    GameManager.UI.Get<MessagePopup>().ShowAlert(message);

    if (success)
    {
        // 플레이어 데이터 로드 후 게임 씬으로 이동
    }
}

public void OnClickSignUp()
{
    bool success = GameManager.LocalLogin.SignUp(
        playerIDInputField.text, passwordInputField.text, out string message
    );

    if (success)
        GameManager.UI.Get<MessagePopup>().ShowAlert(message, OnClickLogin); // "회원가입이 완료되었습니다." → [확인] → 로그인 결과 알림
    else
        GameManager.UI.Get<MessagePopup>().ShowAlert(message);
}
```

- 로그인 성공 알림을 띄우지 않고 바로 씬을 이동하려면 실패했을 때만 `ShowAlert`를 호출한다.
- 알림을 확인한 뒤에 씬을 이동하려면 이동 코드를 `onConfirm`으로 넘긴다.
- MessagePopup은 씬을 전환해도 유지된다.

## 로그인 UI를 UIView로 바꿀 때
`UIManager.Instance.Get<T>()`로 로그인 UI를 찾게 하려면 다음과 같이 바꾼다.

1. 로그인 UI 클래스가 `UIView`(팝업이면 `UIPopup`)를 상속하게 한다.
2. 캔버스 3개(HUD/Screen/Popup)의 부모 오브젝트에 `SceneUIRoot`를 붙인다. `BattleUI.prefab`, `MainUI.prefab`과 같은 구조다.
3. `SceneUIRoot`의 `hudCanvas`/`screenCanvas`/`popupCanvas`에 각 캔버스를 연결한다. 뷰는 해당 캔버스의 자식으로 둔다.

- 씬이 로드되면 `SceneUIRoot`가 캔버스 아래의 뷰를 등록하고, 씬이 내려가면 해제한다.
- Overlay 레이어는 영속 UIManager가 소유한다. 씬 쪽에는 OverlayCanvas를 두지 않는다.
