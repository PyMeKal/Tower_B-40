using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ball : MonoBehaviour
{
    public float jumpSpeed;
    private Rigidbody2D rb;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector2(0f, jumpSpeed);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            rb.gravityScale *= -1f;
            GetComponent<SpriteRenderer>().color = Color.black;
        }
    }
}
