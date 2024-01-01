using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GM : MonoBehaviour
{
    public float physicsSpeedMutlitplier = 1f;
    public Tilemap standardAStarTilemap, colliderTilemap;
    public TimerManager timerManager = new ();
    
    // Start is called before the first frame update
    void Start()
    {
        physicsSpeedMutlitplier = Mathf.Max(0f, physicsSpeedMutlitplier);
        Time.timeScale = physicsSpeedMutlitplier;
    }

    private void Update()
    {
        timerManager.UpdateTimers(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        timerManager.UpdateTimers(Time.fixedDeltaTime);
    }

    public static GM GetGM()
    {
        return GameObject.FindGameObjectWithTag("GM").GetComponent<GM>();
    }

    public static GameObject GetPlayer()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }

    public static Vector3 GetPlayerPosition()
    {
        return GetPlayer().transform.position;
    }

    public static PFManager GetPFManager()
    {
        return GameObject.FindGameObjectWithTag("GM").GetComponent<PFManager>();
    }
}
