using System.Collections;
using DG.Tweening;
using UnityEngine;

public class GameLoopScene : SceneBase
{
    public override SceneType SceneType => SceneType.GameLoop;
    public override string SceneName => "GameLoopScene";
    private PlayerController player;

    public override IEnumerator PrepareBeforeReveal()
    {
        DOTween.Init();
        DOTween.SetTweensCapacity(500, 50);

        yield return null;
        player = Object.FindFirstObjectByType<PlayerController>();
        player.SetGameplayActive(false);
        TerrainManager terrain = Object.FindFirstObjectByType<TerrainManager>();
        while (!terrain.IsInitialLoadComplete)
            yield return null;

        PrewarmDOTween();
    }

    private void PrewarmDOTween()
    {
        const int count = 16;
        Sequence[] sequences = new Sequence[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 position = Vector2.zero;
            Vector3 scale = Vector3.one;
            float alpha = 0f;

            sequences[i] = DOTween.Sequence()
                .SetAutoKill(false)
                .SetRecyclable(true)
                .Pause()
                .Join(DOTween.To(() => position, value => position = value, Vector2.one, 0.01f)
                    .SetRecyclable(true))
                .Join(DOTween.To(() => scale, value => scale = value, Vector3.zero, 0.01f)
                    .SetRecyclable(true))
                .Join(DOTween.To(() => alpha, value => alpha = value, 1f, 0.01f)
                    .SetRecyclable(true));

            sequences[i].ForceInit();
            sequences[i].Complete(false);
        }

        // 모두 동시에 보유한 뒤 반환해야 같은 트윈 하나만 반복해서 재사용하지 않는다.
        foreach (Sequence sequence in sequences)
            sequence.Kill();
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
