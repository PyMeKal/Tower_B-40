// Mud hazard that slows down the player

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Mud : MonoBehaviour
{
    private PlayerMovement playerMovement;
    public float coeffSpeed;  // coeff that shows how slow 
    public float coeffAccel;  // Changed from snake_case to camelCase (C# naming convention)

    void Start()
    {
        playerMovement = GM.GetPlayer().GetComponent<PlayerMovement>();
    }
    
    private void OnCollisionEnter2D(Collision2D other) {
        // print("Mud");
        if (other.gameObject.name == "Player"){
            playerMovement.walkSpeed *= coeffSpeed;  // Switched coeff to apply multiplied speed on mud contact 
            playerMovement.moveAccel *= coeffAccel;
        }
    }

    private void OnCollisionExit2D(Collision2D other) {
        // print("exit");
        if (other.gameObject.name == "Player"){
            playerMovement.walkSpeed /= coeffSpeed;  // revertss
            playerMovement.moveAccel /= coeffAccel;
        }
    }
}
