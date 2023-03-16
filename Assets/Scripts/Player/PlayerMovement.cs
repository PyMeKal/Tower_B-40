using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    
    public float walkSpeed;
    public float jumpForce;
    public LayerMask groundLayers;

    public Transform spritesTransform;
    private Rigidbody2D rb;

    private PlayerAnimation playerAnimation;
    private delegate void DELVoid();
    private DELVoid flipSprite, move;
    void Start()
    {
        //Scripts
        rb = GetComponent<Rigidbody2D>();
        playerAnimation = GetComponent<PlayerAnimation>();


        flipSprite = FlipSpritesByMovement;
        move = Move;
    }

    void FixedUpdate()
    {
        move();
        flipSprite();
    }

    void Move()
    {
        Vector2 inputVector = new Vector3(Input.GetAxisRaw("Horizontal"), rb.velocity.y);
        rb.velocity = walkSpeed * inputVector;
        playerAnimation.RequestAnimation<bool>("Walk", inputVector.magnitude > 0 ? true : false);
    }
    void FlipSpritesByMovement()
    {
        if (Input.GetAxisRaw("Horizontal") > 0)
        {
            spritesTransform.localEulerAngles = new Vector3(0, 0, 0);
        }

        if (Input.GetAxisRaw("Horizontal") < 0)
        {
            spritesTransform.localEulerAngles = new Vector3(0, 180f, 0);
        }
    }
}
