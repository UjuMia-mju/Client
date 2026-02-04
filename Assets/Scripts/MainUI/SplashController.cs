using UnityEngine;
using System.Collections;

/// <summary>
/// 스플래시 로고의 페이드 인/아웃 연출을 담당하는 클래스
/// </summary>
public class SplashController : MonoBehaviour
{
    public CanvasGroup logoCanvasGroup;

    private float fadeInTime = 1.2f;
    private float stayTime = 2.0f;
    private float fadeOutTime = 1.0f;
    
    private string nextSceneName = Define.Scene.MAIN;

    private void Start()
    {
        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
            StartCoroutine(PlaySplashSequence());
        }
    }

    /// <summary>
    /// 로고 연출 재생 후 SceneLoader를 통해 씬 전환을 요청
    /// </summary>
    private IEnumerator PlaySplashSequence()
    {
        // 1. Fade In
        yield return StartCoroutine(Fade(0f, 1f, fadeInTime));

        // 2. 유지
        yield return new WaitForSeconds(stayTime);

        // 3. Fade Out
        yield return StartCoroutine(Fade(1f, 0f, fadeOutTime));

        // 4. SceneLoader 호출
        SceneLoader.Instance.LoadScene(nextSceneName);
    }

    /// <summary>
    /// 페이드 인/아웃 연출 로직
    /// </summary>
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            logoCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        logoCanvasGroup.alpha = endAlpha;
    }
}