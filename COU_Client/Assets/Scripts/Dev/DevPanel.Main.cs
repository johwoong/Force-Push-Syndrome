#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;

// 메인 섹션: 서버 개발용 API로 스태미나 회복, 장비 지급
public partial class DevPanel
{
    // 서버 개발용 API가 꺼져 있으면 경로가 없어 404 NOT_FOUND가 온다 (없는 itemId는 ITEM_NOT_FOUND)
    private const string ErrorNotFound = "NOT_FOUND";

    private bool requesting;
    private string grantItemId = "1001";
    private string grantCount = "2";

    // PlayerInventory는 테스트 씬에도 있어, 로그인 없이 실행한 경우(플레이어 데이터 없음)는 그리지 않는다
    private static bool CanDrawMain => PlayerInventory.Instance != null && GameManager.PlayerData.isPlayerDataLoaded;

    private void DrawMain()
    {
        GUILayout.Label("[메인]");

        GUI.enabled = !requesting;

        if (GUILayout.Button("스태미나 회복"))
            StartCoroutine(RefillStamina());

        GUILayout.BeginHorizontal();
        GUILayout.Label("itemId");
        grantItemId = GUILayout.TextField(grantItemId);
        GUILayout.Label("개수");
        grantCount = GUILayout.TextField(grantCount);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("장비 지급"))
        {
            if (long.TryParse(grantItemId, out var itemId) && int.TryParse(grantCount, out var count))
                StartCoroutine(GrantItems(itemId, count));
            else
                message = "itemId와 개수를 숫자로 입력하세요.";
        }

        GUI.enabled = true;
    }

    private IEnumerator RefillStamina()
    {
        requesting = true;
        yield return DevApi.RefillStamina(result =>
        {
            if (!result.IsSuccess)
            {
                message = ToFailMessage(result, "스태미나를 회복하지 못했습니다.");
                return;
            }

            // 서버 값으로 덮어쓰고 알린다 (MainUIAccountBinder가 TopBar를 갱신)
            GameManager.PlayerData.currentData.currency = result.Data;
            GameManager.PlayerData.NotifyPlayerDataChanged();
            message = $"스태미나 {result.Data.currencyEnergy}/{GameManager.JsonData.AccountConstData.maxStamina}";
        });
        requesting = false;
    }

    private IEnumerator GrantItems(long itemId, int count)
    {
        requesting = true;
        yield return DevApi.GrantItems(itemId, count, result =>
        {
            if (!result.IsSuccess)
            {
                message = ToFailMessage(result, "장비를 지급하지 못했습니다.");
                return;
            }

            var added = PlayerInventory.Instance.AddServerItems(result.Data);
            message = $"장비 {added.Count}개 지급";
        });
        requesting = false;
    }

    // 401은 ServerLoginManager가 처리하므로 메시지만 남긴다
    private static string ToFailMessage<T>(ApiResult<T> result, string fallback)
        => result.Error?.code == ErrorNotFound
            ? "서버 개발용 API가 꺼져 있습니다(DEV_TOOLS_ENABLED)"
            : result.ToUserMessage(fallback);
}
#endif
