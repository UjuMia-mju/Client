using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class InputManager : MonoBehaviorSingleton<InputManager>
{
    private PlayerInputSystem _actions;

    // 외부에서 접근할 때 없으면 바로 생성해서 반환합니다.
    public PlayerInputSystem Actions 
    { 
        get
        {
            if (_actions == null)
            {
                _actions = new PlayerInputSystem();
                _actions.Enable(); // 생성 시점에 바로 활성화
            }
            return _actions;
        }
    }

    protected override void Awake()
    {
        // 부모(MonoBehaviorSingleton)의 Awake를 호출하여 DontDestroyOnLoad 등을 처리
        base.Awake();

        // 이미 get을 통해 생성되었을 수도 있으니 null 체크 후 초기화
        if (_actions == null)
        {
            _actions = new PlayerInputSystem();
            _actions.Enable();
        }
    }

    private void OnEnable()
    {
        _actions?.Enable();
    }

    private void OnDisable()
    {
        _actions?.Disable();
    }

    private void OnDestroy()
    {
        // 싱글톤 인스턴스가 파괴될 때만 정리
        _actions?.Disable();
        _actions = null;
    }
}