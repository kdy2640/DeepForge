using System.Collections;
using DG.Tweening;
using UnityEngine;

public sealed class EdgePanelAnimator : PanelAnimator
{
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float offsetX = -420f;

    public override IEnumerator Show()
    {
        DOTween.Kill(this);

        Vector2 endPosition = Rect.anchoredPosition;
        Vector3 endEuler = Rect.localEulerAngles;
        Vector3 endScale = Rect.localScale;

        Rect.anchoredPosition = endPosition + new Vector2(offsetX, 0f);
        Rect.localEulerAngles = endEuler;
        Rect.localScale = endScale * 0.95f;
        CanvasGroup.alpha = 0f;
        CanvasGroup.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetAutoKill(false)
            .Join(Rect.DOAnchorPos(endPosition, duration).SetEase(Ease.OutExpo))
            .Join(Rect.DOScale(endScale, duration).SetEase(Ease.OutCubic))
            .Join(CanvasGroup.DOFade(1f, duration * 0.75f).SetEase(Ease.OutExpo));

        sequence.OnComplete(() => CanvasGroup.blocksRaycasts = true);
        sequence.OnKill(Restore);

        void Restore()
        {
            Rect.anchoredPosition = endPosition;
            Rect.localEulerAngles = endEuler;
            Rect.localScale = endScale;
            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = true;
        }

        return sequence.WaitForCompletion(true);
    }

    public override IEnumerator Hide()
    {
        DOTween.Kill(this);

        Vector2 startPosition = Rect.anchoredPosition;
        Vector3 startEuler = Rect.localEulerAngles;
        Vector3 startScaleValue = Rect.localScale;
        float startAlphaValue = CanvasGroup.alpha;
        bool startBlocksRaycasts = CanvasGroup.blocksRaycasts;

        CanvasGroup.blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetAutoKill(false)
            .Join(Rect.DOAnchorPos(
                startPosition + new Vector2(offsetX, 0f),
                duration * 0.3f).SetEase(Ease.InCubic))
            .Join(CanvasGroup.DOFade(0f, duration * 0.3f).SetEase(Ease.InQuad));

        sequence.OnKill(Restore);

        void Restore()
        {
            Rect.anchoredPosition = startPosition;
            Rect.localEulerAngles = startEuler;
            Rect.localScale = startScaleValue;
            CanvasGroup.alpha = startAlphaValue;
            CanvasGroup.blocksRaycasts = startBlocksRaycasts;
        }

        return sequence.WaitForCompletion(true);
    }
}
