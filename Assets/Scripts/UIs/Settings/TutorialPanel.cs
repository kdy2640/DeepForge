using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialPanel : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text contentText;
    [SerializeField] private Button confirmButton;

    public void Init(CanvasController owner)
    {
        confirmButton.onClick.AddListener(owner.CloseTutorial);
        gameObject.SetActive(false);
    }

    public void Open(TutorialDataSO data)
    {
        titleText.text = data.Title;
        contentText.text = data.Content;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
