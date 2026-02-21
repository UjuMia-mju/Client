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
    
    [Header("UI Popup (Prefab)")]
    [SerializeField] private GameObject warningPrefab; 
    [SerializeField] private Transform warningParent;

    private GameObject _currentWarningPopup; 
    private Coroutine _warningCoroutine;
    
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

    /// <summary>
    /// Button OnClick 이벤트
    /// </summary>
    public void StartRebinding(int listIndex)
    {
        if (listIndex < 0 || listIndex >= keyBindings.Count) return;

        KeyBindingItem item = keyBindings[listIndex];
        InputAction action = DataManager.Instance.InputAsset.FindAction(item.actionName);
        if (action == null) return;

        // ★ 변경: 시작할 때 떠있던 복제본 즉시 삭제
        HideWarningPopup();

        _rebindOperation?.Dispose();
        action.Disable();

        _rebindOperation = action.PerformInteractiveRebinding(item.bindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                string newPath = action.bindings[item.bindingIndex].effectivePath;

                if (IsDuplicateKey(action, item.bindingIndex, newPath))
                {
                    action.RemoveBindingOverride(item.bindingIndex);

                    // 프리팹 띄우기
                    ShowWarningPopup();

                    action.Enable();
                    operation.Dispose();

                    // (유지) 버튼 텍스트 원상복구
                    StartCoroutine(ResetButtonTextRoutine(item, action));
                    return;
                }

                // 성공 시에도 떠있던 팝업 확실히 지우기
                HideWarningPopup();

                RebindComplete(action, item);
                operation.Dispose();
            })
            .OnCancel(operation =>
            {
                action.Enable();
                operation.Dispose();
                item.buttonText.text = GetKeyName(action, item.bindingIndex);

                // ★ 변경: 취소 시 떠있던 복제본 즉시 삭제
                HideWarningPopup();
            })
            .Start();
    }

    // 경고 팝업 제어 (GameObject)
    // 화면에 떠있는 팝업을 즉시 삭제하는 함수
    private void HideWarningPopup()
    {
        if (_warningCoroutine != null) StopCoroutine(_warningCoroutine);
        
        if (_currentWarningPopup != null)
        {
            Destroy(_currentWarningPopup); // 생성된 복제본만 파괴
        }
    }

    private void ShowWarningPopup()
    {
        // 부모나 프리팹이 연결 안되어있으면 실행 안함
        if (warningPrefab == null || warningParent == null) return; 

        HideWarningPopup(); // 기존 거 지우고

        // ★ 변경: 부모(Canvas) 안에 생성하고, 확실하게 켜주기(SetActive)
        _currentWarningPopup = Instantiate(warningPrefab, warningParent);
        _currentWarningPopup.SetActive(true); 
        
        _warningCoroutine = StartCoroutine(DestroyWarningRoutine(_currentWarningPopup));
    }
    
    // 2초 뒤에 삭제하는 코루틴
    private IEnumerator DestroyWarningRoutine(GameObject targetPopup)
    {
        yield return new WaitForSeconds(2.0f); // 2초 대기
        
        // 시간이 다 되면 오브젝트 파괴
        if (targetPopup != null)
        {
            Destroy(targetPopup);
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
            // 마우스
            case "Left Button": return "LB"; 
            case "Right Button": return "RB";
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