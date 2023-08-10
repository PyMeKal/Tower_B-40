using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class SimpleAgent : MonoBehaviour
{
    public NeuralNetwork brain;
    public Agent agent;
    private AgentInterface agentInterface;
    private MotherNature motherNature;
    public float computeClock, baseSpeed;

    private float computeClockTimer, t;
    
    //Movement

    private Vector2 setVelocity;
    
    void Start()
    {
        agentInterface = GetComponent<AgentInterface>();
        if (!agentInterface.modelReceived)
        {
            // Create default model with randomized w&b if no model has been loaded onto the agent;
            brain = new NeuralNetwork(gameObject.name + "_brain");
            brain.AddLayer(3, NeuralNetwork.ActivationFunction.Sigmoid); // for x y coords
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
            brain.AddLayer(2, NeuralNetwork.ActivationFunction.Tanh); // output -> velocity [-1, 1]
            brain.Compile();
        }
        else
        {
            // Apply received model
            brain = new NeuralNetwork("Donkey");
            brain.ReceiveModel(agentInterface.receivedModel);
            brain.Mutate(0.2f, 0.2f, 2f/32f);
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
            // print("recomputed");
            computeClockTimer = computeClock;
            var position = transform.position;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            float[] output = brain.Compute(new float[] { position.x, position.y,  Mathf.Sin(t)});
            // Debug.Log(output[0] + ", "  + output[1]);
            
            setVelocity = output.Contains(float.NaN) ? Vector3.zero : new Vector2(output[0], output[1]) * baseSpeed;
        }
        transform.Translate(setVelocity*Time.deltaTime);
    }

    void EvaluateReward()
    {
        agent.reward = transform.position.x;
    }
}
