using UnityEngine;

public class ObjectsGravityController : MonoBehaviour
{
    // 중력의 주체
    public PlanetGravity planet;

    // 플레이어
    private Objects objects;

    // 중력 방향의 반대 벡터 (플레이어의 위쪽 방향 벡터)
    public Vector3 gravityUp { get; private set; }

    private void Start()
    {
        objects = GetComponent<Objects>();
        objects.rb.useGravity = false;  // 리지드바디의 기본 중력은 필요 없으므로 비활성화
        planet.Attract(objects);
    }

    // 중력을 적용
    private void FixedUpdate()
    {
        planet.Attract(objects);
        gravityUp = planet.GetGravityUp(objects.gameObject.transform);
        objects.rb.useGravity = false;
    }

}
