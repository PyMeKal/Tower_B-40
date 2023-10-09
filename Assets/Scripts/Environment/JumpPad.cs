// JumpPad that makes the player jump without controlling

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class JumpPad : MonoBehaviour
{
    public GameObject player;
    public float coeff; // how much the player will jump

    // on collision, player will jump on contact
    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.name == "Player"){
            player.GetComponent<PlayerMovement>().jumpSpeed *= coeff;
            player.GetComponent<PlayerMovement>().Free_Jump();
            player.GetComponent<PlayerMovement>().jumpSpeed /= coeff;
        }
    }

}
