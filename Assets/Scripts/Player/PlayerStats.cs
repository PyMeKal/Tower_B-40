using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerAnimation playerAnimation;
    private PlayerWingsBehaviour playerWingsBehaviour;
    
    
    public enum debuffs
    {
        slowed,
    }
    
    public float maxHealth, health;

    public Material defaultMat, damagedMat;

    public List<debuffs> appliedDebuffs = new();
    public Dictionary<debuffs, IPlayerDebuff> debuffsDict;
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health < 0f) health = 0f;
        
        playerAnimation.RequestAnimation("TakeDamage", "Trigger");
        
        if (health <= 0f)
        {
            // Die
            Die();
        }
    }

    public void Die()
    {
        // Temporary
        playerMovement.enabled = false;
        playerAnimation.enabled = false;
        playerWingsBehaviour.enabled = false;
    }

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();
        playerWingsBehaviour = GetComponent<PlayerWingsBehaviour>();

        debuffsDict = new Dictionary<debuffs, IPlayerDebuff>();
        debuffsDict[debuffs.slowed] = new Slowed(this);
    }

    public void Update()
    {
        Action updateAppliedDebuffs = () => { };
        foreach (var debuff in appliedDebuffs)
        {
            debuffsDict[debuff].Update();
            if (debuffsDict[debuff].GetDurationTimer() <= 0f)
                updateAppliedDebuffs += debuffsDict[debuff].Exit;
        }

        updateAppliedDebuffs();
    }

    public void ApplyDebuff(debuffs debuff, float duration)
    {
        debuffsDict[debuff].Enter(duration);
        appliedDebuffs.Add(debuff);
    }
    
    public interface IPlayerDebuff
    {
        public void Enter(float duration);
        public void Update();
        public float GetDurationTimer();
        public void Exit();
    }

    public class Slowed : IPlayerDebuff
    {
        private PlayerStats stats;
        private float defaultSpeed;
        private float slowedSpeed;

        private float timer;

        public Slowed(PlayerStats stats)
        {
            this.stats = stats;
        }

        public void Enter(float duration)
        {
            defaultSpeed = stats.playerMovement.walkSpeed;
            stats.playerMovement.walkSpeed = defaultSpeed * 0.5f;
            timer = duration;
        }

        public void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
            }
        }

        public float GetDurationTimer()
        {
            return timer;
        }

        public void Exit()
        {
            stats.playerMovement.walkSpeed = defaultSpeed;
            stats.appliedDebuffs.Remove(debuffs.slowed);
            timer = 0f;
        }
    }
}
