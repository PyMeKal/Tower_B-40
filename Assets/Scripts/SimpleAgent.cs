using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class SimpleAgent : MonoBehaviour
{
    public NeuralNetwork brain;
    public Agent agent;
    private AgentInterface agentInterface;
    private MotherNature motherNature;
    public bool enableEvolution;
    public float computeClock, baseSpeed;

    public float reward;

    public float sensorDistance;
    public LayerMask agentLayer, bombLayer;
    
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
            // 3. Closest bomb relative position -> 2
            // 4. Sin(t) -> 1
            // 5. BombCooldownTimer -> 1
            // 6. Residual A -> 1
            // 7. Residual B -> 1
            // (+) => 11
            brain.AddLayer(11, NeuralNetwork.ActivationFunction.Sigmoid); // for x y coords
            // --------------------
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(32, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.Sigmoid);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.Sigmoid);
            // --------------------
            // Output:
            // 1. v x
            // 2. v y
            // 3. 'Plant bomb' switch: plants bomb if >0.5f
            // 4. Bomb v x
            // 5. Bomb v y
            // 6. Residual A
            // 7. Residual B
            brain.AddLayer(7, NeuralNetwork.ActivationFunction.Tanh); // output -> velocity [-1, 1]
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
        if (enableEvolution)
            motherNature.agents.Add(agent);
        
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        string fullPath = brain.SaveModel(string.Empty);
        NeuralNetwork loadedModel = NeuralNetwork.LoadModel(fullPath);
        print(loadedModel.layers[1].biases[0]);
        print(loadedModel.layers[1].weights[0, 0]);
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
            // Using OverlapCircleNonAlloc instead of OverlapCircleAlloc. Feat. GPT4
            int maxAgents = 100; // Just an example; set this to the maximum number of agents you expect
            Collider2D[] agents = new Collider2D[maxAgents];
            int numAgents = Physics2D.OverlapCircleNonAlloc(position, sensorDistance, agents, agentLayer);

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
            
            int maxBombs = 50; // Just an example; set this to the maximum number of agents you expect
            Collider2D[] bombs = new Collider2D[maxBombs];
            int numBombs = Physics2D.OverlapCircleNonAlloc(position, sensorDistance, bombs, bombLayer);

            // Now agents contains the colliders, and numAgents tells you how many there are.
            // If you want to work with a trimmed array, you can do this:
            // Array.Resize(ref agents, numAgents);

            float minDistSqrBomb = 999f;
            Vector3 minDeltaBomb = Vector3.zero;

            if (bombs.Length > 0)
            {
                for (int i = 0; i < numBombs; i++)
                {
                    Collider2D thisBomb = bombs[i];
                    Vector3 delta = thisBomb.transform.position - position;
                    float distSqr = (delta.x * delta.x) + (delta.y * delta.y);
                    if (minDistSqrBomb > distSqr)
                    {
                        minDistSqrBomb = distSqr;
                        minDeltaBomb = delta;
                    }
                }
            }
            
            // -------------------- COMPUTE
            float[] output = brain.Compute(new float[] {position.x, position.y, minDelta.x,
                                                                minDelta.y, numAgents / 4f, minDeltaBomb.x, minDeltaBomb.y,
                                                                Mathf.Sin(t),
                                                                bombCooldownTimer, resA, resB});
            // --------------------
            
            // Process Output
            setVelocity = output.Contains(float.NaN) ? Vector3.zero : new Vector2(output[0], output[1]) * baseSpeed;
            if(output[2] > 0.5f)
                PlantBomb(new Vector2(output[3], output[4]));

            resA = output[5];
            resB = output[6];
        }
        rb.velocity = setVelocity;
    }

    void EvaluateReward()
    {
        agent.reward = reward;
    }

    void PlantBomb(Vector2 vel)
    {
        if (bombCount <= 0 || bombCooldownTimer > 0f)
            return;
        Rigidbody2D bombRb = Instantiate(bombObject, transform.position, quaternion.identity).GetComponent<Rigidbody2D>();
        bombRb.velocity = vel * bombVelocityMultiplier;
        BombBehaviour behaviour = bombRb.GetComponent<BombBehaviour>();
        behaviour.origin = this;
        behaviour.motherNature = motherNature;
        bombCount--;
        bombCooldownTimer = bombCooldown;

        reward += 0.5f;
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
