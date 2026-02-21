using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 조작키 및 마우스 감도 설정
/// </summary>
public class ControlPanelController : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityText;
    
    [Header("UI Message")]
    [SerializeField] private GameObject warningObject;
    
    private Coroutine _warningCoroutine; // 코루틴 제어용 변수
    
    // 플레이어 이동 스크립트 등에서 참조할 static 변수
    public static float MouseSensitivity = 1.0f;

    [System.Serializable]
    public class KeyBindingItem
    {
        public string actionName;       
        public int bindingIndex;        
        public TextMeshProUGUI buttonText; 
    }

    [Header("Key Bindings Setup")]
    [SerializeField] private List<KeyBindingItem> keyBindings;

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
    
    private void Start()
    {
        // 1. 감도 불러오기 (DataManager에 저장된 값 사용)
        // DataManager가 Awake에서 이미 Load를 끝냈으므로 바로 가져오면 됩니다.
        float savedSensitivity = DataManager.Instance.data.mouseSensitivity;
        
        MouseSensitivity = savedSensitivity; // Static 변수 갱신

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = savedSensitivity;
            UpdateSensitivityText(savedSensitivity);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        // 2. 키 바인딩 UI 갱신 (DataManager가 이미 로드해둔 InputAsset 상태를 화면에 표시)
        RefreshKeyBindingsUI();
        
        if (warningObject != null)
        {
            warningObject.SetActive(false);
        }
    }

    /// <summary>
    /// 마우스 감도 변경
    /// </summary>
    public void OnSensitivityChanged(float value)
    {
        // 1. DataManager 데이터 갱신
        DataManager.Instance.data.mouseSensitivity = value;
    
        // 2. static 변수 갱신 (인게임 적용용)
        ControlPanelController.MouseSensitivity = value;

        // 3. 저장 요청
        DataManager.Instance.Save();
        
        // 텍스트 갱신
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityText != null)
            sensitivityText.text = value.ToString("F2");
    }

    /// <summary>
    /// 키 바인딩 UI 갱신
    /// </summary>
    public void RefreshKeyBindingsUI()
    {
        foreach (var item in keyBindings)
        {
            InputAction action = DataManager.Instance.InputAsset.FindAction(item.actionName);
            
            if (action != null)
            {
                item.buttonText.text = GetKeyName(action, item.bindingIndex);
            }
        }
    }

    /// <summary>
    /// 중복 키 검사 (수정됨)
    /// </summary>
    private bool IsDuplicateKey(InputAction targetAction, int targetIndex, string newPath)
    {
        var actionMap = targetAction.actionMap; 

        foreach (var action in actionMap.actions)
        {
            // 액션의 모든 바인딩(키 설정)을 순회
            for (int i = 0; i < action.bindings.Count; i++)
            {
                // 1. 자기 자신(지금 바꾸고 있는 키)은 비교 대상에서 제외
                if (action == targetAction && i == targetIndex) continue;

                // 2. Composite(Vector2 껍데기 등)은 실제 키가 아니므로 제외
                if (action.bindings[i].isComposite) continue;

                // 3. ★ 수정된 부분: 단순 문자열 비교 (==) 사용
                // effectivePath가 null일 수도 있으므로 안전하게 처리
                string pathToCheck = action.bindings[i].effectivePath;
                if (string.IsNullOrEmpty(pathToCheck)) continue;

                if (newPath == pathToCheck)
                {
                    Debug.LogWarning($"중복 발견! '{action.name}' 액션에서 이미 사용 중입니다.");
                    return true; // 중복됨!
                }
            }
        }
        return false; // 중복 없음
    }
    
    public void StartRebinding(int listIndex)
    {
        if (listIndex < 0 || listIndex >= keyBindings.Count) return;

        KeyBindingItem item = keyBindings[listIndex];
        InputAction action = DataManager.Instance.InputAsset.FindAction(item.actionName);
        
        if (action == null) return;

        // 리바인딩 시작할 때 혹시 켜져있던 경고창 끄기
        if(warningObject != null) warningObject.SetActive(false);

        _rebindOperation?.Dispose();
        action.Disable();

        item.buttonText.text = "Waiting...";

        _rebindOperation = action.PerformInteractiveRebinding(item.bindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                string newPath = action.bindings[item.bindingIndex].effectivePath;

                // 중복 검사
                if (IsDuplicateKey(action, item.bindingIndex, newPath))
                {
                    // 1. 중복 시 키 설정 취소 (복구)
                    action.RemoveBindingOverride(item.bindingIndex);
                    
                    // 2. 버튼 텍스트에 "DUPLICATE" 표시
                    item.buttonText.text = "DUPLICATE!";
                    
                    // 3. ★ 경고 오브젝트(패널) 띄우기
                    ShowWarningPopup();

                    action.Enable();
                    operation.Dispose();
                    
                    // 1초 뒤 버튼 텍스트 원상복구
                    StartCoroutine(ResetButtonTextRoutine(item, action));
                    return;
                }

                RebindComplete(action, item);
                operation.Dispose();
            })
            .OnCancel(operation =>
            {
                action.Enable();
                operation.Dispose();
                item.buttonText.text = GetKeyName(action, item.bindingIndex);
                
                // 취소 시 경고창 끄기
                if(warningObject != null) warningObject.SetActive(false);
            })
            .Start();
    }

    // =================================================================
    // 💡 경고 팝업 제어 (GameObject)
    // =================================================================

    private void ShowWarningPopup()
    {
        if (warningObject == null) return;

        // 이미 켜져있는 코루틴이 있다면 끄고 다시 시작 (시간 리셋)
        if (_warningCoroutine != null) StopCoroutine(_warningCoroutine);
        
        warningObject.SetActive(true); // ★ 오브젝트 켜기
        
        _warningCoroutine = StartCoroutine(HideWarningRoutine());
    }

    // 2초 뒤에 자동으로 꺼지는 코루틴
    private IEnumerator HideWarningRoutine()
    {
        yield return new WaitForSeconds(2.0f); // 2초 대기
        
        if (warningObject != null)
        {
            warningObject.SetActive(false); // ★ 오브젝트 끄기
        }
    }

    private IEnumerator ResetButtonTextRoutine(KeyBindingItem item, InputAction action)
    {
        yield return new WaitForSeconds(1.0f);
        item.buttonText.text = GetKeyName(action, item.bindingIndex);
    }

    private void RebindComplete(InputAction action, KeyBindingItem item)
    {
        action.Enable();
        item.buttonText.text = GetKeyName(action, item.bindingIndex);

        // 저장
        DataManager.Instance.Save(); 
    }

    private string GetKeyName(InputAction action, int bindingIndex)
    {
        if (action == null || action.bindings.Count <= bindingIndex) return "??";

        // 1. 기본 이름 가져오기
        string keyName = InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        // 2. "Digit 1" 같은 숫자 키에서 "Digit " 제거 (그냥 "1"로)
        keyName = keyName.Replace("Digit ", "");

        // 3. 특수 키들을 의미 있는 2글자로 변환 (원하는 대로 수정 가능)
        switch (keyName)
        {
            case "Space": return "SP";
            case "Enter": return "EN";
            case "Escape": return "ES";
            case "Left Shift": return "LS";
            case "Right Shift": return "RS";
            case "Left Ctrl": return "LC";
            case "Right Ctrl": return "RC";
            case "Left Alt": return "LA";
            case "Right Alt": return "RA";
            case "Tab": return "TB";
            case "Up Arrow": return "UP";
            case "Down Arrow": return "DN";
            case "Left Arrow": return "LT";
            case "Right Arrow": return "RT";
            case "Left Button": return "LB"; // 마우스
            case "Right Button": return "RB"; // 마우스
        }

        // 4. 그 외의 키가 2글자를 넘으면 앞 2글자만 자르기
        if (keyName.Length > 2)
        {
            keyName = keyName.Substring(0, 2);
        }

        // 5. 대문자로 반환
        return keyName.ToUpper();
    }
}