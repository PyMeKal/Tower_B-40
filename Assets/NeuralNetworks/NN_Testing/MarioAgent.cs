using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MarioAgent : MonoBehaviour
{
    public NeuralNetwork brain;
    public Agent agent;
    public bool enableEvolution=true;
    private AgentInterface agentInterface;
    public float baseSpeed;
    public float jumpVelocity;
    public LayerMask groundLayer;
    public float computeClock;
    private float computeClockTimer;
    public float reward;
    

    private Rigidbody2D rb;
    private MotherNature motherNature;

    public float sensorRange;
    public int rayCount;
    public int residualConnectionCount;
    private float[] residual;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        computeClockTimer = computeClock;
        residual = new float[residualConnectionCount];
        
        agent = new Agent(gameObject, 0f, brain);
        
        agentInterface = GetComponent<AgentInterface>();
        if (!agentInterface.modelReceived)
        {
            // Create default model with randomized w&b if no model has been loaded onto the agent;
            brain = new NeuralNetwork(gameObject.name + "_brain");
            
            // Input:
            // Sensor rays, xy pos, residual
            brain.AddLayer(rayCount + 2 + residualConnectionCount, NeuralNetwork.ActivationFunction.Linear);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(2 + residualConnectionCount, NeuralNetwork.ActivationFunction.Sigmoid);
            brain.Compile(true, 1.5f);
        }
        else
        {
            // Apply received model
            print(agentInterface.receivedModel.layers.Count);
            brain = agentInterface.receivedModel;
            brain.Mutate(0.2f, 0.2f, 1f/64f, 2f, 1.5f);
            // print(brain.name);
        }
        
        motherNature = GameObject.FindGameObjectWithTag("GM").GetComponent<MotherNature>();
        if (enableEvolution)
            motherNature.agents.Add(agent);
    }

    // Update is called once per frame
    void Update()
    {
        computeClockTimer -= Time.deltaTime;
        if (computeClockTimer <= 0f)
        {
            computeClockTimer = computeClock;
            OnCompute();
        }

        agent.reward = transform.position.x;
    }

    void OnCompute()
    {
        float theta = 0f;
        float[] distances = new float[rayCount];
        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            RaycastHit2D col = Physics2D.Raycast(transform.position, dir, sensorRange);

            if (col.collider != null)
                distances[i] = Vector3.Distance(transform.position, col.point);
            else
                distances[i] = sensorRange + 0.5f;
            
            theta += 2 * Mathf.PI / rayCount;
        }
        
        
        float[] inputVector = new float[rayCount + 2 + residualConnectionCount];
        for (int i = 0; i < rayCount; i++)
        {
            inputVector[i] = distances[i];
        }

        var position = transform.position;
        inputVector[rayCount] = position.x;
        inputVector[rayCount+1] = position.y;

        for (int i = 0; i < residualConnectionCount; i++)
        {
            inputVector[rayCount + 2 + i] = residual[i];
        }
        
        float[] outputs = brain.Compute(inputVector);

        rb.velocity = new Vector2((outputs[0] * 2 - 1) * baseSpeed, rb.velocity.y);
        bool onGround = 
            Physics2D.OverlapCircle(position + new Vector3(0, -0.5f), 0.5f, groundLayer) != null
            ? true
            : false;
        if (outputs[1] > 0.75f && onGround)
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);

        for (int i = 0; i < residualConnectionCount; i++)
        {
            residual[i] = outputs[2 + i];
        }
    }
}
