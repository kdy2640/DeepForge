using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UI_SceneChangeButton : MonoBehaviour
{
    [SerializeField] private SceneType nextSceneType;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClicked);
    }

    public void OnButtonClicked()
    {
        GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Global_ButtonClick);
        GameManager.Instance.SceneController.ChangeScene(nextSceneType);
    }
}
