using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class LocalLoginManager : MonoSingleton<LocalLoginManager>
{
    public bool isLoggedIn => GameManager.PlayerData.isPlayerDataLoaded;
    public string currentAccountLoginId => GameManager.PlayerData.currentAccountLoginId;

    public bool SignUp(string accountLoginId, string password, out string message)
    {
        if (isLoggedIn)
        {
            message = "로그아웃 후 회원가입 하세요.";
            return false;
        }

        accountLoginId = NormalizeLoginId(accountLoginId);

        if (!Regex.IsMatch(accountLoginId, @"^[a-z0-9_]{3,20}$"))
        {
            message = "아이디는 영문, 숫자, 밑줄로 3 ~ 20자 입력해주세요.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            message = "비밀번호를 입력해주세요.";
            return false;
        }

        try
        {
            AccountSaveData accountData = SaveLoadHelper.LoadAccounts();

            if (accountData.accounts == null)
            {
                message = "계정 목록 데이터가 올바르지 않습니다.";
                return false;
            }

            // LocalAccountData.playerId = 로그인 아이디 (로컬 전용 필드명 유지)
            bool alreadyExists = accountData.accounts.Exists(account =>
                account != null && string.Equals(account.playerId, accountLoginId, StringComparison.OrdinalIgnoreCase)
            );

            if (alreadyExists)
            {
                message = "이미 사용 중인 아이디입니다.";
                return false;
            }

            // 초기 플레이어 파일을 먼저 저장합니다.
            // 기존 파일이 있으면 덮어쓰지 않고 가입을 중단합니다.
            if (SaveLoadHelper.PlayerFileExists(accountLoginId))
            {
                message = "해당 아이디의 게임 데이터가 이미 있습니다. " + "저장 상태를 확인해 주세요.";
                return false;
            }

            // 서버의 Auto Increment를 흉내 내 계정 순번으로 ID 부여 (서버 연동 시 서버가 부여)
            int newId = accountData.accounts.Count + 1;

            // 닉네임 입력 UI가 생기기 전까지 로그인 아이디를 닉네임으로 사용
            var playerData = PlayerSaveData.CreateDefault(newId, newId, accountLoginId);
            ApplyInitialValues(playerData);

            SaveLoadHelper.SavePlayer(accountLoginId, playerData);
            accountData.accounts.Add(new LocalAccountData
            {
                playerId = accountLoginId,
                password = password
            });

            SaveLoadHelper.SaveAccounts(accountData);
            message = "회원가입이 완료되었습니다.";
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"회원가입 처리 실패: {e.Message}");
            message = "회원가입 저장에 실패했습니다. " + "저장 상태를 확인해주세요.";

            return false;
        }
    }

    // 초기 재화를 AccountConst.json 값으로 설정 (CreateDefault는 데이터 정의서 기본값 0을 사용)
    private static void ApplyInitialValues(PlayerSaveData data)
    {
        var accountConst = GameManager.JsonData.AccountConstData;
        if (accountConst == null)
        {
            Debug.LogWarning("[LocalLoginManager] AccountConst 데이터가 없어 초기 재화를 0으로 둡니다.");
            return;
        }

        data.currency.currencyGold = accountConst.initialGold;
        data.currency.currencyGem = accountConst.initialGem;
        data.currency.currencyEnergy = accountConst.initialStamina;   // 스태미나 = currencyEnergy
        data.currency.currencyEnergyUpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool Login(string accountLoginId, string password, out string message)
    {
        if (isLoggedIn)
        {
            message = "이미 로그인되어 있습니다.";
            return false;
        }

        accountLoginId = NormalizeLoginId(accountLoginId);

        if (string.IsNullOrEmpty(accountLoginId) || string.IsNullOrEmpty(password))
        {
            message = "아이디와 비밀번호를 입력해 주세요.";
            return false;
        }

        try
        {
            AccountSaveData accountData = SaveLoadHelper.LoadAccounts();

            if (accountData.accounts == null)
            {
                message = "계정 목록 데이터가 올바르지 않습니다.";
                return false;
            }

            LocalAccountData account = accountData.accounts.Find(item =>
                item != null && string.Equals(item.playerId, accountLoginId, StringComparison.OrdinalIgnoreCase)
            );

            if (account == null || account.password != password)
            {
                message = "아이디 또는 비밀번호가 일치하지 않습니다.";
                return false;
            }

            message = "로그인되었습니다.";
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"로그인 처리 실패: {e.Message}");
            message = "로그인 처리 중 오류가 발생했습니다.";
            return false;
        }
    }

    [Obsolete("서버 연동으로 사용하지 않음. 발표 자료용으로 코드만 보존")]
    public bool Logout(out string message)
    {
        if (!GameManager.LocalSaveLoad.Logout())
        {
            message = "저장에 실패하여 로그아웃하지 못했습니다.";
            return false;
        }

        message = "로그아웃되었습니다.";
        return true;
    }

    private static string NormalizeLoginId(string accountLoginId)
    {
        return (accountLoginId ?? string.Empty).Trim().ToLowerInvariant();
    }
}