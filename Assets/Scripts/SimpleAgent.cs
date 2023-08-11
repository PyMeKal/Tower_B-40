using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class SimpleAgent : MonoBehaviour
{
    public NeuralNetwork brain;
    public Agent agent;
    private AgentInterface agentInterface;
    private MotherNature motherNature;
    public float computeClock, baseSpeed;

    public float sensorDistance;
    
    public int bombCount;
    public GameObject bombObject;
    public float bombVelocityMultiplier;
    
    private float computeClockTimer, t, resA, resB;
    
    //Movement

    private Vector2 setVelocity;
    private Rigidbody2D rb;
    
    void Start()
    {
        agentInterface = GetComponent<AgentInterface>();
        if (!agentInterface.modelReceived)
        {
            // Create default model with randomized w&b if no model has been loaded onto the agent;
            brain = new NeuralNetwork(gameObject.name + "_brain");
            
            // Input:
            // 1. Closest agent relative position. (0, 0) if none in sight -> 2
            // 2. Number of agents in sight / 4 -> 1
            // 3. Sin(t) -> 1
            // 4. Residual A -> 1
            // 5. Residual B -> 1
            // (+) => 6
            brain.AddLayer(6, NeuralNetwork.ActivationFunction.Sigmoid); // for x y coords
            // --------------------
            brain.AddLayer(64, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(64, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            // --------------------
            // Output:
            // 1. v x
            // 2. v y
            // 3. 'Plant bomb' switch: plants bomb if >0.5f
            // 4. Residual A
            // 5. Residual B
            brain.AddLayer(5, NeuralNetwork.ActivationFunction.Tanh); // output -> velocity [-1, 1]
            brain.Compile();
        }
        else
        {
            // Apply received model
            brain = agentInterface.receivedModel;
            brain.Mutate(0.2f, 0.2f, 2f/32f);
            // print(brain.name);
        }

        agent = new Agent(gameObject, 0f, brain);
        
        computeClockTimer = computeClock;

        motherNature = GameObject.FindGameObjectWithTag("GM").GetComponent<MotherNature>();
        motherNature.agents.Add(agent);
    }
    
    void Update()
    {
        EvaluateReward();
        
        computeClockTimer -= Time.deltaTime;
        
        t += Time.deltaTime;
        if (computeClockTimer <= 0f)
        {
            computeClockTimer = computeClock;
            
            // Handle Inputs
            // Using OverlapCircleNonAlloc instad of OverlapCircleAlloc. Feat. GPT4
            int maxAgents = 100; // Just an example; set this to the maximum number of agents you expect
            Collider2D[] agents = new Collider2D[maxAgents];
            int numAgents = Physics2D.OverlapCircleNonAlloc(transform.position, sensorDistance, agents);

            // Now agents contains the colliders, and numAgents tells you how many there are.
            // If you want to work with a trimmed array, you can do this:
            // Array.Resize(ref agents, numAgents);

            float minDistSqr = 999f;
            Vector3 minDelta = Vector3.zero;

            if (agents.Length > 0)
            {
                for (int i = 0; i < numAgents; i++)
                {
                    Collider2D thisAgent = agents[i];
                    Vector3 delta = thisAgent.transform.position - transform.position;
                    float distSqr = (delta.x * delta.x) + (delta.y * delta.y);
                    if (minDistSqr > distSqr)
                    {
                        minDistSqr = distSqr;
                        minDelta = delta;
                    }
                }
            }
            
            // -------------------- COMPUTE
            float[] output = brain.Compute(new float[] {minDelta.x, minDelta.y, numAgents / 4f, Mathf.Sin(t), resA, resB});
            // --------------------
            
            // Process Output
            setVelocity = output.Contains(float.NaN) ? Vector3.zero : new Vector2(output[0], output[1]) * baseSpeed;
            if(output[2] > 0.5f)
                PlantBomb();

            resA = output[3];
            resB = output[4];
        }
        transform.Translate(setVelocity);
    }

    void EvaluateReward()
    {
        agent.reward = transform.position.x;
    }

    void PlantBomb()
    {
        if (bombCount <= 0)
            return;
        Instantiate(bombObject, transform.position, quaternion.identity);
        bombCount--;
    }

    public void TakeDamage(float damage)
    {
        
    }
}
