using UnityEngine;

public class MovingObject : MonoBehaviour
{
    public Rigidbody rb { get; protected set; }
    protected Vector3 moveAmount;
    public float walkSpeed = 10;
    public float jumpForce = 5;

    protected LayerMask groundMask;
    protected LayerMask wallMask;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    protected virtual void Moving(Vector3 movDir)
    {
        Vector3 targetMoveAmount = movDir * walkSpeed;
        moveAmount = Vector3.MoveTowards(moveAmount, targetMoveAmount, walkSpeed);
        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    protected virtual void Jump()
    {
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
}