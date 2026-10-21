using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoSingleton<GameSceneManager>
{
    private int? pendingStageId;
    private long? pendingBattleId;
    private bool isLoading;

    public void ChangeScene(string sceneName)
    {
        pendingStageId = null;
        pendingBattleId = null;
        StopBgmForScene(sceneName);
        SceneManager.LoadScene(sceneName);
    }

    // 전투 씬 진입: 서버가 발급한 battleId를 결과 요청에 쓰도록 함께 넘긴다
    public void ChangeScene(string sceneName, int stageId, long battleId)
    {
        pendingStageId = stageId;
        pendingBattleId = battleId;
        StopBgmForScene(sceneName);
        SceneManager.LoadScene(sceneName);
    }

    // 씬을 떠날 때 현재 BGM을 즉시 끈다. 다음 씬에서 필요하면 PlayBGM으로 다시 튼다 (SceneBgmRouter, WaveManager)
    // 다만 가는 씬이 지금 곡을 그대로 쓰면(로그인 ↔ 메인) 끊지 않고 이어서 들려준다
    private static void StopBgmForScene(string nextSceneName)
    {
        GameManager.Sound.StopAllSFX();

        string next = GameConstants.Bgm.ForScene(nextSceneName);
        if (next != null && GameManager.Sound.CurrentBgmName == next) return;

        GameManager.Sound.StopBGM(0f);
    }

    // ===== 로딩창 씬 이동 =====

    // 로딩창을 띄우고 비동기로 씬을 바꾼다 (로그인 → 메인)
    public void ChangeSceneWithLoading(string sceneName)
    {
        pendingStageId = null;
        pendingBattleId = null;
        StartLoading(sceneName);
    }

    // 로딩창을 띄우고 전투 씬으로 (메인 → 전투)
    public void ChangeSceneWithLoading(string sceneName, int stageId, long battleId)
    {
        pendingStageId = stageId;
        pendingBattleId = battleId;
        StartLoading(sceneName);
    }

    private void StartLoading(string sceneName)
    {
        if (isLoading) return;
        isLoading = true;

        StopBgmForScene(sceneName);

        // UIManager는 항상 DontDestroy라 씬이 바뀌어도 코루틴이 끊기지 않는다
        // (에디터에서 MainScene부터 실행하면 GameSceneManager가 DontDestroy가 아닐 수 있음)
        UIManager.Instance.StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        LoadingScreen loading = UIManager.Instance.Loading;
        loading.Show();       // 로그인처럼 이미 떠 있으면 그대로 둔다
        yield return null;    // 로딩창이 한 번 그려진 뒤 로드 시작

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 로드는 0.9에서 멈춰 활성화를 기다린다. 최소 표시 시간도 함께 채운다
        while (op.progress < 0.9f || loading.ShownSeconds < GameConstants.Value.MIN_LOADING_SECONDS)
            yield return null;

        op.allowSceneActivation = true;
        yield return op;
        yield return null;    // 새 씬의 Start가 한 번 돈 뒤 걷어낸다

        loading.Hide();
        isLoading = false;
    }

    // ===== 씬 간 전달값 =====

    public bool TryConsumePendingStageId(out int stageId)
    {
        if (pendingStageId.HasValue)
        {
            stageId = pendingStageId.Value;
            pendingStageId = null;
            return true;
        }

        stageId = default;
        return false;
    }

    public bool TryConsumePendingBattleId(out long battleId)
    {
        if (pendingBattleId.HasValue)
        {
            battleId = pendingBattleId.Value;
            pendingBattleId = null;
            return true;
        }

        battleId = default;
        return false;
    }
}