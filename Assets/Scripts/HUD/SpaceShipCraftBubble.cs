using UnityEngine;

/// <summary>
/// Rocket Trigger 안에 로컬 Player가 있을 때 CraftBubble을 켤 후보로 두고,
/// <see cref="InputManager.IsGameplaySuppressed"/> 동안(ReadyToStart 등)에는 비활성입니다.
/// </summary>
public class SpaceShipCraftBubble : MonoBehaviour
{
    [Header("CraftBubble")]
    [SerializeField] private GameObject bubblePrefab;

    private GameObject _bubbleInstance;
    private int _playerOverlapCount;
    /// <summary>트리거 조건상 버블을 켜야 할지. 실제 활성 여부는 <see cref="InputManager.IsGameplaySuppressed"/>와 함께 결정합니다.</summary>
    private bool _desiredBubbleVisible;

    private void Awake()
    {
        GameplayReadyCoordinator.WhenGateReleased(ApplyBubbleVisibility);
    }

    private void OnDestroy()
    {
        GameplayReadyCoordinator.CancelWhenGateReleased(ApplyBubbleVisibility);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        _playerOverlapCount++;
        EnsureBubbleInstance();
        SetBubbleDesiredVisible(true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsPlayer(other))
            return;

        EnsureBubbleInstance();
        SetBubbleDesiredVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
        if (_playerOverlapCount == 0)
            SetBubbleDesiredVisible(false);
    }

    private void OnDisable()
    {
        SetBubbleDesiredVisible(false);
        _playerOverlapCount = 0;
    }

    private static bool IsPlayer(Collider other)
    {
        if (other == null || !other.CompareTag(Define.Tag.PLAYER))
            return false;
        return other.GetComponentInParent<Player>() != null;
    }

    private void EnsureBubbleInstance()
    {
        if (bubblePrefab == null || _bubbleInstance != null)
            return;

        _bubbleInstance = Instantiate(bubblePrefab);
        ApplyBubbleVisibility();
    }

    private void SetBubbleDesiredVisible(bool visible)
    {
        _desiredBubbleVisible = visible;
        ApplyBubbleVisibility();
    }

    private void ApplyBubbleVisibility()
    {
        if (_bubbleInstance == null)
            return;
        bool visible = _desiredBubbleVisible && !InputManager.IsGameplaySuppressed;
        _bubbleInstance.SetActive(visible);
    }
}
