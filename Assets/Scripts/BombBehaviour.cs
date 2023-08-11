using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombBehaviour : MonoBehaviour
{
    public float timer;
    public float radius;
    public float damage;

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
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var col in colliders)
        {
            col.GetComponent<SimpleAgent>().TakeDamage(damage);
        }
        
        Destroy(gameObject);
    }
}
