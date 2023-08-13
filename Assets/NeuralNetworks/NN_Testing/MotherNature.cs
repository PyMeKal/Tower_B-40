using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
public class MotherNature : MonoBehaviour
{
    // Simple evolution/natural selection testing script
    public List<Agent> agents = new List<Agent>();
    public GameObject agentPrefab;
    public EvolutionUI evolutionUI;
    public int generation;
    public float genocideClock;
    private float genocideClockTimer;
    public int offspringCount;
    public bool saveBestModel;
    
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

        evolutionUI.clock = Mathf.Round(genocideClockTimer*10f)/10f;
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

        evolutionUI.bestReward = agentsSorted[0].reward;
        evolutionUI.agentCount = agents.Count;
        
        List<Agent> survivors = agentsSorted.Where((a, c) => c + 1 <= survivorCount).ToList();
        List<Agent> killList = agentsSorted.Where((a, c) => c + 1 > survivorCount).ToList();
        for (int i = 0; i < killList.Count; i++)
        {
            Destroy(killList[i].agentObj);
        }

        survivors[0].brain.name = "Fittest";
        survivors[0].brain.SaveModel();
        
        for (int i = 0; i < survivors.Count; i++)
        {
            GameObject[] offspring = new GameObject[offspringCount];
            for (int j = 0; j < offspringCount; j++)
            {
                Vector3 spawnPoint = new Vector3(Random.Range(-spawnArea.x, spawnArea.x),
                    Random.Range(-spawnArea.y, spawnArea.y));
                offspring[j] = Instantiate(agentPrefab, spawnPoint, Quaternion.identity);
                AgentInterface agentInterface = offspring[j].GetComponent<AgentInterface>();
                agentInterface.receivedModel = survivors[i].brain.DeepCopy(survivors[i].brain.name);
                agentInterface.modelReceived = true;
            }
            Destroy(survivors[i].agentObj);
        }

        agents = new List<Agent>();
        generation++;
        evolutionUI.generation = generation;
    }
}
