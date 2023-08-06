using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class MotherNature : MonoBehaviour
{
    // Simple evolution/natural selection testing script
    public List<Agent> agents = new List<Agent>();
    public float genocideClock;
    private float genocideClockTimer;
    [Range(0f, 100f)] public float genocidePercentage;

    void Start()
    {
        genocideClockTimer = genocideClock;
    }
    void Update()
    {
        genocideClockTimer -= Time.deltaTime;
        if (genocideClockTimer <= 0f)
        {
            genocideClockTimer = genocideClock;
            DayOfReckoning();
        }
    }

    private void DayOfReckoning()
    {
        List<Agent> agentsSorted = agents.OrderByDescending(a => a.reward).ToList();
        int survivorCount = Mathf.RoundToInt(agents.Count * (genocidePercentage/100f));
        List<Agent> survivors = agentsSorted.Where((a, c) => c + 1 <= survivorCount).ToList();
        // To be continued
    }
}
