using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class CameraOrbit : MonoBehaviour
{
    [Header("노드 설정")]
    public Transform startNode;
    public Transform endNode;

    [Header("연출 설정")]
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 2.0f;

    private string targetTag = Define.Tag.PLANET;
    private List<Transform> _orderedNodes = new List<Transform>();
    private int _currentIndex = 0;
    private bool _isMovingForward = true;
    private Transform _currentTarget;
    private Rigidbody _rb;

    void Start()
    {
        // Rigidbody 설정: 이 설정이 되어 있어야 트리거가 작동해.
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true; 
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        GeneratePath();
        
        if (_orderedNodes.Count > 0)
        {
            _currentIndex = 0;
            _currentTarget = _orderedNodes[_currentIndex];
        }
    }

    void GeneratePath()
    {
        // 1. 태그로 모든 행성 탐색 (시작/끝 노드 제외)
        List<Transform> allPlanets = GameObject.FindGameObjectsWithTag(targetTag)
            .Select(obj => obj.transform)
            .Where(t => t != startNode && t != endNode)
            .ToList();

        _orderedNodes.Clear();
        _orderedNodes.Add(startNode);
        Transform current = startNode;

        // 2. 3D 거리 기준 Greedy 정렬
        while (allPlanets.Count > 0)
        {
            Transform closest = allPlanets.OrderBy(p => Vector3.Distance(current.position, p.position)).First();
            _orderedNodes.Add(closest);
            allPlanets.Remove(closest);
            current = closest;
        }
        _orderedNodes.Add(endNode);
    }

    void FixedUpdate()
    {
        if (_currentTarget == null) return;

        // 1. 타겟 방향으로 부드럽게 회전
        Vector3 direction = (_currentTarget.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotateSpeed));
        }

        // 2. 물리 엔진을 이용한 이동 (트리거 감지 최적화)
        Vector3 nextPos = transform.position + (transform.forward * moveSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(nextPos);
    }

    // 네가 만들어둔 IsTrigger 콜라이더와 충돌했을 때 실행되는 구간
    private void OnTriggerEnter(Collider other)
    {
        // 현재 내가 가고 있는 타겟의 콜라이더에 닿았을 때만 다음으로 넘어가게 함
        if (other.transform == _currentTarget)
        {
            SetNextTarget();
        }
    }

    void SetNextTarget()
    {
        // 왕복(Yoyo) 이동 로직
        if (_isMovingForward)
        {
            if (_currentIndex >= _orderedNodes.Count - 1)
            {
                _isMovingForward = false;
                _currentIndex--;
            }
            else _currentIndex++;
        }
        else
        {
            if (_currentIndex <= 0)
            {
                _isMovingForward = true;
                _currentIndex++;
            }
            else _currentIndex--;
        }

        _currentTarget = _orderedNodes[_currentIndex];
    }
}