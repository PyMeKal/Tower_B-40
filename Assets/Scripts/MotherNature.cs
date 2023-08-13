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
    
    public Vector2 spawnArea;  // Center origin

    public delegate void DelNonArg();

    public DelNonArg purge;

    void Start()
    {
        genocideClockTimer = genocideClock;
        purge += EmptyFunc;
    }
    void Update()
    {
        if (agents.Count == 0)
            return;
        
        genocideClockTimer -= Time.deltaTime;
        if (genocideClockTimer <= 0f)
        {
            genocideClockTimer = genocideClock;
            foreach (var agent in agents)
            {
                break;
                //(agent.brain.layers[1].biases[0]);
            }
            DayOfReckoning();
        }
    }

    void EmptyFunc()
    {
        return;
    }
    
    private void DayOfReckoning()
    {
        purge();
        
        List<Agent> agentsSorted = agents.OrderByDescending(a => a.reward).ToList();
        int survivorCount = agents.Count/offspringCount;
        List<Agent> survivors = agentsSorted.Where((a, c) => c + 1 <= survivorCount).ToList();
        List<Agent> killList = agentsSorted.Where((a, c) => c + 1 > survivorCount).ToList();
        for (int i = 0; i < killList.Count; i++)
        {
            Destroy(killList[i].agentObj);
        }

        for (int i = 0; i < survivors.Count; i++)
        {
            GameObject[] offspring = new GameObject[offspringCount];
            for (int j = 0; j < offspringCount; j++)
            {
                Vector3 spawnPoint = new Vector3(Random.Range(-spawnArea.x, spawnArea.x),
                    Random.Range(-spawnArea.y, spawnArea.y));
                offspring[j] = Instantiate(agentPrefab, spawnPoint, Quaternion.identity);
                AgentInterface agentInterface = offspring[j].GetComponent<AgentInterface>();
                agentInterface.receivedModel = survivors[i].brain.DeepCopy(i + ", " + j);
                agentInterface.modelReceived = true;
            }
            Destroy(survivors[i].agentObj);
        }

        agents = new List<Agent>();
        generation++;
    }
}
