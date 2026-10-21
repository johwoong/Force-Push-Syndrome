using System;
using UnityEngine;

public class LocalSaveLoadManager : MonoSingleton<LocalSaveLoadManager>
{
    // 로그인 검증 성공 후 호출 후 JSON에서 읽은 데이터를 DataManager에 전달
    public bool LoadPlayerAfterLogin(string accountLoginId)
    {
        if (GameManager.PlayerData.isPlayerDataLoaded)
        {
            Debug.LogWarning("현재 계정에서 로그아웃한 뒤 로드하세요.");
            return false;
        }

        try
        {
            PlayerSaveData data = SaveLoadHelper.LoadPlayer(accountLoginId);

            if (data == null)
            {
                Debug.LogError("해당 계정의 저장 데이터가 없습니다.");
                return false;
            }

            GameManager.PlayerData.SetPlayerDataFromLocal(accountLoginId, data);
        }
        catch (Exception exception)
        {
            Debug.LogError($"플레이어 데이터 로드 실패: {exception.Message}");
            return false;
        }

        GameManager.PlayerData.NotifyPlayerDataChanged();
        return true;
    }

    // DataManager의 현재 플레이어 데이터를 JSON으로 저장
    public bool SaveCurrentPlayerData()
    {
        return SavePlayerData(GameManager.PlayerData);
    }

    private bool SavePlayerData(PlayerDataManager dataManager)
    {
        if (dataManager == null || !dataManager.isPlayerDataLoaded)
        {
            Debug.LogWarning("저장할 플레이어 데이터가 없습니다.");
            return false;
        }

        PlayerSaveData data = dataManager.currentData;

        if (string.IsNullOrWhiteSpace(dataManager.currentAccountLoginId) || data.profile == null)
        {
            Debug.LogError("저장 대상 계정 정보가 올바르지 않습니다.");
            return false;
        }

        try
        {
            SaveLoadHelper.SavePlayer(dataManager.currentAccountLoginId, data);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"플레이어 데이터 저장 실패: {exception.Message}");
            return false;
        }
    }

    // 저장 성공 후 DataManager의 플레이어 데이터를 해제
    public bool Logout()
    {
        var dataManager = GameManager.PlayerData;

        if (!dataManager.isPlayerDataLoaded)
            return true;

        if (!SavePlayerData(dataManager))
            return false;

        dataManager.ClearPlayerData();
        return true;
    }

    // 앱이 백그라운드로 이동할 때 보조 저장
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
            return;

        SaveIfLoaded();
    }

    // 종료 시 보조 저장
    private void OnApplicationQuit()
    {
        SaveIfLoaded();
    }

    private void SaveIfLoaded()
    {
        // 종료 과정에서 Singleton.Instance 접근으로 매니저가 새로 생성되지 않도록 기존 객체만 조회
        var dataManager = FindFirstObjectByType<PlayerDataManager>();

        if (dataManager != null && dataManager.isPlayerDataLoaded)
        {
            SavePlayerData(dataManager);
        }
    }
}