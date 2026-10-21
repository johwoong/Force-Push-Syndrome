using System;
using UnityEngine;

public class PlayerDataManager : MonoSingleton<PlayerDataManager>
{
    public PlayerSaveData currentData { get; private set; }

    public bool isPlayerDataLoaded => currentData != null;

    // [로컬 전용] 세이브 파일 이름으로 쓰는 로그인 아이디 (Players/{accountLoginId}.json)
    public string currentAccountLoginId { get; private set; }

    // 데이터 정의서 기준 플레이어 ID (PlayerProfile.playerId)
    public long currentPlayerId => currentData?.profile?.playerId ?? 0;

    public event Action<PlayerSaveData> OnPlayerDataChanged;

    // 로컬 JSON에서 읽은 플레이어 데이터를 보관
    public void SetPlayerDataFromLocal(string accountLoginId, PlayerSaveData data)
    {
        if (string.IsNullOrWhiteSpace(accountLoginId))
            throw new ArgumentException("로그인 아이디가 없습니다.", nameof(accountLoginId));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.profile == null || data.profile.playerId <= 0)
            throw new ArgumentException("플레이어 ID가 없습니다.");

        if (isPlayerDataLoaded)
            throw new InvalidOperationException("기존 계정 데이터를 해제한 뒤 적용하세요.");

        currentAccountLoginId = accountLoginId;
        currentData = data;
    }

    public void SetPlayerDataFromServer(string accountLoginId, PlayerSaveData data)
    {
        if (string.IsNullOrWhiteSpace(accountLoginId))
            throw new ArgumentException("로그인 아이디가 없습니다.", nameof(accountLoginId));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.profile == null || data.profile.playerId <= 0)
            throw new ArgumentException("플레이어 ID가 없습니다.");

        if (isPlayerDataLoaded)
            throw new InvalidOperationException("기존 계정 데이터를 해제한 뒤 적용하세요.");

        currentAccountLoginId = accountLoginId;
        currentData = data;
    }

    // 데이터 로드 또는 변경 후 Action을 Invoke하는 메서드
    public void NotifyPlayerDataChanged()
    {
        if (isPlayerDataLoaded) OnPlayerDataChanged?.Invoke(currentData);
    }

    // 로그아웃 시 플레이어 데이터 해제
    public void ClearPlayerData()
    {
        currentData = null;
        currentAccountLoginId = null;
    }
}