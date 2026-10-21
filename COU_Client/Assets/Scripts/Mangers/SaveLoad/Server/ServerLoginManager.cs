using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class ServerLoginManager : MonoSingleton<ServerLoginManager>
{
    // 토큰은 ApiClient가 보관한다
    // 로그인 여부는 계정 정보로 판단한다 (에디터에서는 ApiClient에 개발용 토큰이 미리 있을 수 있음)
    public ServerAccountData CurrentAccount { get; private set; }
    public bool IsLoggedIn => CurrentAccount != null;
    
    // [자동 로그인] 저장된 세션으로 로그인해 데이터를 불러오는 중 (게임 입장 전까지)
    public bool IsRestoringSession { get; private set; }
    
    // 버튼을 연타해도 요청이 한 번만 나가도록 막는다
    private bool isRequesting;

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            ApiClient.OnUnauthorized += HandleUnauthorized;
    }

    protected override void OnDestroy()
    {
        ApiClient.OnUnauthorized -= HandleUnauthorized;
        base.OnDestroy();
    }

    // 회원가입: onComplete(성공 여부, 안내 메시지)
    public IEnumerator SignUp(string accountLoginId, string password, Action<bool, string> onComplete)
    {
        if (IsLoggedIn)
        {
            onComplete?.Invoke(false, "로그아웃 후 회원가입하세요.");
            yield break;
        }

        accountLoginId = NormalizeLoginId(accountLoginId);
        
        // 서버와 같은 규칙으로 먼저 검사해서 불필요한 요청을 줄인다
        if (!Regex.IsMatch(accountLoginId, @"^[a-z0-9_]{3,20}$"))
        {
            onComplete?.Invoke(false, "아이디는 영문, 숫자, 밑줄로 3 ~ 20자 입력해주세요.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            onComplete?.Invoke(false, "비밀번호를 입력해주세요.");
            yield break;
        }

        if (isRequesting)
        {
            onComplete?.Invoke(false, "요청을 처리하고 있습니다. 잠시만 기다려 주세요.");
            yield break;
        }

        isRequesting = true;

        var body = new SignupRequest
        {
            accountLoginId = accountLoginId,
            password = password
        };

        yield return AccountApi.Signup(body, result =>
        {
            isRequesting = false;

            if (result.IsSuccess)
                onComplete?.Invoke(true, "회원가입이 완료되었습니다.");
            else
                onComplete?.Invoke(false, result.ToUserMessage("회원가입에 실패했습니다."));
        });
    }
    
    // 로그인: 성공하면 토큰을 ApiClient에 넣고 계정 정보를 보관한다.
    public IEnumerator Login(string accountLoginId, string password, Action<bool, string> onComplete)
    {
        if (IsLoggedIn)
        {
            onComplete?.Invoke(false, "이미 로그인되어 있습니다.");
            yield break;
        }

        accountLoginId = NormalizeLoginId(accountLoginId);

        if (string.IsNullOrEmpty(accountLoginId) || string.IsNullOrEmpty(password))
        {
            onComplete?.Invoke(false, "아이디와 비밀번호를 입력해주세요.");
            yield break;
        }

        if (isRequesting)
        {
            onComplete?.Invoke(false, "요청을 처리하고 있습니다. 잠시만 기다려 주세요.");
            yield break;
        }

        isRequesting = true;

        var body = new LoginRequest
        {
            accountLoginId = accountLoginId,
            password = password
        };

        yield return AccountApi.Login(body, result =>
        {
            isRequesting = false;

            if (!result.IsSuccess)
            {
                // 로그인의 401은 만료가 아니라 아이디/비밀번호 불일치 (아이디 존재 여부는 구분하지 않음)
                string message = result.FailType == ApiFailType.Unauthorized
                    ? "아이디 또는 비밀번호가 일치하지 않습니다."
                    : result.ToUserMessage("로그인에 실패했습니다.");

                onComplete?.Invoke(false, message);
                return;
            }

            if (string.IsNullOrEmpty(result.Data?.accessToken))
            {
                onComplete?.Invoke(false, "로그인 응답에 토큰이 없습니다.");
                return;
            }

            ApiClient.SetAccessToken(result.Data.accessToken);
            CurrentAccount = result.Data.account;
            GameManager.ServerPresence.StartHeartbeat();   // 친구에게 온라인으로 보이도록 접속을 알린다
            onComplete?.Invoke(true, "로그인되었습니다.");
        });
    }
    
    // [자동 로그인] 저장된 세션이 있으면 그 계정 아이디를 알려준다 (팝업 표시용, 로그인 상태는 바꾸지 않음)
    public bool TryGetSavedLoginId(out string accountLoginId)
    {
        accountLoginId = null;
        if (IsLoggedIn) return false;
        if (!AuthSessionStore.TryLoad(out _, out ServerAccountData account)) return false;

        accountLoginId = account.accountLoginId;
        return true;
    }
    
    // [자동 로그인] 저장된 토큰과 계정 정보로 로그인 상태를 되살린다.
    // 토큰이 아직 유효한지는 이어서 부르는 LoadPlayerData가 확인한다 (만료된 401 -> HandleUnauthorized)
    public bool TryRestoreSession()
    {
        if (IsLoggedIn) return false;
        if (!AuthSessionStore.TryLoad(out string token, out ServerAccountData account)) return false;
        
        ApiClient.SetAccessToken(token);
        CurrentAccount = account;
        IsRestoringSession = true;
        
        return true;
    }
    
    // [자동 로그인] 로그인(직접/자동)과 데이터 로드가 모두 끝나 게임에 들어갈 때 호출
    // 여기서 저장하므로 "게임에 들어가 본 적이 있는" 계정만 자동 로그인 대상
    public void CompleteLogin()
    {
        if (!IsLoggedIn) return;

        IsRestoringSession = false;
        AuthSessionStore.Save(ApiClient.AccessToken, CurrentAccount);
        GameManager.ServerPresence.StartHeartbeat(); // 자동 로그인은 Login()을 거치지 않으므로 여기서 시작 (이미 돌고 있으면 무시)
    }
    
    // 로그아웃: 서버에 따로 알릴 필요 없이 토큰과 플레이어 데이터를 지운다
    // [자동 로그인] clearSavedSession = false : 서버 연결 실패처럼 일시적인 문제일 때 자동 로그인 정보는 남겨 둔다
    public void Logout(bool clearSavedSession = true)
    {
        if (clearSavedSession) AuthSessionStore.Clear();
        
        GameManager.ServerPresence.StopHeartbeat();
        GameManager.ServerChat.StopConversation();
        FriendRequestState.Set(0);
        CurrentAccount = null;
        ApiClient.ClearAccessToken();
        GameManager.PlayerData.ClearPlayerData();
    }
    
    // 토큰 만료·무효: 로그아웃하고 로그인 화면으로 보낸다
    private void HandleUnauthorized()
    {
        // 로그인 안 된 상태거나, 여러 요청이 동시에 401을 받아 이미 처리한 경우
        if (!IsLoggedIn) return;

        Debug.LogWarning("[ServerLogin] 토큰이 만료되어 로그아웃합니다.");
        Logout();   // 토큰, 계정 정보, PlayerDataManager 데이터 삭제 → IsLoggedIn = false

        UIManager.Instance.CloseAll();
        GameManager.Scene.ChangeScene(GameConstants.SceneNames.LOGIN_SCENE);
        UIManager.Instance.Get<MessagePopup>().ShowAlert("로그인이 만료되었습니다. 다시 로그인해 주세요.");
    }

    private static string NormalizeLoginId(string accountLoginId)
        => (accountLoginId ?? string.Empty).Trim().ToLowerInvariant();
}