using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    private AudioManager audioManager;
    private PlayerMovement playerMovement;
    public AudioClip walkSFX;
    
    void Start()
    {
        audioManager = GM.GetAudioManager();
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void PlayWalkSFX()
    {
        audioManager.Request(walkSFX,
             () => transform.position,
             () => (Input.GetAxisRaw("Horizontal") ==0 || !playerMovement.OnGround),
             volume:0.7f, reverb:0f, loop:true, priority: 100);
    }

}
