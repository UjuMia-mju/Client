using UnityEngine;

/// <summary>
/// Run 애니메이션 클립의 Animation Event(OnFootstep)에서 호출되어
/// 발 밑 위치에 파티클을 1회 재생한다. Player와 OtherPlayers 양쪽에 부착해 사용.
/// AnimState가 모든 머신에 동기화되므로 별도 네트워크 작업 불필요.
/// </summary>
public class FootstepEmitter : MonoBehaviour
{
    [Tooltip("발자국이 찍힐 위치 기준 Transform (보통 Player 루트의 발 밑 빈 오브젝트)")]
    [SerializeField] private Transform footAnchor;
    [Tooltip("발자국 파티클 프리팹 (One Shot)")]
    [SerializeField] private GameObject footstepParticlePrefab;
    [Tooltip("스폰 시 위로 띄울 오프셋 (z-fighting 방지)")]
    [SerializeField] private float yOffset = 0.02f;
    [Tooltip("자동 정리 시간 (초)")]
    [SerializeField] private float autoDestroyTime = 2f;

    // Animation Event에서 호출
    public void OnFootstep()
    {
        if (footAnchor == null || footstepParticlePrefab == null) return;

        Vector3 pos = footAnchor.position + footAnchor.up * yOffset;
        Quaternion rot = Quaternion.LookRotation(footAnchor.forward, footAnchor.up);
        GameObject p = Instantiate(footstepParticlePrefab, pos, rot);
        Destroy(p, autoDestroyTime);
    }
}