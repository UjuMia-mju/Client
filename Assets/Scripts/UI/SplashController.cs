using UnityEngine;
using System.Collections;

/// <summary>
/// 스플래시 로고의 페이드 인/아웃 연출을 담당하는 클래스
/// </summary>
public class SplashController : MonoBehaviour
{
    [Header("CanvasGroup Of Logo")]
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

    private IEnumerator PlaySplashSequence()
    {
        // 1. 로고 나타남
        yield return StartCoroutine(Fade(0f, 1f, fadeInTime));

        // 2. 유지
        yield return new WaitForSeconds(stayTime);

        // 3. 로고 사라짐
        yield return StartCoroutine(Fade(1f, 0f, fadeOutTime));

        // 4. 로고가 완전히 사라지면 SceneLoader에게 모든 권한 위임
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