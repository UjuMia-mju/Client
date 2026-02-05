using UnityEngine;
using System.Collections;

public class PlayerInput : MonoBehaviour
{
    public float axisX { get; private set; } = 0;
    public float axisY { get; private set; } = 0;
    public Vector3 axisResultDir { get; private set; } = Vector3.zero;


    private bool isJumping = false;

    public void InputProcess()
    {
        axisX = Input.GetAxisRaw("Horizontal");
        axisY = Input.GetAxisRaw("Vertical");
        axisResultDir = new Vector3(axisX, 0, axisY).normalized;

        if (Input.GetButtonDown("Jump"))
        {
            isJumping = true;
        }
    }

    public bool GetIsJumping()
    {
        return isJumping;
    }

    public void MakeIsJumpingFalse()
    {
        isJumping = false;
    }
}