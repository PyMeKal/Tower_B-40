// Button that does something hit

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
public class MovingPlat : MonoBehaviour
{

    // velocity when moving in x axis, positive when going right, negative when going left
    public float x_vel;

    // velocity when moving in y axis, positive when going up, negative when going down
    public float y_vel;
    
    // amount of how much the platform will go, should be positive
    public int x_length;
    public int y_length;

    // whether it loops or not
    public bool loop;

    // whether if player is needed to start moving
    public bool player;

    // trigger that starts the moving
    public bool trig;

    // if loops, how long is the stop time
    public float stop_time;

    private float pos_x;
    private float pos_y;
    private bool x_moving = false;
    
    private bool stop = false;

    private Rigidbody2D rb;

    //bool gotHit = false;

    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        pos_x = transform.position.x;
        pos_y = transform.position.y;

        if (x_vel != 0){
            x_moving = true;
        }

        // 플레이어를 플랫폼의 자식으로 설정
        if (transform.childCount > 0)
        {
            Transform player = transform.GetChild(0);
            player.SetParent(null); // 부모 해제
        }
    }

    void Update(){

        // update moving
        if (trig){
            Move();
            Stop_Plat();
        }
    }


    void FixedUpdate() {
        // making the player follow the platform
        if (x_moving && transform.childCount > 0)
        {
            Transform player = transform.GetChild(0);
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            playerRb.velocity = new Vector2(playerRb.velocity.x + x_vel, playerRb.velocity.y);
        }
        if (loop && stop){
            Invoke("Loop", 2f);
            stop = false;
        }
    }

    // function of moving
    private void Move(){
        rb.velocity = new Vector2(x_vel, y_vel); 
    }
    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.CompareTag("Player")){
            if (player){
                trig = true;
            }
            other.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D other) {
        if (other.gameObject.CompareTag("Player"))
        {
            other.transform.SetParent(null);
        }
    }

    private void Stop_Plat(){
        // stop
        if (x_vel >= 0 && transform.position.x >= pos_x + x_length){
            rb.velocity = new Vector2(0, rb.velocity.y);
            x_moving = false;
            stop = true;
        }
        else if (x_vel < 0 && transform.position.x <= pos_x - x_length){
            rb.velocity = new Vector2(0, rb.velocity.y);
            x_moving = false;
            stop = true;
        }
        if (y_vel >= 0 && transform.position.y >= pos_y + y_length){
            rb.velocity = new Vector2(rb.velocity.x, 0);
            stop = true;
        }
        else if (y_vel < 0 && transform.position.y <= pos_y - y_length){
            rb.velocity = new Vector2(rb.velocity.x, 0);
            stop = true;
        }
    }

    private void Loop(){

        if (x_vel != 0){
            x_moving = true;
        }

        x_vel = -x_vel;
        y_vel = -y_vel;

        stop = false;
    }
}
