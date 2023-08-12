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

    public float reward;

    public float sensorDistance;
    
    public int bombCount;
    public GameObject bombObject;
    public float bombVelocityMultiplier;
    public float bombCooldown;
    private float bombCooldownTimer;
    
    private float computeClockTimer, t, resA, resB;

    private Vector2 setVelocity;
    private Rigidbody2D rb;
    private Animator anim;
    
    void Start()
    {
        agentInterface = GetComponent<AgentInterface>();
        if (!agentInterface.modelReceived)
        {
            // Create default model with randomized w&b if no model has been loaded onto the agent;
            brain = new NeuralNetwork(gameObject.name + "_brain");
            
            // Input:
            // 0. transform.position -> 2
            // 1. Closest agent relative position. (0, 0) if none in sight -> 2
            // 2. Number of agents in sight / 4 -> 1
            // 3. Sin(t) -> 1
            // 4. BombCooldownTimer;
            // 5. Residual A -> 1
            // 6. Residual B -> 1
            // (+) => 9
            brain.AddLayer(9, NeuralNetwork.ActivationFunction.Sigmoid); // for x y coords
            // --------------------
            brain.AddLayer(64, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            // --------------------
            // Output:
            // 1. v x
            // 2. v y
            // 3. 'Plant bomb' switch: plants bomb if >0.5f
            // 4. Bomb timer -> x4
            // 5. Residual A
            // 6. Residual B
            brain.AddLayer(6, NeuralNetwork.ActivationFunction.Tanh); // output -> velocity [-1, 1]
            brain.Compile();
        }
        else
        {
            // Apply received model
            brain = agentInterface.receivedModel;
            brain.Mutate(0.2f, 0.2f, 1f/64f);
            // print(brain.name);
        }

        agent = new Agent(gameObject, 0f, brain);
        
        computeClockTimer = computeClock;
        bombCooldownTimer = bombCooldown;

        motherNature = GameObject.FindGameObjectWithTag("GM").GetComponent<MotherNature>();
        motherNature.agents.Add(agent);

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }
    
    void Update()
    {
        EvaluateReward();
        
        computeClockTimer -= Time.deltaTime;
        bombCooldownTimer -= Time.deltaTime;
        
        t += Time.deltaTime;
        if (computeClockTimer <= 0f)
        {
            computeClockTimer = computeClock;
            var position = transform.position;
            
            // Handle Inputs
            // Using OverlapCircleNonAlloc instad of OverlapCircleAlloc. Feat. GPT4
            int maxAgents = 100; // Just an example; set this to the maximum number of agents you expect
            Collider2D[] agents = new Collider2D[maxAgents];
            int numAgents = Physics2D.OverlapCircleNonAlloc(position, sensorDistance, agents);

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
                    Vector3 delta = thisAgent.transform.position - position;
                    float distSqr = (delta.x * delta.x) + (delta.y * delta.y);
                    if (minDistSqr > distSqr)
                    {
                        minDistSqr = distSqr;
                        minDelta = delta;
                    }
                }
            }
            
            // -------------------- COMPUTE
            float[] output = brain.Compute(new float[] {position.x, position.y, minDelta.x,
                                                                minDelta.y, numAgents / 4f, Mathf.Sin(t),
                                                                bombCooldownTimer, resA, resB});
            // --------------------
            
            // Process Output
            setVelocity = output.Contains(float.NaN) ? Vector3.zero : new Vector2(output[0], output[1]) * baseSpeed;
            if(output[2] > 0.5f)
                PlantBomb(output[3] * 4f);

            resA = output[4];
            resB = output[5];
        }
        rb.velocity = setVelocity;
    }

    void EvaluateReward()
    {
        agent.reward = reward;
    }

    void PlantBomb(float timer)
    {
        if (bombCount <= 0 || bombCooldownTimer > 0f)
            return;
        Rigidbody2D bombRb = Instantiate(bombObject, transform.position, quaternion.identity).GetComponent<Rigidbody2D>();
        bombRb.velocity = rb.velocity * bombVelocityMultiplier;
        BombBehaviour behaviour = bombRb.GetComponent<BombBehaviour>();
        behaviour.origin = this;
        behaviour.timer = timer;
        behaviour.motherNature = motherNature;
        bombCount--;
        bombCooldownTimer = bombCooldown;
    }

    public void TakeDamage(float damage)
    {
        reward -= damage;
        anim.SetTrigger("TakeDamage");
    }

    public void DamageInflicted(float totalDamage)
    {
        reward += totalDamage;
    }
}
