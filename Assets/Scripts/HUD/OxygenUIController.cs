using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // DOTween 사용을 위해 추가

public class OxygenUIController : MonoBehaviour
{
    public PlayerStat playerStat;
    [SerializeField] private Image oxygenImage;
    [SerializeField] private TMP_Text oxygenValueText;

    [Header("UI Sprites")]
    [SerializeField] private Sprite blueRingSprite; // 파란색 링 이미지 연결
    [SerializeField] private Sprite redRingSprite;  // 빨간색 링 이미지 연결
    
    [Header("경고 Settings")]
    [SerializeField] private float warningAmount = 0.5f; // 이 수치 이하일 때 빨간색으로 변경
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color warningTextColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    [Header("DOTween Settings")]
    [SerializeField] private float fillDuration = 0.3f; // 게이지가 부드럽게 변하는 데 걸리는 시간(초)

    public void SetPlayerStat(PlayerStat stat)
    {
        if (playerStat == stat) return;
        Unhook();
        playerStat = stat;
        if (isActiveAndEnabled)
            Hook();
    }

    private void Hook()
    {
        if (playerStat == null) return;
        // OnEnable과 SetPlayerStat 둘 다에서 호출될 수 있어 이중 구독 방지
        playerStat.OnOxygenChanged -= UpdateOxygenFill;
        playerStat.OnOxygenChanged += UpdateOxygenFill;
        UpdateOxygenFill(playerStat.GetOxygen());
    }

    private void Unhook()
    {
        if (playerStat == null) return;
        playerStat.OnOxygenChanged -= UpdateOxygenFill;
    }

    private void OnEnable()
    {
        Hook();
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void UpdateOxygenFill(float amount)
    {
        if (oxygenImage != null)
        {
            // 1. 기존 진행 중인 애니메이션이 있다면 취소 (중복 겹침 방지)
            oxygenImage.DOKill();

            // 2. DOTween을 사용하여 FillAmount를 부드럽게 변화시킴
            oxygenImage.DOFillAmount(amount, fillDuration).SetEase(Ease.OutQuad);
            
            // 3. 목표 수치(amount)에 따라 이미지 교체
            if (amount <= warningAmount) 
            {
                oxygenImage.sprite = redRingSprite; // 빨간색 링으로 교체
            }
            else
            {
                oxygenImage.sprite = blueRingSprite; // 파란색 링 유지
            }
        }

        UpdateOxygenText(amount);
    }

    private void UpdateOxygenText(float amount)
    {
        if (oxygenValueText == null) return;

        // 산소는 0~1, UI에는 퍼센트로 표기
        oxygenValueText.text = $"{Mathf.RoundToInt(amount * 100f)}";

        bool warning = amount <= warningAmount;
        oxygenValueText.color = warning ? warningTextColor : normalTextColor;
    }
}