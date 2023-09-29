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

    public Transform spritesTransform;
    private Rigidbody2D rb;

    private PlayerAnimation playerAnimation;
    private delegate void DELVoid();
    private DELVoid flipSprite, move, jump;

    [Header("Dash settings")] 
    [SerializeField] private float dashDistance;
    [SerializeField] private float dashYSnap, upDashSpeed;
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
        Dash();
    }
    void Empty()
    {return;}

    // Local Variables for Move()
    Vector3 input, prevInput, deltaInput;
    float direction = 1f;
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

        float vx;
        if (Input.GetAxisRaw("Horizontal") != 0f)
            direction = Input.GetAxisRaw("Horizontal");
        //print(direction);
        if (walkAccelPow == moveAccel)
        {
            vx = Mathf.Clamp(direction * walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) + direction * v0, -walkSpeed, walkSpeed);
            moveVector = new Vector3(vx, rb.velocity.y);
        }
        else
        {
            vx = walkSpeed * Mathf.Pow(Mathf.Abs(Input.GetAxis("Horizontal")), walkAccelPow) * direction;
            moveVector = new Vector3(vx, rb.velocity.y);
        }

        rb.velocity = moveVector;
        RequestAnimation<bool>("Walk", Input.GetAxisRaw("Horizontal") != 0 ? true : false);
        RequestAnimation<bool>("Walk_play_Decel", Mathf.Abs(vx) >= walkSpeed * 0.8f ? true : false);
        prevInput = GetInputVector();

        Vector3 GetInputVector()
        {
            return new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxis("Vertical")); ;
        }
    }

    void Jump()
    {
        bool onGround = !(Physics2D.OverlapCircle(feetPosition.position, 0.5f, groundLayers) == null);
        RequestAnimation<bool>("On_Ground", onGround);

        if (Input.GetKeyDown(KeyCode.W) && onGround)
        {
            //print("Jump");
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            RequestAnimation<string>("Jump", string.Empty);
        }

    }

    void FlipSpritesByMovement()
    {
        if (Input.GetAxisRaw("Horizontal") > 0)
        {
            spritesTransform.localEulerAngles = new Vector3(0, 0, 0);
            facingRight = true;
        }

        if (Input.GetAxisRaw("Horizontal") < 0)
        {
            spritesTransform.localEulerAngles = new Vector3(0, 180f, 0);
            facingRight = false;
        }
    }

    void Dash()
    {
        DELVoid dash;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePos.y > transform.position.y - dashYSnap && mousePos.y < transform.position.y + dashYSnap)
        {
            dash = DashHorizontal;
        }
        else if (mousePos.y >= transform.position.y + dashYSnap)
        {
            dash = DashUp;
        }
        else
        {
            dash = Phase;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            dash();
        }

        void DashHorizontal()
        {
            float direction = mousePos.x > transform.position.x ? 1f : -1f;
            float d = dashDistance;
            rb.MovePosition(transform.position + new Vector3(d*direction, 0));
        }
        void DashUp()
        {
            float d = Vector3.Distance(transform.position, mousePos) >= dashDistance ? dashDistance : Vector3.Distance(transform.position, mousePos);

            rb.MovePosition(transform.position + Vector3.Normalize(mousePos - transform.position)*d);
            rb.velocity = Vector3.Normalize(mousePos - transform.position)*upDashSpeed;

            move = Empty;
        }
        if (Input.GetAxisRaw("Horizontal") != 0)
            move = Move;

        void Phase()
        {

        }
    }

    private void RequestAnimation<T>(string param, T value)
    {
        if (playerAnimation == null)
            return;
        playerAnimation.RequestAnimation(param, value);
    }
}
