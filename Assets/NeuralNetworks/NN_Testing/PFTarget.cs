using System;
using System.Collections;
using System.Collections.Generic;
using NeuralNetworks.NN_Testing;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class PFTarget : MonoBehaviour
{
    public Vector2 tl, br;

    public float positionResetClock;

    private float positionResetClockTimer;
    // Start is called before the first frame update
    void Start()
    {
        GameObject.FindGameObjectWithTag("GM").GetComponent<MotherNature>().purge += ResetRandomPosition;
        GameObject.FindGameObjectWithTag("GM").GetComponent<MotherNature>().purge += IncreaseBox;
        ResetRandomPosition();
        positionResetClockTimer = positionResetClock;
    }

    void IncreaseBox()
    {
        tl += new Vector2(-0.05f, 0.05f);
        br += new Vector2(0.05f, -0.05f);

        tl = new Vector2(Mathf.Max(tl.x, -10f), Mathf.Min(tl.y, 22f));
        br = new Vector2(Mathf.Min(br.x, 10f), Mathf.Max(br.y, 5));
    }

    Vector2 GetRandomPosition()
    {
        return new Vector2(Random.Range(tl.x, br.x), Random.Range(br.y, tl.y));
    }
    
    void ResetRandomPosition()
    {
        transform.position = GetRandomPosition();
        while (Physics2D.OverlapCircle(transform.position, 1.2f))
        {
            transform.position = GetRandomPosition();
        }
    }

    private void FixedUpdate()
    {
        positionResetClockTimer -= Time.fixedDeltaTime;
        if (positionResetClockTimer <= 0f)
        {
            positionResetClockTimer = positionResetClock;
            ResetRandomPosition();
        }
    }
}
