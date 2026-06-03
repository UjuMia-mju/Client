using UnityEngine;
using System;
using TMPro;
using UnityEngine.UIElements;

public class WarningMessageController : MonoBehaviour
{
    [Header("Left Button (기존 유지)")]
    [SerializeField] private Button leftButton;
    [SerializeField] private TextMeshProUGUI leftText;

    [Header("Right Button (새로운 설정 적용)")]
    [SerializeField] private Button rightButton;
    [SerializeField] private TextMeshProUGUI rightText;

    private Action _onKeepExisting;
    private Action _onApplyNew;

    /// <summary>
    /// 팝업 초기화 및 UI 세팅
    /// </summary>
    public void Initialize(string existingActionName, string newActionName, string conflictingKey, Action onKeepExisting, Action onApplyNew)
    {
        // 텍스트: "이동 (W)" / "던지기 (W)"
        leftText.text = MessageTexts.Format(MessageKeys.KeyBindingConflictChoice, existingActionName, conflictingKey);
        rightText.text = MessageTexts.Format(MessageKeys.KeyBindingConflictChoice, newActionName, conflictingKey);

        _onKeepExisting = onKeepExisting;
        _onApplyNew = onApplyNew;

        if (leftButton != null)
        {
            leftButton.clicked -= OnLeftButtonClicked;
            leftButton.clicked += OnLeftButtonClicked;
        }

        if (rightButton != null)
        {
            rightButton.clicked -= OnRightButtonClicked;
            rightButton.clicked += OnRightButtonClicked;
        }
    }

    public void OnLeftButtonClicked()
    {
        SoundManager.Instance.PlaySFX("Click2");
        _onKeepExisting?.Invoke(); // 기존 키 유지 콜백 실행
        Destroy(gameObject);       // 선택 완료 후 팝업 파괴
    }

    public void OnRightButtonClicked()
    {
        SoundManager.Instance.PlaySFX("Click2");
        _onApplyNew?.Invoke();     // 새 키 적용 콜백 실행
        Destroy(gameObject);       // 선택 완료 후 팝업 파괴
    }
}