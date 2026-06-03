using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class InputManager : MonoBehaviorSingleton<InputManager>
{
    private PlayerInputSystem _actions;

    static int _readyPanelHoldDepth;
    static int _pauseMenuHoldDepth;
    static bool _prevGateBlocking;
    static event Action _onBecameUnblocked;

    static bool _appliedPlayerMapSuppressed;

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
        ApplyGameplayActionMapSuppression(forceSync: true);
    }

    private void Update()
    {
        ApplyGameplayActionMapSuppression(forceSync: false);
    }

    /// <summary>
    /// true면 이동·카메라 look(플레이어 액션맵) 등 게임플레이 입력을 막습니다.
    /// 일시정지 패널 포함. HUD ESC는 <see cref="IsEscBlockedForHud"/>를 씁니다.
    /// </summary>
    public static bool IsGameplaySuppressed =>
        GameplayReadyCoordinator.IsGateBlocking || _readyPanelHoldDepth > 0 || _pauseMenuHoldDepth > 0;

    /// <summary>Ready/로딩 게이트 중에만 true — 일시정지 중에는 false(ESC로 패널 닫기 허용).</summary>
    public static bool IsEscBlockedForHud =>
        GameplayReadyCoordinator.IsGateBlocking || _readyPanelHoldDepth > 0;

    /// <summary>ReadyToStart 패널이 활성화된 뒤 한 번 호출. <see cref="PopReadyPanelHold"/>와 짝을 맞춥니다.</summary>
    public static void PushReadyPanelHold()
    {
        _readyPanelHoldDepth++;
        RefreshBlockingState();
    }

    public static void PopReadyPanelHold()
    {
        _readyPanelHoldDepth = Mathf.Max(0, _readyPanelHoldDepth - 1);
        RefreshBlockingState();
    }

    /// <summary>일시정지(또는 HUD 전면 패널)가 연출 시작 시. <see cref="PopPauseMenuHold"/>와 짝을 맞춥니다.</summary>
    public static void PushPauseMenuHold()
    {
        _pauseMenuHoldDepth++;
        RefreshBlockingState();
    }

    public static void PopPauseMenuHold()
    {
        _pauseMenuHoldDepth = Mathf.Max(0, _pauseMenuHoldDepth - 1);
        RefreshBlockingState();
    }

    /// <summary><see cref="GameplayReadyCoordinator.NotifyGateReleased"/> 직후 호출됩니다.</summary>
    public static void RefreshAfterCoordinatorReleased()
    {
        RefreshBlockingState();
    }

    public static void WhenBecameUnblocked(Action action)
    {
        if (action == null) return;

        if (!IsGameplaySuppressed)
        {
            action();
            return;
        }

        _onBecameUnblocked += action;
    }

    public static void CancelWhenBecameUnblocked(Action action)
    {
        if (action != null)
            _onBecameUnblocked -= action;
    }

    /// <summary>스테이지 선택 씬 진입 시 잔여 홀드·대기 콜백을 비웁니다.</summary>
    public static void ResetGameplaySuppressionForStageSelect()
    {
        _readyPanelHoldDepth = 0;
        _pauseMenuHoldDepth = 0;
        _onBecameUnblocked = null;
        _prevGateBlocking = false;
        _appliedPlayerMapSuppressed = false;
        Instance?.ApplyGameplayActionMapSuppression(forceSync: true);
    }

    static void RefreshBlockingState()
    {
        bool now = IsGameplaySuppressed;
        if (_prevGateBlocking && !now)
        {
            var h = _onBecameUnblocked;
            _onBecameUnblocked = null;
            h?.Invoke();
        }

        _prevGateBlocking = now;
        Instance?.ApplyGameplayActionMapSuppression(forceSync: true);
    }

    void ApplyGameplayActionMapSuppression(bool forceSync)
    {
        if (_actions == null) return;

        bool suppress = IsGameplaySuppressed;
        if (!forceSync && _appliedPlayerMapSuppressed == suppress)
            return;

        _appliedPlayerMapSuppressed = suppress;

        if (suppress)
            _actions.Player.Disable();
        else
            _actions.Player.Enable();
    }

    private void OnEnable()
    {
        _actions?.Enable();
        ApplyGameplayActionMapSuppression(forceSync: true);
    }

    private void OnDisable()
    {
        _actions?.Disable();
    }

    protected override void OnDestroy()
    {
        if (_actions != null)
        {
            _actions.Disable();
            _actions.Dispose();
            _actions = null;
        }

        base.OnDestroy();
    }
}