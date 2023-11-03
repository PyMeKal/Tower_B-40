using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobStats
{
    public string id;
    public float health;

    public MobStats(string id, float health)
    {
        this.id = id;
        this.health = health;
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
    }
}

public class MobStatsInterface : MonoBehaviour
{
    public MobStats stats;
}
