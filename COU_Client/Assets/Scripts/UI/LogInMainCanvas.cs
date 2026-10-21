using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class LogInMainCanvas : MonoBehaviour
{
    [FormerlySerializedAs("playerIDInputField")]
    [SerializeField] private TMP_InputField playerIdInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Popup_Alarm popUp_Alarm;

    private const string AppUpdateRequiredMessage = "앱 업데이트가 필요합니다.";

    // 서버 정적 데이터 확인이 끝나기 전에는 로그인/가입을 막는다
    private bool isStaticDataChecked;
    private bool isAppUpdateRequired;
    
    // 자동 로그인 [확인] 후 데이터를 불러오는 중
    private bool isAutoLoggingIn;

    private void Awake()
    {
        popUp_Alarm.Close();
    }

    private void Start()
    {
        //GameManager.Sound.PlayBGM("EX"); 예시

        StartCoroutine(GameManager.JsonData.FetchStaticData(appUpdateRequired =>
        {
            isStaticDataChecked = true;
            isAppUpdateRequired = appUpdateRequired;

            if (appUpdateRequired)
            {
                popUp_Alarm.Show(AppUpdateRequiredMessage);
                return;
            }

            ShowAutoLoginPopup();
        }));
    }

    // ===== 자동 로그인 =====
    
    // 이전에 게임에 들어간 적이 있는 계정이 저장되어 있으면 자동 로그인 여부를 묻는다
    // 저장된 세션이 없으면 아무것도 하지 않고 직접 로그인을 기다린다
    private void ShowAutoLoginPopup()
    {
        if (!GameManager.ServerLogin.TryGetSavedLoginId(out string savedLoginId)) return;
        
        GameManager.UI.Get<MessagePopup>().ShowConfirm(
            $"{savedLoginId} 계정으로 자동 로그인합니다.",
            StartAutoLogin,                                         // [확인]
            () => Debug.Log("[Login] 자동 로그인 취소 -> 직접 로그인")   // [취소]: 팝업만 닫힘
            );
    }

    private void StartAutoLogin()
    {
        if (!GameManager.ServerLogin.TryRestoreSession())
        {
            popUp_Alarm.Show("자동 로그인 정보를 불러오지 못했습니다. 직접 로그인해 주세요.");
            return;
        }

        isAutoLoggingIn = true;
        Debug.Log($"[Login] 자동 로그인 시도: accountLoginId = {GameManager.ServerLogin.CurrentAccount.accountLoginId}");
        UIManager.Instance.Loading.Show();   // [로딩] 데이터 로드부터 로딩창
        StartCoroutine(GameManager.ServerLoad.LoadPlayerData(OnPlayerDataLoaded));
    }
    
    // ===== 직접 로그인 / 회원가입 =====
    
    private bool CanProceed()
    {
        if (!isStaticDataChecked)
        {
            popUp_Alarm.Show("게임 데이터를 확인하고 있습니다. 잠시 후 다시 시도해 주세요.");
            return false;
        }

        if (isAppUpdateRequired)
        {
            popUp_Alarm.Show(AppUpdateRequiredMessage);
            return false;
        }

        if (isAutoLoggingIn)
        {
            popUp_Alarm.Show("자동 로그인 중입니다. 잠시만 기다려 주세요.");
            return false;
        }

        return true;
    }

    public void OnClickLogin()
    {
        if (!CanProceed()) return;

        TryLogin(playerIdInputField.text, passwordInputField.text);
    }

    public void OnClickSignUp()
    {
        if (!CanProceed()) return;

        StartCoroutine(GameManager.ServerLogin.SignUp(
            playerIdInputField.text, passwordInputField.text,
            (success, message) =>
            {
                if (success)
                {
                    // 회원가입 완료 안내 -> 확인 버튼을 누르면 자동으로 로그인
                    popUp_Alarm.Show(message, () => TryLogin(playerIdInputField.text, passwordInputField.text));
                    Debug.Log("[Login] 회원가입 성공");
                }
                else
                {
                    popUp_Alarm.Show(message);
                }
            }
        ));
    }

    private void TryLogin(string playerId, string password)
    {
        StartCoroutine(GameManager.ServerLogin.Login(playerId, password, (success, message) =>
            {
                if (!success)
                {
                    popUp_Alarm.Show(message);
                    return;
                }

                Debug.Log($"[Login] 서버 로그인 성공: accountId={GameManager.ServerLogin.CurrentAccount.accountId}");

                // 로그인 성공 → 로딩창을 띄우고 플레이어 데이터 불러오기
                UIManager.Instance.Loading.Show();   // [로딩]
                StartCoroutine(GameManager.ServerLoad.LoadPlayerData(OnPlayerDataLoaded));
            }
        ));
    }
    
    // 직접 로그인 / 자동 로그인 공통: 데이터 로드 결과 처리
    private void OnPlayerDataLoaded(bool success, string message)
    {
        bool wasAutoLogin = isAutoLoggingIn;
        isAutoLoggingIn = false;
        
        if (!success)
        {
            UIManager.Instance.Loading.Hide();   // [로딩] 안내 팝업이 로딩창에 가려지지 않게

            // 이미 로그아웃됨 = 토큰 만료(401)로 HandleUnauthorized가 저장된 세션까지 지운 경우
            if (!GameManager.ServerLogin.IsLoggedIn)
            {
                popUp_Alarm.Show(wasAutoLogin ? "로그인 정보가 만료되었습니다. 다시 로그인해주세요." : message);
                return;
            }
            
            // 서버 연결 실패 등: 다시 로그인할 수 있게 토큰은 지우되, 자동 로그인 정보는 남겨둔다
            GameManager.ServerLogin.Logout(clearSavedSession: false);
            popUp_Alarm.Show(wasAutoLogin ? "자동 로그인에 실패했습니다.\n" + message : message);
            return;
        }

        // 인증과 데이터 로드가 모두 성공 → 자동 로그인용 세션 저장 후 입장
        GameManager.ServerLogin.CompleteLogin();
        Debug.Log(wasAutoLogin ? "[Login] 자동 로그인 및 플레이어 데이터 로드 완료" : "[Login] 로그인 및 플레이어 데이터 로드 완료");
        GameManager.Scene.ChangeSceneWithLoading(GameConstants.SceneNames.MAIN_SCENE);   // [로딩]
    }
}