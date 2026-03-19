using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviorSingleton<InputManager>
{
    private static InputManager instance;
    
    // 외부에서 접근할 실제 Input Actions
    public PlayerInputSystem Actions { get; private set; }

    private void Awake()
    {
        Actions = new PlayerInputSystem();
        Actions.Enable();
    }

    private void OnEnable()
    {
        Actions?.Enable();
    }

    private void OnDisable()
    {
        // 앱이 꺼지거나 오브젝트가 비활성화될 때 안전하게 정리
        Actions?.Disable();
    }

    private void OnDestroy()
    {
        Actions?.Disable();
        Actions = null;
    }
}