using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent
{
    public GameObject agent;
    public float reward;
    public NeuralNetwork brain;

    public Agent(GameObject agent, float reward, NeuralNetwork brain)
    {
        this.agent = agent;
        this.reward = reward;
        this.brain = brain;
    }
}
