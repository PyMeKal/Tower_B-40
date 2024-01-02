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
    [SerializeField] private Transform feetPosition;

    [HideInInspector] public bool facingRight;

    public SpriteRenderer sprite;
    private Rigidbody2D rb;

    private PlayerAnimation playerAnimation;
    private delegate void DELVoid();
    private DELVoid flipSprite, move, jump;
    private bool onGround, prevOnGround;
    private float onGroundTimer;

    [Header("Dash settings")]
    [SerializeField] private float dashDistance, dashCooldown;
    private float dashCooldownTimer;
    [SerializeField] private float dashYSnap, upDashSpeed;
    private bool useInputVelocity;
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
        HandleUseInputX();
        
        jump();
        Dash();
    }

    void HandleUseInputX()
    {
        // Used to determine whether to use x velocity from Input.GetAxis or use its current rb velocity (from dash)
        
        onGround = !(Physics2D.OverlapCircle(feetPosition.position, 0.5f, groundLayers) == null);
        if (onGround && !prevOnGround || onGroundTimer >= 0.5f)
            useInputVelocity = true;
        prevOnGround = onGround;
        if (onGround)
            onGroundTimer += Time.deltaTime;
    }

    // Local Variables for Move()
    float prevInput;
    float direction = 1f;
    bool usingMoveAccel;
    void Move()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        
        if (inputX != 0f) useInputVelocity = true;
        
        if (inputX != 0f && prevInput == 0f)
        {
            walkAccelPow = moveAccel;
            usingMoveAccel = true;
        }

        if (inputX == 0f && prevInput != 0f)
        {
            walkAccelPow = stopAccel;
            usingMoveAccel = false;
        }

        float vx = 0f;
        
        if (inputX != 0f)
            direction = inputX;
        
        if (usingMoveAccel && useInputVelocity)
        {
            vx = Mathf.Clamp(direction * walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) + direction * v0, -walkSpeed, walkSpeed);
            rb.velocity = new Vector3(vx, rb.velocity.y);
        }
        else if (useInputVelocity)
        {
            vx = walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) * direction;
            rb.velocity = new Vector3(vx, rb.velocity.y);
        }
        RequestAnimation("Walk", inputX != 0);
        RequestAnimation("Walk_play_Decel", Mathf.Abs(vx) >= walkSpeed * 0.8f);
        prevInput = inputX;
    }

    private void Jump()
    {
        RequestAnimation("On_Ground", onGround);

        if (Input.GetKeyDown(KeyCode.W) && onGround)
        {
            //print("Jump");
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            RequestAnimation("Jump", string.Empty);
        }

    }

    // jump without using w key
    public void Free_Jump(){
        if (onGround){
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            RequestAnimation("Jump", string.Empty);
        }
    }

    void FlipSpritesByMovement()
    {
        if(Input.GetAxisRaw("Horizontal") != 0)
            sprite.flipX = Input.GetAxisRaw("Horizontal") < 0;
        facingRight = !sprite.flipX;
    }

    void Dash()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var position = transform.position;
        Vector3 targetDashPos;
        
        if (mousePos.y > position.y - dashYSnap && mousePos.y < position.y + dashYSnap)
        {
            float dashDirectionX = mousePos.x > transform.position.x ? 1f : -1f;
            float d = dashDistance;
            targetDashPos = position + new Vector3(dashDirectionX, 0) * d;
        }
        else
        {
            float d = Vector3.Distance(position, mousePos) >= dashDistance ? dashDistance : Vector3.Distance(position, mousePos);
            
            targetDashPos = position + Vector3.Normalize(mousePos - position) * d;
        }
        if (Input.GetKeyDown(KeyCode.F) && dashCooldownTimer <= 0f)
        {
            rb.MovePosition(targetDashPos);
            dashCooldownTimer = dashCooldown;
            playerAnimation.RequestAnimation("Dash", "Trigger");
            
            sprite.flipX = position.x > mousePos.x;
            facingRight = !sprite.flipX;
            rb.velocity = (mousePos - position).normalized * upDashSpeed;
            useInputVelocity = false;
            onGroundTimer = 0f;
        }
        else
            dashCooldownTimer -= Time.deltaTime;
    }

    private void RequestAnimation<T>(string param, T value)
    {
        if (playerAnimation == null)
            return;
        playerAnimation.RequestAnimation(param, value);
    }
}
