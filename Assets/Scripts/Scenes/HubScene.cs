using System.Collections;
using UnityEngine;

public class HubScene : SceneBase
{
    public override SceneType SceneType => SceneType.Hub;
    public override string SceneName => "HubScene";

    public override IEnumerator Enter()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        yield break;
    }

}
