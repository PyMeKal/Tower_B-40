using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent
{
    public GameObject agentObj;
    public float reward;
    public NeuralNetwork brain;

    public Agent(GameObject agentObj, float reward, NeuralNetwork brain)
    {
        this.agentObj = agentObj;
        this.reward = reward;
        this.brain = brain;
    }
}
