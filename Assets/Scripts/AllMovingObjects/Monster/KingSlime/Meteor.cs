using UnityEngine;

public class Meteor : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    private Vector3 targetPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (targetPos != null)
        {
            // Meteor 효과 오브젝트를 목표 위치로 이동
            this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, speed * Time.deltaTime);

            // 효과가 도착하면 삭제
            if (this.transform.position == targetPos)
            {
                Destroy(this.gameObject);
            }
        }
    }

    public void SetTargetPosition(Vector3 pos)
    {
        targetPos = pos;
    }
}