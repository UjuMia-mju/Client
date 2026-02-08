using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGravityController : MonoBehaviour
{
    // 중력의 주체
    public PlanetGravity planet;

    // 플레이어
    private Player player;

    // 중력 방향의 반대 벡터 (플레이어의 위쪽 방향 벡터)
    public Vector3 gravityUp { get; private set; }

    private void Start()
    {
        player = GetComponent<Player>();
        player.rb.useGravity = false;  // 리지드바디의 기본 중력은 필요 없으므로 비활성화
        player.rb.constraints = RigidbodyConstraints.FreezeRotation;	// 꼿꼿히 세울 것이므로 회전을 비활성화함
        planet.AttractPlayer(player);
    }

    // 중력을 적용
    private void FixedUpdate()
    {
        planet.AttractPlayer(player);
        gravityUp = planet.GetGravityUp(player.gameObject.transform);
        player.rb.useGravity = false;
    }
}