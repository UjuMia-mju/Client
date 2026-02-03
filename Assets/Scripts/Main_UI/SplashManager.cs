using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashController : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] private CanvasGroup logoCanvasGroup; // LogoImage의 Canvas Group
    
    private float fadeInTime = 2.5f;   // 로고 나타나는 시간
    private float stayTime = 0.1f;    // 로고 유지 시간
    private float fadeOutTime = 2.0f;  // 로고 사라지는 시간
    private string nextSceneName = "Main";

    void Start()
    {
        // 시작 시 로고를 투명하게 설정
        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f;
            StartCoroutine(PlaySplashSequence());
        }
    }

    IEnumerator PlaySplashSequence()
    {
        // 1. 로고 페이드 인
        yield return StartCoroutine(FadeCanvas(0f, 1f, fadeInTime));

        // 2. 대기
        yield return new WaitForSeconds(stayTime);

        // 3. 로고 페이드 아웃
        yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutTime));

        // 4. 다음 씬 로드
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeCanvas(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            logoCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        logoCanvasGroup.alpha = end;
    }
}