using System;
using System.Collections.Generic;
using Protocol;
using UnityEngine;

/// <summary>
/// 스테이지 선택 씬에서 S_GAME_READY_TO_START(또는 폴백)를 받은 뒤 게임 씬으로 넘길 때,
/// 서버 기준 남은 시간이 씬 로드 차이로 줄어들지 않도록 페이로드를 보관했다가
/// 인게임(PlayManager)에서 ReadyToStartPanel과 함께 소비합니다.
/// </summary>
public static class GameplayReadyCoordinator
{
    static S_GAME_READY_TO_START _serverPacket;
    static List<ulong> _fallbackIds;
    static int _fallbackDelaySeconds;
    static bool _useFallback;

    static bool _gateBlocking;

    static event Action _onReleased;

    public static bool IsGateBlocking => _gateBlocking;

    public static void ResetForStageSelect()
    {
        _serverPacket = null;
        _useFallback = false;
        _fallbackIds = null;
        _gateBlocking = false;
        _onReleased = null;
    }

    public static void SetPendingFromServer(S_GAME_READY_TO_START packet)
    {
        _serverPacket = packet?.Clone();
        _useFallback = false;
        _fallbackIds = null;
        _gateBlocking = _serverPacket != null;
    }

    public static void SetPendingFallback(IReadOnlyList<ulong> idOrder, int delaySeconds)
    {
        _serverPacket = null;
        _useFallback = true;
        _fallbackDelaySeconds = Mathf.Max(0, delaySeconds);
        _fallbackIds = idOrder != null ? new List<ulong>(idOrder) : new List<ulong>();
        _gateBlocking = true;
    }

    /// <summary>인게임에서 1회: 보관된 데이터를 꺼내 UI에 넘깁니다. 게이트는 카운트다운 종료까지 유지됩니다.</summary>
    public static bool TryTakePendingForUi(
        out S_GAME_READY_TO_START serverPacket,
        out bool useFallback,
        out IReadOnlyList<ulong> fallbackIds,
        out int fallbackDelaySeconds)
    {
        serverPacket = _serverPacket;
        useFallback = _useFallback;
        fallbackIds = _fallbackIds;
        fallbackDelaySeconds = _fallbackDelaySeconds;

        _serverPacket = null;
        _useFallback = false;
        _fallbackIds = null;

        return serverPacket != null || useFallback;
    }

    /// <summary>게이트 활성 시 해제될 때까지 대기합니다. 비활성이면 즉시 호출됩니다.</summary>
    public static void WhenGateReleased(Action action)
    {
        if (action == null) return;

        if (!_gateBlocking)
        {
            action();
            return;
        }

        _onReleased += action;
    }

    /// <summary>씬 unload 등으로 더 이상 게이트 이벤트를 받지 않을 때 구독을 제거합니다.</summary>
    public static void CancelWhenGateReleased(Action action)
    {
        if (action != null)
            _onReleased -= action;
    }

    public static void NotifyGateReleased()
    {
        if (!_gateBlocking)
            return;

        _gateBlocking = false;
        var h = _onReleased;
        _onReleased = null;
        h?.Invoke();
        InputManager.RefreshAfterCoordinatorReleased();
    }
}
