using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// 키 변경 및 마우스 감도 조절
/// </summary>
public class ControlPanelController : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityText;
    
    [Header("UI Popup (Prefab)")]
    [SerializeField] private GameObject warningPrefab; 
    [SerializeField] private Transform warningParent;
    
    // TODO: 마우스 감도 게임 씬에 적용해야 함
    public static float MouseSensitivity = 1.0f;

    [System.Serializable]
    public class KeyBindingItem
    {
        public string actionName;       
        public int bindingIndex;        
        public TextMeshProUGUI buttonText; 
        public Button keyButton;
    }

    [Header("Key Bindings Setup")]
    [SerializeField] private List<KeyBindingItem> keyBindings;

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
    
    // 키변경 버튼 깜빡임 효과
    private Coroutine _blinkCoroutine;
    private Button _currentBlinkingButton; // 현재 깜빡이고 있는 버튼 기억
    private Color _normalColor = Color.white; // 기본 색상
    private Color _blinkColor = new Color(0.8f, 0f, 0.3f); // 깜빡일 때 색상
    
    private void Start()
    {
        float savedSensitivity = DataManager.Instance.data.mouseSensitivity;
        MouseSensitivity = savedSensitivity; 

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = savedSensitivity;
            UpdateSensitivityText(savedSensitivity);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        RefreshKeyBindingsUI();
    }
    
    /// <summary>
    /// 버튼 내장 색상(ColorBlock)을 변경하는 코루틴
    /// </summary>
    private IEnumerator BlinkButtonRoutine(Button targetButton)
    {
        if (targetButton == null) yield break;

        bool isWhite = false;
        while (true)
        {
            // Button의 ColorBlock을 가져와서 색상을 수정한 뒤 다시 덮어씌워야 합니다.
            ColorBlock cb = targetButton.colors;
            cb.normalColor = isWhite ? _normalColor : _blinkColor;
            cb.selectedColor = isWhite ? _normalColor : _blinkColor; // 클릭(선택)된 상태일 수도 있으니 같이 변경
            targetButton.colors = cb;
        
            isWhite = !isWhite;
            yield return new WaitForSeconds(0.5f);
        }
    }
    /// <summary>
    /// 깜빡임을 멈추고 버튼 색상을 원래대로 돌려놓는 메서드
    /// </summary>
    private void StopBlink()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
    
        // 이전에 깜빡이던 버튼이 있다면 원래 색상으로 복구
        if (_currentBlinkingButton != null)
        {
            ColorBlock cb = _currentBlinkingButton.colors;
            cb.normalColor = _normalColor;
            cb.selectedColor = _normalColor;
            _currentBlinkingButton.colors = cb;
        
            _currentBlinkingButton = null;
        }
    }
    
    /// <summary>
    /// 마우스 감도 슬라이더 이벤트
    /// </summary>
    public void OnSensitivityChanged(float value)
    {
        DataManager.Instance.data.mouseSensitivity = value;
        ControlPanelController.MouseSensitivity = value;
        DataManager.Instance.Save();
        UpdateSensitivityText(value);
    }

    /// <summary>
    /// 마우스 감도 텍스트 업데이트
    /// </summary>
    private void UpdateSensitivityText(float value)
    {
        if (sensitivityText != null)
            sensitivityText.text = value.ToString("F2");
    }

    /// <summary>
    /// 키 설정 UI 새로고침
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
    /// 키 중복 검사 로직
    /// </summary>
    private (bool isDuplicate, InputAction duplicateAction, int duplicateIndex) CheckDuplicateKey(InputAction targetAction, int targetIndex, string newPath)
    {
        var actionMap = targetAction.actionMap; 

        foreach (var action in actionMap.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action == targetAction && i == targetIndex) continue;
                if (action.bindings[i].isComposite) continue;

                string pathToCheck = action.bindings[i].effectivePath;
                if (string.IsNullOrEmpty(pathToCheck)) continue;

                if (newPath == pathToCheck)
                {
                    return (true, action, i); 
                }
            }
        }
        return (false, null, -1); 
    }

    /// <summary>
    /// 키 변경 로직
    /// </summary>
    public void StartRebinding(int listIndex)
    {
        if (listIndex < 0 || listIndex >= keyBindings.Count) return;

        KeyBindingItem item = keyBindings[listIndex];
        InputAction action = DataManager.Instance.InputAsset.FindAction(item.actionName);
        if (action == null) return;

        string oldOverridePath = action.bindings[item.bindingIndex].overridePath;
        string oldEffectivePath = action.bindings[item.bindingIndex].effectivePath; 

        // 기존 깜빡임을 멈추고, 누른 버튼의 깜빡임 시작
        StopBlink();
        if (item.keyButton != null)
        {
            _currentBlinkingButton = item.keyButton;
            _blinkCoroutine = StartCoroutine(BlinkButtonRoutine(item.keyButton));
        }

        _rebindOperation?.Dispose();
        action.Disable();

        _rebindOperation = action.PerformInteractiveRebinding(item.bindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                // 키 입력 완료 시 깜빡임 멈추기
                StopBlink(); 

                string newPath = action.bindings[item.bindingIndex].effectivePath;
                var duplicateResult = CheckDuplicateKey(action, item.bindingIndex, newPath);

                if (duplicateResult.isDuplicate)
                {
                    ShowWarningMessage(action, item, oldOverridePath, oldEffectivePath, newPath, duplicateResult.duplicateAction, duplicateResult.duplicateIndex);
                    operation.Dispose();
                    return;
                }

                RebindComplete(action, item);
                operation.Dispose();
            })
            .OnCancel(operation =>
            {
                // 키 입력 취소 시 깜빡임 멈추기
                StopBlink(); 

                action.Enable();
                operation.Dispose();
                item.buttonText.text = GetKeyName(action, item.bindingIndex);
            })
            .Start();
    }

    /// <summary>
    /// 중복 시 
    /// </summary>
    private void ShowWarningMessage(InputAction newAction, KeyBindingItem newItem, string oldOverridePath, 
        string oldEffectivePath, string newPath, InputAction existingAction, int existingIndex)
    {
        if (warningPrefab == null || warningParent == null) return; 

        GameObject popupObj = Instantiate(warningPrefab, warningParent);
        popupObj.SetActive(true); 

        WarningMessageController messageController = popupObj.GetComponent<WarningMessageController>();
        if (messageController == null) return;

        string keyNameUI = GetKeyName(newAction, newItem.bindingIndex);

        Action onKeepExisting = () =>
        {
            if (string.IsNullOrEmpty(oldOverridePath))
                newAction.RemoveBindingOverride(newItem.bindingIndex); 
            else
                newAction.ApplyBindingOverride(newItem.bindingIndex, oldOverridePath); 

            RebindComplete(newAction, newItem);
        };

        Action onApplyNew = () =>
        {
            // 맞교환(Swap) 로직
            if (string.IsNullOrEmpty(oldEffectivePath))
            {
                // 만약 뺏어온 액션이 원래 빈칸이었다면, 기존 액션도 빈칸으로 만듦
                existingAction.ApplyBindingOverride(existingIndex, "");
            }
            else
            {
                // 뺏긴 기존 액션에게, 방금 뺏어온 액션이 쓰던 '예전 키'를 덮어씌움
                existingAction.ApplyBindingOverride(existingIndex, oldEffectivePath);
            }

            // 새로운 액션은 RebindingOperation이 이미 덮어씌웠으므로 저장과 UI 갱신만 진행
            RebindComplete(newAction, newItem);
            RefreshKeyBindingsUI(); 
        };

        // 기존 액션 이름 매핑 (Define.KeyName 사용)
        string existingName = existingAction.name;
        string existingPartName = "";
        if (existingIndex >= 0 && existingIndex < existingAction.bindings.Count && existingAction.bindings[existingIndex].isPartOfComposite)
            existingPartName = existingAction.bindings[existingIndex].name.ToLower();

        if (existingName.Equals("Move", StringComparison.OrdinalIgnoreCase))
        {
            switch (existingPartName)
            {
                case "up": existingName = Define.KeyName.up; break;
                case "down": existingName = Define.KeyName.down; break;
                case "left": existingName = Define.KeyName.left; break;
                case "right": existingName = Define.KeyName.right; break;
                default: existingName = Define.KeyName.move; break;
            }
        }
        else
        {
            switch (existingName.ToLower())
            {
                case "jump": existingName = Define.KeyName.jump; break;
                case "throworcancel": existingName = Define.KeyName.@throw; break;
                case "interact": existingName = Define.KeyName.interact; break;
            }
        }

        string newName = newAction.name;
        string newPartName = "";
        if (newItem.bindingIndex >= 0 && newItem.bindingIndex < newAction.bindings.Count && newAction.bindings[newItem.bindingIndex].isPartOfComposite)
            newPartName = newAction.bindings[newItem.bindingIndex].name.ToLower();

        if (newName.Equals("Move", StringComparison.OrdinalIgnoreCase))
        {
            switch (newPartName)
            {
                case "up": newName = Define.KeyName.up; break;
                case "down": newName = Define.KeyName.down; break;
                case "left": newName = Define.KeyName.left; break;
                case "right": newName = Define.KeyName.right; break;
                default: newName = Define.KeyName.move; break;
            }
        }
        else
        {
            switch (newName.ToLower())
            {
                case "jump": newName = Define.KeyName.jump; break;
                case "throworcancel": newName = Define.KeyName.@throw; break;
                case "interact": newName = Define.KeyName.interact; break;
            }
        }

        messageController.Initialize(existingName, newName, keyNameUI, onKeepExisting, onApplyNew);
    }

    private void RebindComplete(InputAction action, KeyBindingItem item)
    {
        action.Enable();
        item.buttonText.text = GetKeyName(action, item.bindingIndex);
        DataManager.Instance.Save(); 
    }

    private string GetKeyName(InputAction action, int bindingIndex)
    {
        if (action == null || action.bindings.Count <= bindingIndex) return "??";
        
        string path = action.bindings[bindingIndex].effectivePath;
        if (string.IsNullOrEmpty(path)) return "??"; 

        string keyName = InputControlPath.ToHumanReadableString(
            path,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        keyName = keyName.Replace("Digit ", "");

        switch (keyName)
        {
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
            case "Left Button": return "LB"; 
            case "Right Button": return "RB";
        }

        if (keyName.Length > 2)
            keyName = keyName.Substring(0, 2);

        return keyName.ToUpper();
    }
}