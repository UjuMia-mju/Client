// LobbyManager.cs (빈 오브젝트에 부착)
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public Vector3 spawnCenter; // 스폰될 왼쪽 구역의 중심 좌표
    public float spawnRadius = 4f; // 스폰 허용 반경

    // 누군가 방에 들어왔을 때 호출할 함수
    public void SpawnNewPlayer()
    {
        Vector3 safePosition = GetSafeSpawnPosition();
        Instantiate(playerPrefab, safePosition, Quaternion.identity);
    }

    // 빈 공간을 찾는 함수
    Vector3 GetSafeSpawnPosition()
    {
        int maxAttempts = 10; // 최대 10번 빈 공간을 찾음
        float playerRadius = 1f; // 플레이어의 대략적인 충돌체 반지름

        for (int i = 0; i < maxAttempts; i++)
        {
            // 중심점 주변 랜덤 위치 생성
            Vector3 randomPos = spawnCenter + Random.insideUnitSphere * spawnRadius;
            randomPos.z = spawnCenter.z; // Z축 고정

            // 해당 위치에 겹치는 다른 콜라이더(플레이어)가 없는지 확인
            if (!Physics.CheckSphere(randomPos, playerRadius))
            {
                return randomPos; // 안전한 빈 공간 발견 시 해당 위치 반환
            }
        }
        
        // 10번 다 실패했으면 그냥 중심에 스폰 (가장자리에 다 몰려있을 경우를 대비)
        return spawnCenter; 
    }
}