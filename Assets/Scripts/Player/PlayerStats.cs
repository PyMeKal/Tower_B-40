using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float maxHealth, health;

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health < 0f) health = 0f;

        if (health <= 0f)
        {
            // Die
            Die();
        }
    }

    public void Die()
    {
        // Temporary
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerAnimation>().enabled = false;
        GetComponent<PlayerWingsBehaviour>().enabled = false;
    }
}
