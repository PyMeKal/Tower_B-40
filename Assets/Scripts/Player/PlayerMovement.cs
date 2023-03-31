using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float walkSpeed;
    public float moveAccel, stopAccel, v0;
    private float walkAccelPow;
    public float jumpSpeed;
    public LayerMask groundLayers;

    public Transform spritesTransform;
    private Rigidbody2D rb;

    private PlayerAnimation playerAnimation;
    private delegate void DELVoid();
    private DELVoid flipSprite, move, jump;
    void Start()
    {
        //Scripts
        rb = GetComponent<Rigidbody2D>();
        playerAnimation = GetComponent<PlayerAnimation>();

        //Delegates
        flipSprite = FlipSpritesByMovement;
        move = Move;
        jump = Jump;

        //Initial variable values
        walkAccelPow = stopAccel;
    }

    void FixedUpdate()
    {
        move();
        flipSprite();
    }

    void Update()
    {
        jump();
    }

    Vector3 input, prevInput, deltaInput;
    void Move()
    {
        input = GetInputVector();
        deltaInput = input - prevInput;

        //----Walk----
        if (input.x != 0f && prevInput.x == 0f)
            walkAccelPow = moveAccel;
        if (input.x == 0f && prevInput.x != 0f)
            walkAccelPow = stopAccel;

        Vector3 moveVector;
        float direction = 1f;
        float vx = 0f;
        if(Input.GetAxisRaw("Horizontal") != 0f)
            direction = Input.GetAxisRaw("Horizontal");
        //print(direction);
        if (walkAccelPow == moveAccel)
        {
            vx = Mathf.Clamp(walkSpeed * Mathf.Pow(Input.GetAxis("Horizontal"), walkAccelPow) + direction * v0, -walkSpeed, walkSpeed);
            moveVector = new Vector3(vx, rb.velocity.y);
        }
        else
        {
            vx = walkSpeed * Mathf.Pow(Input.GetAxis("Horizontal"), walkAccelPow);
            moveVector = new Vector3(vx, rb.velocity.y);
        }

        rb.velocity = moveVector;
        playerAnimation.RequestAnimation<bool>("Walk", Input.GetAxisRaw("Horizontal") != 0 ? true : false);
        playerAnimation.RequestAnimation<bool>("Walk_play_Decel", Mathf.Abs(vx) >= walkSpeed * 0.8f ? true : false);
        prevInput = GetInputVector();

        Vector3 GetInputVector()
        {
            return new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxis("Vertical")); ;
        }
    }

    void Jump()
    {
        //----Jump----
        if (Input.GetKeyDown(KeyCode.W))
        {
            print("Jump");
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        }

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
