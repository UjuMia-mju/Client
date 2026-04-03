using UnityEngine;

public class OtherPlayersGravityController : MonoBehaviour
{
    // 중력의 주체
    public PlanetGravity planet;

    // 플레이어
    private OtherPlayers player;

    // 중력 방향의 반대 벡터 (플레이어의 위쪽 방향 벡터)
    public Vector3 gravityUp { get; private set; }

    private void Start()
    {
        player = GetComponent<OtherPlayers>();
        player.rb.useGravity = false;
        player.rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // 중력을 적용
    //private void FixedUpdate()
    //{
    //    planet.Attract(player);
    //    gravityUp = planet.GetGravityUp(player.gameObject.transform);
    //    player.rb.useGravity = false;
    //}
}
