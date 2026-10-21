using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SaveLoadHelper
{
    private static string AccountPath =>
        Path.Combine(Application.persistentDataPath, GameConstants.Paths.ACCOUNT_SAVE_PATH);

    private static string PlayerDirectory =>
        Path.Combine(Application.persistentDataPath, GameConstants.Paths.PLAYER_DIRECTORY);

    private static string StaticDataDirectory =>
        Path.Combine(Application.persistentDataPath, GameConstants.Paths.STATIC_DATA_DIRECTORY);

    private static string StaticDataVersionPath =>
        Path.Combine(StaticDataDirectory, GameConstants.Paths.STATIC_DATA_VERSION_FILE);

    // 계정 목록 저장
    public static void SaveAccounts(AccountSaveData data)
    {
        SaveJson(AccountPath, data);
    }

    public static AccountSaveData LoadAccounts()
    {
        // 첫 실행에는 계정 파일이 없으므로 빈 목록을 반환
        return LoadJson<AccountSaveData>(AccountPath) ?? new AccountSaveData();
    }

    // 플레이어 데이터 (파일 이름 = 로그인 아이디)
    public static void SavePlayer(string accountLoginId, PlayerSaveData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        SaveJson(GetPlayerPath(accountLoginId), data);
    }

    public static bool PlayerFileExists(string accountLoginId)
    {
        return File.Exists(GetPlayerPath(accountLoginId));
    }

    public static PlayerSaveData LoadPlayer(string accountLoginId)
    {
        string path = GetPlayerPath(accountLoginId);
        PlayerSaveData data = LoadJson<PlayerSaveData>(path);

        if (data != null)
            Validate(data, path);

        return data;
    }

    // 정적 데이터 저장본 (테이블마다 <이름>.json, 버전은 별도 파일). 없으면 null
    public static string LoadStaticDataVersion()
    {
        return File.Exists(StaticDataVersionPath) ? File.ReadAllText(StaticDataVersionPath) : null;
    }

    public static string LoadStaticDataTable(string tableName)
    {
        string path = Path.Combine(StaticDataDirectory, tableName + ".json");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    // 버전 파일은 테이블을 모두 쓴 뒤 마지막에 쓴다. 중간에 실패하면 이전 버전이 남아 다음 요청에서 다시 받는다
    public static void SaveStaticData(string version, JObject tables)
    {
        Directory.CreateDirectory(StaticDataDirectory);

        foreach (JProperty table in tables.Properties())
        {
            if (table.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"파일명으로 사용할 수 없는 테이블 이름입니다: {table.Name}");

            File.WriteAllText(Path.Combine(StaticDataDirectory, table.Name + ".json"), table.Value.ToString());
        }

        File.WriteAllText(StaticDataVersionPath, version);
    }

    // 이전 형식(gold, equipmentList 등)의 세이브 파일은 profile이 비어 있어 여기서 걸러진다
    private static void Validate(PlayerSaveData data, string path)
    {
        if (data.profile == null || data.profile.playerId <= 0 || data.profile.accountId <= 0
            || data.currency == null || data.stageProgress == null)
        {
            throw new InvalidDataException(
                $"이전 형식이거나 손상된 세이브 파일입니다. 파일을 삭제하고 다시 가입하세요: {path}");
        }

        data.inventoryList ??= new System.Collections.Generic.List<InventoryData>();
        data.playerStat ??= new PlayerStatData { playerId = data.profile.playerId };
    }

    // 공통 파일 처리
    private static void SaveJson<T>(string path, T data) where T : class
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        Directory.CreateDirectory(Path.GetDirectoryName(path));

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);

        File.WriteAllText(path, json);
    }

    private static T LoadJson<T>(string path) where T : class
    {
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        T data = JsonConvert.DeserializeObject<T>(json);

        if (data == null)
        {
            throw new InvalidDataException($"저장 파일에 데이터가 없습니다: {path}");
        }

        return data;
    }

    private static string GetPlayerPath(string accountLoginId)
    {
        if (string.IsNullOrWhiteSpace(accountLoginId)
            || accountLoginId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || accountLoginId.Contains("/")
            || accountLoginId.Contains("\\")
            || accountLoginId == "."
            || accountLoginId == "..")
        {
            throw new ArgumentException("파일명으로 사용할 수 없는 로그인 아이디입니다.", nameof(accountLoginId));
        }

        return Path.Combine(PlayerDirectory, accountLoginId + ".json");
    }
}