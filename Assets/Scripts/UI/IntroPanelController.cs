using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// IntroPanel 애니메이션 제어, 유저 입력을 감지하여 MainManager를 통해 패널을 전환
/// </summary>
public class IntroPanelController : MonoBehaviour
{
    [SerializeField] private MainManager mainManager;
    
    [Header("")]
    public TextMeshProUGUI pressText;
    public Image titleLogoImage;

    private InputAction _anyKeyAction;
    
    private bool _canInteract = false;

    void Awake()
    {
        // 모든 장치의 버튼 입력을 감지하기 위한 설정
        _anyKeyAction = new InputAction(binding: "/*/<button>");
        _anyKeyAction.performed += ctx => OnAnyKeyPressed();
    }

    void Start()
    {
        // 연출을 위해 IntroPanel을 투명하게 함
        SetUIAlpha(titleLogoImage, 0);
        SetUIAlpha(pressText, 0);
        
        // 인트로 연출 시작
        StartCoroutine(PlayIntroSequence());
    }

    private void OnEnable() => _anyKeyAction.Enable();
    private void OnDisable() => _anyKeyAction.Disable();

    /// <summary>
    /// 모든 장치의 버튼 입력을 감지하기 위한 설정
    /// </summary>
    private void OnAnyKeyPressed()
    {
        // 연출 중이거나 이미 눌렀다면 무시
        if (!_canInteract) return;

        _canInteract = false; 
        _anyKeyAction.Disable(); // 추가 입력 방지

        // MainManager를 통해 로비 패널로 전환
        if (mainManager != null)
        {
            mainManager.ChangeFromIntroToMenu();
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        // 1. 타이틀 로고 페이드 인 및 스케일 업 (유명 게임 스타일)
        yield return StartCoroutine(FadeInLogo());

        yield return new WaitForSeconds(0.5f);
        
        // 2. 입력 가능 상태로 변경 및 "Press Any Key" 점멸 시작
        _canInteract = true;
        StartCoroutine(AnimatePressText());
    }

    private IEnumerator FadeInLogo()
    {
        float elapsed = 0f;
        float duration = 2.0f;
        Vector3 startScale = new Vector3(0.85f, 0.85f, 0.85f);
        titleLogoImage.transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float alpha = Mathf.SmoothStep(0, 1, progress);
            
            SetUIAlpha(titleLogoImage, alpha);
            titleLogoImage.transform.localScale = Vector3.Lerp(startScale, Vector3.one, alpha);
            yield return null;
        }
    }

    private IEnumerator AnimatePressText()
    {
        // 루프 애니메이션 (MainManager에 의해 패널이 비활성화될 때까지)
        while (_canInteract)
        {
            float alpha = (Mathf.Sin(Time.time * 3f) + 1.0f) / 2.0f;
            SetUIAlpha(pressText, Mathf.Clamp(alpha, 0f, 1.0f));
            yield return null;
        }
    }

    private void SetUIAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}