using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ClearPanel의 Title·Subtitle(Clear/GameOver)과 Stars(star/star_empty)를 묶어 표시합니다.
/// </summary>
public class ClearPanelController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button exitButton;
    [SerializeField] private Button replayButton;

    [Header("Reveal (DOTween, Time.timeScale 무시)")]
    [SerializeField] private float revealDuration = 0.28f;
    [SerializeField] private float pauseAfterEachStar = 0.06f;

    [SerializeField] private DualSpriteImage title;
    [SerializeField] private DualSpriteImage subtitle;
    [SerializeField] private List<StarSlot> stars = new();

    private Sequence _revealSequence;

    public DualSpriteImage Title => title;
    public DualSpriteImage Subtitle => subtitle;
    public IReadOnlyList<StarSlot> Stars => stars;

    /// <param name="filledStarCount"><see cref="GameRuleManager"/>가 정한 채운 별 개수만 반영합니다.</param>
    public void ApplyOutcome(bool isClear, int filledStarCount)
    {
        title?.Apply(isClear);
        subtitle?.Apply(isClear);
        if (stars == null)
            return;

        var filled = isClear ? Mathf.Clamp(filledStarCount, 0, stars.Count) : 0;

        for (var i = 0; i < stars.Count; i++)
            stars[i]?.SetFilled(i < filled);
    }

    /// <summary>
    /// 스테이지 나가기 / 같은 스테이지 다시 하기 버튼에 동작을 연결합니다.
    /// </summary>
    public void ConfigureNavigation(Action onExitToStageSelect, Action onReplayCurrentStage = null)
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            if (onExitToStageSelect != null)
                exitButton.onClick.AddListener(() => onExitToStageSelect());
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            if (onReplayCurrentStage != null)
                replayButton.onClick.AddListener(() => onReplayCurrentStage());
        }
    }

    /// <summary>
    /// 순서: 별(한 개씩) → Title → Subtitle → 버튼들.
    /// </summary>
    /// <param name="filledStarCount"><see cref="GameRuleManager"/>에서 계산·전달한 값.</param>
    public void PlayRevealSequence(bool isClear, int filledStarCount)
    {
        KillRevealSequence();

        ApplyOutcome(isClear, filledStarCount);

        var panelRt = transform as RectTransform;
        if (panelRt != null)
            panelRt.localScale = Vector3.one;

        SetButtonsInteractable(false);
        HideForReveal();

        _revealSequence = DOTween.Sequence().SetUpdate(true);
        var seq = _revealSequence;

        if (stars != null)
        {
            for (var i = 0; i < stars.Count; i++)
            {
                var img = stars[i]?.Image;
                if (img == null)
                    continue;
                SetGraphicAlpha(img, 0f);
                seq.Append(img.DOFade(1f, revealDuration).SetEase(Ease.OutQuad));
                if (pauseAfterEachStar > 0f && i < stars.Count - 1)
                    seq.AppendInterval(pauseAfterEachStar);
            }
        }

        if (title?.Image != null)
        {
            SetGraphicAlpha(title.Image, 0f);
            seq.Append(title.Image.DOFade(1f, revealDuration).SetEase(Ease.OutQuad));
        }

        if (subtitle?.Image != null)
        {
            SetGraphicAlpha(subtitle.Image, 0f);
            seq.Append(subtitle.Image.DOFade(1f, revealDuration).SetEase(Ease.OutQuad));
        }

        if (exitButton != null)
            AppendFadeInButtonTree(seq, exitButton.transform);

        if (replayButton != null)
            AppendFadeInButtonTree(seq, replayButton.transform);

        seq.OnComplete(() => SetButtonsInteractable(true));
    }

    private void OnDisable()
    {
        KillRevealSequence();
    }

    private void OnDestroy()
    {
        KillRevealSequence();
    }

    private void KillRevealSequence()
    {
        _revealSequence?.Kill();
        _revealSequence = null;
    }

    private void HideForReveal()
    {
        if (title?.Image != null)
            SetGraphicAlpha(title.Image, 0f);
        if (subtitle?.Image != null)
            SetGraphicAlpha(subtitle.Image, 0f);
        if (stars != null)
        {
            foreach (var s in stars)
            {
                if (s?.Image != null)
                    SetGraphicAlpha(s.Image, 0f);
            }
        }

        if (exitButton != null)
            HideGraphicTree(exitButton.transform);
        if (replayButton != null)
            HideGraphicTree(replayButton.transform);
    }

    private static void HideGraphicTree(Transform root)
    {
        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            SetGraphicAlpha(g, 0f);
    }

    private void AppendFadeInButtonTree(Sequence seq, Transform buttonRoot)
    {
        var graphics = buttonRoot.GetComponentsInChildren<Graphic>(true);
        if (graphics.Length == 0)
            return;

        var step = DOTween.Sequence().SetUpdate(true);
        foreach (var g in graphics)
        {
            SetGraphicAlpha(g, 0f);
            step.Join(g.DOFade(1f, revealDuration).SetEase(Ease.OutQuad));
        }

        seq.Append(step);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (exitButton != null)
            exitButton.interactable = value;
        if (replayButton != null)
            replayButton.interactable = value;
    }

    private static void SetGraphicAlpha(Graphic g, float a)
    {
        if (g == null)
            return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    [System.Serializable]
    public class DualSpriteImage
    {
        [SerializeField] private Image image;
        [SerializeField] private Sprite clearSprite;
        [SerializeField] private Sprite gameOverSprite;

        public Image Image => image;
        public Sprite ClearSprite => clearSprite;
        public Sprite GameOverSprite => gameOverSprite;

        public void Apply(bool isClear)
        {
            if (image == null)
                return;
            image.sprite = isClear ? clearSprite : gameOverSprite;
        }
    }

    /// <summary>별 Image와 채운 별(star) / 빈 별(star_empty) 스프라이트.</summary>
    [System.Serializable]
    public class StarSlot
    {
        [SerializeField] private Image image;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Sprite starEmptySprite;

        public Image Image => image;
        public Sprite StarSprite => starSprite;
        public Sprite StarEmptySprite => starEmptySprite;

        public void SetFilled(bool filled)
        {
            if (image == null)
                return;
            image.sprite = filled ? starSprite : starEmptySprite;
        }
    }
}
