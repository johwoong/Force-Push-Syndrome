using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerLoadManager : MonoSingleton<ServerLoadManager>
{
    private bool isRequesting;

    // 로그인 직후 호출: 플레이어 데이터 + 장비 목록을 받아 PlayerDataManager에 넣는다
    // onComplete(성공 여부, 안내 메시지)
    public IEnumerator LoadPlayerData(Action<bool, string> onComplete)
    {
        var login = GameManager.ServerLogin;

        if (!login.IsLoggedIn)
        {
            onComplete?.Invoke(false, "로그인이 필요합니다.");
            yield break;
        }

        if (GameManager.PlayerData.isPlayerDataLoaded)
        {
            onComplete?.Invoke(false, "이미 플레이어 데이터가 로드되어 있습니다.");
            yield break;
        }

        if (isRequesting)
        {
            onComplete?.Invoke(false, "데이터를 불러오는 중입니다. 잠시만 기다려 주세요.");
            yield break;
        }

        isRequesting = true;

        try
        {
            // 1) 플레이어 데이터 (프로필, 재화, 스테이지, 스탯)
            PlayerSaveData data = null;
            string error = null;
            yield return RequestSave((loaded, message) =>
            {
                data = loaded;
                error = message;
            });

            if (data == null)
            {
                onComplete?.Invoke(false, error);
                yield break;
            }

            // 2) 장비 목록
            ApiResult<InventoryResponse> inventoryResult = null;
            yield return InventoryApi.GetInventory(result => inventoryResult = result);

            if (!inventoryResult.IsSuccess)
            {
                onComplete?.Invoke(false, inventoryResult.ToUserMessage("장비 목록을 불러오지 못했습니다."));
                yield break;
            }

            // 서버 장비 목록을 그대로 넣는다 (형식이 같아서 변환 없음)
            data.inventoryList = inventoryResult.Data?.items ?? new List<InventoryData>();

            // 3) PlayerDataManager에 보관
            try
            {
                GameManager.PlayerData.SetPlayerDataFromServer(login.CurrentAccount.accountLoginId, data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerLoad] 데이터 적용 실패: {e.Message}");
                onComplete?.Invoke(false, "플레이어 데이터를 적용하지 못했습니다.");
                yield break;
            }
        }
        finally
        {
            // 성공/실패/중간 종료와 관계없이 다음 요청을 허용한다
            isRequesting = false;
        }

        GameManager.PlayerData.NotifyPlayerDataChanged();
        Debug.Log($"[ServerLoad] 불러오기 완료: playerId={GameManager.PlayerData.currentPlayerId}, " +
                  $"장비 {GameManager.PlayerData.currentData.inventoryList.Count}개");
        onComplete?.Invoke(true, "플레이어 데이터를 불러왔습니다.");
    }

    // GET /api/players/me/save 응답을 받아 검사만 한다. PlayerDataManager에는 넣지 않는다
    // (BattleManager.ReloadSave에서도 사용)
    // onComplete(받은 데이터, 실패 시 안내 메시지)
    public IEnumerator RequestSave(Action<PlayerSaveData, string> onComplete)
    {
        ApiResult<PlayerSaveData> result = null;
        yield return AccountApi.GetSave(r => result = r);

        if (!result.IsSuccess)
        {
            onComplete?.Invoke(null, result.ToUserMessage("플레이어 데이터를 불러오지 못했습니다."));
            yield break;
        }

        var data = result.Data;
        if (data?.profile == null || data.currency == null
            || data.stageProgress == null || data.playerStat == null)
        {
            Debug.LogError("[ServerLoad] 응답에 필요한 데이터가 빠져 있습니다.");
            onComplete?.Invoke(null, "플레이어 데이터가 올바르지 않습니다.");
            yield break;
        }

        onComplete?.Invoke(data, null);
    }
}