// Mud hazard that slows down the player

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Mud : MonoBehaviour
{
    public GameObject player;
    public float coeff_speed; // coeff that shows how slow 
    public float coeff_accel;

    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.name == "Player"){
            player.GetComponent<PlayerMovement>().walkSpeed /= coeff_speed;
            player.GetComponent<PlayerMovement>().moveAccel /= coeff_accel;
        }
    }

    private void OnCollisionExit2D(Collision2D other) {
        if (other.gameObject.name == "Player"){
            player.GetComponent<PlayerMovement>().walkSpeed *= coeff_speed;
            player.GetComponent<PlayerMovement>().moveAccel *= coeff_accel;
        }
    }
}
