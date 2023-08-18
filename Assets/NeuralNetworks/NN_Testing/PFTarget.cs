using System.Collections;
using System.Collections.Generic;
using NeuralNetworks.NN_Testing;
using UnityEngine;

public class PFTarget : MonoBehaviour
{
    public Vector2 tl, br;
    // Start is called before the first frame update
    void Start()
    {
        GameObject.FindGameObjectWithTag("GM").GetComponent<MotherNature>().purge += ResetRandomPosition;
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
}
