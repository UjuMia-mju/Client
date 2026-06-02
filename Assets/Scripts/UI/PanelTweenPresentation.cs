using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// ExitPopup과 동일한 중앙 확대·페이드 연출. <see cref="UIPanelAnimator"/> 및 패널 UI 공통.
/// </summary>
public static class PanelTweenPresentation
{
    public const float DefaultShowDuration = 0.42f;
    public const float DefaultHideDuration = 0.28f;
    public const float DefaultShowScaleFrom = 0.68f;
    public const float DefaultDimFadeDuration = 0.32f;
    public const float DefaultBodyFadeDuration = 0.36f;

    sealed class Targets
    {
        public CanvasGroup Dim;
        public CanvasGroup BodyGroup;
        public Transform Body;
        public Vector3 BodyTargetScale;
    }

    public static IEnumerator Show(
        GameObject panel,
        Vector3 targetScale,
        float showDuration = DefaultShowDuration,
        float dimFadeDuration = DefaultDimFadeDuration,
        float bodyFadeDuration = DefaultBodyFadeDuration,
        float showScaleFrom = DefaultShowScaleFrom)
    {
        if (panel == null)
            yield break;

        panel.SetActive(true);
        Kill(panel);

        var targets = ResolveTargets(panel, targetScale);
        PrepareShowState(targets, showScaleFrom);

        var sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetId(panel);

        if (targets.Dim != null)
            sequence.Join(targets.Dim.DOFade(1f, dimFadeDuration).SetEase(Ease.OutQuad));

        if (targets.BodyGroup != null && targets.BodyGroup != targets.Dim)
            sequence.Join(targets.BodyGroup.DOFade(1f, bodyFadeDuration).SetEase(Ease.OutQuad));

        if (targets.Body != null)
            sequence.Join(targets.Body.DOScale(targets.BodyTargetScale, showDuration).SetEase(Ease.OutQuart));

        yield return sequence.WaitForCompletion();
        ApplyShownState(targets);
    }

    public static IEnumerator Hide(
        GameObject panel,
        bool destroyOnEnd = true,
        float hideDuration = DefaultHideDuration,
        float showScaleFrom = DefaultShowScaleFrom)
    {
        if (panel == null)
            yield break;

        Kill(panel);

        var targets = ResolveTargets(panel, panel.transform.localScale);
        var sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetId(panel);

        if (targets.Dim != null)
            sequence.Join(targets.Dim.DOFade(0f, hideDuration).SetEase(Ease.InQuad));

        if (targets.BodyGroup != null && targets.BodyGroup != targets.Dim)
            sequence.Join(targets.BodyGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));

        if (targets.Body != null)
        {
            Vector3 hiddenScale = targets.BodyTargetScale * showScaleFrom;
            sequence.Join(targets.Body.DOScale(hiddenScale, hideDuration).SetEase(Ease.InQuart));
        }

        yield return sequence.WaitForCompletion();

        if (panel == null)
            yield break;

        if (destroyOnEnd)
            Object.Destroy(panel);
        else
            panel.SetActive(false);
    }

    public static void Kill(GameObject panel)
    {
        if (panel == null)
            return;

        DOTween.Kill(panel);
    }

    static Targets ResolveTargets(GameObject panel, Vector3 targetScale)
    {
        var result = new Targets();

        Transform panelDim = panel.transform.Find("Panel");
        Transform messageBox = panel.transform.Find("Panel/MessageBox");

        if (messageBox != null)
        {
            result.Dim = GetOrAddCanvasGroup(panelDim);
            result.Body = messageBox;
            result.BodyGroup = GetOrAddCanvasGroup(messageBox);
            result.BodyTargetScale = Vector3.one;
            return result;
        }

        if (panelDim != null)
        {
            result.Dim = GetOrAddCanvasGroup(panelDim);
            result.Body = FindScalableContent(panelDim);
            if (result.Body != null)
            {
                result.BodyGroup = GetOrAddCanvasGroup(result.Body);
                result.BodyTargetScale = Vector3.one;
            }

            return result;
        }

        result.Body = panel.transform;
        result.BodyGroup = GetOrAddCanvasGroup(panel.transform);
        result.BodyTargetScale = targetScale;
        return result;
    }

    static void PrepareShowState(Targets targets, float showScaleFrom)
    {
        if (targets.Dim != null)
            targets.Dim.alpha = 0f;

        if (targets.BodyGroup != null && targets.BodyGroup != targets.Dim)
            targets.BodyGroup.alpha = 0f;

        if (targets.Body != null)
            targets.Body.localScale = targets.BodyTargetScale * showScaleFrom;
    }

    static void ApplyShownState(Targets targets)
    {
        if (targets.Dim != null)
            targets.Dim.alpha = 1f;

        if (targets.BodyGroup != null)
            targets.BodyGroup.alpha = 1f;

        if (targets.Body != null)
            targets.Body.localScale = targets.BodyTargetScale;
    }

    static Transform FindScalableContent(Transform panelDim)
    {
        if (panelDim == null)
            return null;

        for (int i = 0; i < panelDim.childCount; i++)
        {
            Transform child = panelDim.GetChild(i);
            switch (child.name)
            {
                case "MessageBox":
                case "Content":
                case "Buttons":
                case "Menu":
                    return child;
            }
        }

        return null;
    }

    static CanvasGroup GetOrAddCanvasGroup(Transform target)
    {
        if (target == null)
            return null;

        return target.GetComponent<CanvasGroup>() ?? target.gameObject.AddComponent<CanvasGroup>();
    }
}
