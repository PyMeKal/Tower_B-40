using System;
using System.Collections;
using System.Collections.Generic;
using NeuralNetworks.NN_Testing;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class BombBehaviour : MonoBehaviour
{
    public float timer;
    public float radius;
    public float damage;
    public SimpleAgent origin;
    
    [SerializeField] private LayerMask agentLayer;

    [HideInInspector] public MotherNature motherNature;

    void DeleteBomb()
    {
        motherNature.purge -= DeleteBomb;
        Destroy(gameObject);
    }
    
    private void Start()
    {
        motherNature.purge += DeleteBomb;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Detonate();
        }
    }

    private void Detonate()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius, agentLayer);
        float totalDamage = 0f;
        foreach (var col in colliders)
        {
            if(col.gameObject == origin.gameObject)
                col.GetComponent<SimpleAgent>().TakeDamage(damage * 2f);
            else
                col.GetComponent<SimpleAgent>().TakeDamage(damage);
            totalDamage += damage;
        }
        origin.DamageInflicted(totalDamage);
        motherNature.purge -= DeleteBomb;
        Destroy(gameObject);
    }
}
