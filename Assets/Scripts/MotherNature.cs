using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class MotherNature : MonoBehaviour
{
    // Simple evolution/natural selection testing script
    public List<Agent> agents = new List<Agent>();
    public GameObject agentPrefab;
    public int generation;
    public float genocideClock;
    private float genocideClockTimer;
    public int offspringCount;

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
            foreach (var agent in agents)
            {
                print(agent.brain.layers[1].biases[0]);
            }
            DayOfReckoning();
        }
    }

    private void DayOfReckoning()
    {
        List<Agent> agentsSorted = agents.OrderByDescending(a => a.reward).ToList();
        int survivorCount = agents.Count/offspringCount;
        List<Agent> survivors = agentsSorted.Where((a, c) => c + 1 <= survivorCount).ToList();
        List<Agent> killList = agentsSorted.Where((a, c) => c + 1 > survivorCount).ToList();
        for (int i = 0; i < killList.Count; i++)
        {
            Destroy(killList[i].agentObj);
        }

        List<GameObject> newGeneration = new List<GameObject>();
        for (int i = 0; i < survivors.Count; i++)
        {
            GameObject[] offspring = new GameObject[offspringCount];
            for (int j = 0; j < offspringCount; j++)
            {
                offspring[j] = Instantiate(agentPrefab, Vector3.zero, Quaternion.identity);
                AgentInterface agentInterface = offspring[j].GetComponent<AgentInterface>();
                agentInterface.receivedModel = survivors[i].brain.DeepCopy("Billy");
                agentInterface.modelReceived = true;
            }
            Destroy(survivors[i].agentObj);
        }

        agents = new List<Agent>();
        generation++;
    }
}
