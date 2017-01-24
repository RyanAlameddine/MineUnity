using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    CharacterController cc;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }
    public float speed = 6.0F;
    public float jumpSpeed = 8.0F;
    public float gravity = 20.0F;
    private Vector3 moveDirection = Vector3.zero;
    bool isGrounded = false;

    void Update()
    {
        //Feed moveDirection with input.
        moveDirection = new Vector3(Input.GetAxis("Horizontal"), moveDirection.y, Input.GetAxis("Vertical"));
        moveDirection = transform.TransformDirection(moveDirection);
        //Multiply it by speed.
        moveDirection *= speed;
        moveDirection.y /= speed;
        // is the controller on the ground?
        if (cc.isGrounded)
        {
            //Jumping
            if (Input.GetButton("Jump"))
                moveDirection.y = jumpSpeed;

        }
        else
        {
            //Applying gravity to the controller
            moveDirection.y -= gravity * Time.deltaTime;
        }
        //Making the character move
        cc.Move(moveDirection * Time.deltaTime);
    }
}
