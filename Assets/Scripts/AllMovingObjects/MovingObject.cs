using UnityEngine;

public class MovingObject : MonoBehaviour
{
    public Rigidbody rb { get; protected set; }
    protected Vector3 moveAmount;
    public float walkSpeed;
    public float jumpForce;

    protected LayerMask groundMask;
    protected LayerMask wallMask;

    protected Vector3 currentMoveDir;


    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentMoveDir = Vector3.zero;
        groundMask = LayerMask.GetMask("Ground");
        wallMask = LayerMask.GetMask("Wall");
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