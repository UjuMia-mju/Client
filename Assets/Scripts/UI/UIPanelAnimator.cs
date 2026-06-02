using UnityEngine;
using System.Collections;

/// <summary>
/// Fade 루트(SceneLoader에서 생성)의 패널만 싱글톤입니다. 다른 씬·매니저는 <see cref="Instance"/> 참조만 쓰고,
/// 새 <see cref="GameObject"/>에 붙여 싱글톤을 만들지 마세요.
/// </summary>
public class UIPanelAnimator : MonoBehaviour
{
    public static UIPanelAnimator Instance { get; private set; }

    [SerializeField] private float showDuration = PanelTweenPresentation.DefaultShowDuration;
    [SerializeField] private float hideDuration = PanelTweenPresentation.DefaultHideDuration;
    [SerializeField] private float showScaleFrom = PanelTweenPresentation.DefaultShowScaleFrom;
    [SerializeField] private float dimFadeDuration = PanelTweenPresentation.DefaultDimFadeDuration;
    [SerializeField] private float bodyFadeDuration = PanelTweenPresentation.DefaultBodyFadeDuration;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Fade 프리팹을 올리기 직전, 예전 패턴으로 생긴 UIP-only 임시 호스트가 싱글톤을 잡고 있으면 풉니다.</summary>
    internal static void ClearStandaloneSingletonHostBeforeFadeInstall()
    {
        UIPanelAnimator anim = Instance;
        if (anim == null)
            return;

        GameObject host = anim.gameObject;
        if (!IsStandaloneAnimatorOnlyHost(host))
            return;

        Instance = null;
        Destroy(host);
    }

    static bool IsStandaloneAnimatorOnlyHost(GameObject host)
    {
        if (host == null)
            return false;
        MonoBehaviour[] mbs = host.GetComponents<MonoBehaviour>();
        return mbs.Length == 1 && mbs[0] is UIPanelAnimator;
    }

    public IEnumerator FadeIn(GameObject panel, Vector3 targetScale)
    {
        yield return PanelTweenPresentation.Show(
            panel,
            targetScale,
            showDuration,
            dimFadeDuration,
            bodyFadeDuration,
            showScaleFrom);
    }

    public IEnumerator FadeOut(GameObject panel, bool destroyOnEnd = true)
    {
        yield return PanelTweenPresentation.Hide(panel, destroyOnEnd, hideDuration, showScaleFrom);
    }
}
