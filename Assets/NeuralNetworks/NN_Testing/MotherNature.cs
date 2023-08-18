using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeuralNetworks.NN_Testing
{
    public class MotherNature : MonoBehaviour
    {
        // Simple evolution/natural selection testing script
        public List<Agent> agents = new List<Agent>();
        public GameObject agentPrefab;
        public EvolutionUI evolutionUI;
        public int generation;
        public float genocideClock;
        [HideInInspector] public float genocideClockTimer;
        public int poolDivider;  // Formerly offspringCount
        public int preserveCount;
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
            int survivorCount = agents.Count/poolDivider;

            evolutionUI.bestReward = agentsSorted[0].reward;
            evolutionUI.agentCount = agents.Count;

            List<Agent> survivors = agentsSorted.Where((a, c) => c + 1 <= survivorCount).ToList();
            List<Agent> killList = agentsSorted.Where((a, c) => c + 1 > survivorCount).ToList();
            for (int i = 0; i < killList.Count; i++)
            {
                Destroy(killList[i].agentObj);
            }

            if (saveBestModel)
            {
                survivors[0].brain.name = "Fittest";
                survivors[0].brain.SaveModel();
            }

            for (int i = 0; i < preserveCount; i++)
            {
                Vector3 spawnPoint = new Vector3(Random.Range(-spawnArea.x, spawnArea.x),
                    Random.Range(-spawnArea.y, spawnArea.y));
                GameObject offspring = Instantiate(agentPrefab, spawnPoint, Quaternion.identity);
                AgentInterface agentInterface = offspring.GetComponent<AgentInterface>();
                NeuralNetwork deepCopy = survivors[i].brain.DeepCopy(survivors[i].brain.name);
                agentInterface.receivedModel = deepCopy;
                agentInterface.modelReceived = true;
                agentInterface.mutate = false;
            }
        
            // Going for an improved selection/reproduction method, where the chance of reproducing is proportional to
            // the agent's reward. Stolen from here: https://youtu.be/q_PtNIEDVnE (Pezzza's Work)
            List<float> rewards = survivors.Select(a => a.reward).ToList();
            if (rewards.Min() < 0f)
                rewards = rewards.Select(r => r - rewards.Min()).ToList();
            
            // Adaptive Mutation Rates: Variable mutation scale & reshuffle chance based on reward
            // -> less reward, more mutation
            float[] mutationScales = new float[survivorCount];
            float[] reshuffleChanceScales = new float[survivorCount];
            float minMS = 1.0f, maxMS = 1.5f, minRCS = 0.5f, maxRCS = 1.5f;  // min is exclusive
            for (int i = 0; i < survivorCount; i++)
            {
                mutationScales[i] = minMS + (maxMS - minMS) * ((i + 1) / (float)survivorCount);
                reshuffleChanceScales[i] = minRCS + (maxRCS - minRCS) * ((i + 1) / (float)survivorCount);
            }
            
            for (int i = 0; i < agents.Count - preserveCount; i++)
            {
                float dice = Random.Range(0f, rewards.Sum());
                // print(dice);
                float accumulatedReward = rewards[0];
                int index = 0;
                while (accumulatedReward < dice && index < rewards.Count - 1)
                {
                    index++;
                    accumulatedReward += rewards[index];
                }
            
                Vector3 spawnPoint = new Vector3(Random.Range(-spawnArea.x, spawnArea.x),
                    Random.Range(-spawnArea.y, spawnArea.y));
                GameObject offspring = Instantiate(agentPrefab, spawnPoint, Quaternion.identity);
                AgentInterface agentInterface = offspring.GetComponent<AgentInterface>();
                NeuralNetwork deepCopy = survivors[index].brain.DeepCopy(survivors[index].brain.name);
                agentInterface.receivedModel = deepCopy;
                agentInterface.modelReceived = true;
                agentInterface.mutate = true;
                agentInterface.mutationScale = mutationScales[index];
                agentInterface.reshuffleChanceScale = reshuffleChanceScales[index];
            }

            foreach (var survivor in survivors)
            {
                Destroy(survivor.agentObj);
            }
        
            /*
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
            // 1. Preserved agents (Fittest among them)
            /*
            if (i < preserveCount)
            {
                GameObject[] offspring = new GameObject[poolDivider];
                for (int j = 0; j < poolDivider; j++)
                {
                    Vector3 spawnPoint = new Vector3(Random.Range(-spawnArea.x, spawnArea.x),
                        Random.Range(-spawnArea.y, spawnArea.y));
                    offspring[j] = Instantiate(agentPrefab, spawnPoint, Quaternion.identity);
                    AgentInterface agentInterface = offspring[j].GetComponent<AgentInterface>();
                    NeuralNetwork deepCopy = survivors[i].brain.DeepCopy(survivors[i].brain.name);
                    agentInterface.receivedModel = deepCopy;
                    if(j==0)
                        agentInterface.mutate = false;
                    else
                        agentInterface.mutate = true;
                    agentInterface.modelReceived = true;
                }
            }
            else
            {
                GameObject[] offspring = new GameObject[poolDivider];
                for (int j = 0; j < poolDivider; j++)
                {
                    Vector3 spawnPoint = new Vector3(Random.Range(-spawnArea.x, spawnArea.x),
                        Random.Range(-spawnArea.y, spawnArea.y));
                    offspring[j] = Instantiate(agentPrefab, spawnPoint, Quaternion.identity);
                    AgentInterface agentInterface = offspring[j].GetComponent<AgentInterface>();
                    NeuralNetwork deepCopy = survivors[i].brain.DeepCopy(survivors[i].brain.name);
                    agentInterface.receivedModel = deepCopy;
                    agentInterface.modelReceived = true;
                    agentInterface.mutate = true;
                }
            }
            Destroy(survivors[i].agentObj);
        }
        */
            agents = new List<Agent>();
            generation++;
            evolutionUI.generation = generation;
        }
    }
}
