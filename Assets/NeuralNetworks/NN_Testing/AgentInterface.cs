using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;

public class AgentInterface : MonoBehaviour
{
    public Agent agent;
    public bool modelReceived;
    public NeuralNetwork receivedModel;
    public bool mutate;
    public string directLoadModel;

    private void Awake()
    {
        if (directLoadModel != string.Empty)
        {
            modelReceived = true;
            receivedModel = NeuralNetwork.LoadModel(NeuralNetwork.DefaultDirectory + directLoadModel);
            // print("Load successful");
        }
    }
}
