using System.Collections;
using UnityEngine;

public class GameLoopScene : SceneBase
{
    public override SceneType SceneType => SceneType.GameLoop;
    public override string SceneName => "GameLoopScene";
    private PlayerController player;

    public override IEnumerator PrepareBeforeReveal()
    {
        yield return null;
        player = Object.FindFirstObjectByType<PlayerController>();
        player.SetGameplayActive(false);
        TerrainManager terrain = Object.FindFirstObjectByType<TerrainManager>();
        while (!terrain.IsInitialLoadComplete)
            yield return null;
    }

    public override IEnumerator Enter()
    {
        player.SetGameplayActive(true);
        yield break;
    }

    public override IEnumerator Exit()
    {
        player.SetGameplayActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield break;
    }

}
