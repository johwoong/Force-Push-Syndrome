using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬이 열릴 때 그 씬의 BGM을 자동으로 튼다. 씬에 따로 오브젝트를 둘 필요가 없다.
///   로그인·메인 씬 → Main
///   전투 씬        → 스테이지가 정해지는 WaveManager.SetupStage에서 Stage{stageId}를 튼다
/// 같은 곡이 이미 재생 중이면 SoundManager.PlayBGM이 무시하므로 로그인 → 메인에서 곡이 끊기지 않는다.
/// 곡 이름은 GameConstants.Bgm에서 정한다.
/// </summary>
public static class SceneBgmRouter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;   // 도메인 리로드를 끈 에디터에서 중복 구독 방지
        SceneManager.sceneLoaded += HandleSceneLoaded;

        PlayFor(SceneManager.GetActiveScene().name);     // 첫 씬은 이미 열려 있다
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            PlayFor(scene.name);
    }

    // 곡이 정해진 씬만 튼다. 그 밖의 씬(테스트 씬 등)은 재생 상태를 건드리지 않는다
    private static void PlayFor(string sceneName)
    {
        string bgm = GameConstants.Bgm.ForScene(sceneName);
        if (bgm != null)
            GameManager.Sound.PlayBGM(bgm);
    }
}
