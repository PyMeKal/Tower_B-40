using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class JumpPad : MonoBehaviour
{

    
    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.name == "Player"){
            Debug.Log("HI");
        }
    }

    void Jump(){
    }

    void Start(){
    }

}
