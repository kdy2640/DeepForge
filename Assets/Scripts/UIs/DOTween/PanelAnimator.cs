using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public abstract class PanelAnimator : MonoBehaviour
{
    protected RectTransform Rect { get; private set; }
    protected CanvasGroup CanvasGroup { get; private set; }

    protected virtual void Awake()
    {
        Rect = GetComponent<RectTransform>();
        CanvasGroup = GetComponent<CanvasGroup>();

        CanvasGroup.alpha = 0f;
        CanvasGroup.blocksRaycasts = false;
    }

    public abstract IEnumerator Show();
    public abstract IEnumerator Hide();

    protected virtual void OnDisable()
    {
        DOTween.Kill(this);
        CanvasGroup.alpha = 0f;
        CanvasGroup.blocksRaycasts = false;
    }
}
