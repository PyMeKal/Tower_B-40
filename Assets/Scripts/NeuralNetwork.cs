using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork
{
    public enum ActivationFunction
    {
        Sigmoid,
        ReLU,
    }
    public class Dense
    {
        private int neurons;
        private ActivationFunction activationFunction;
        public float[][] weights;
        public float[] bias;

        public Dense(int neurons, ActivationFunction actFunction)
        {
            this.neurons = neurons;
            this.activationFunction = actFunction;
        }
    }

    public string name;
    public List<Dense> layers = new List<Dense>();
    public bool compiled = false;
    public NeuralNetwork(string name)
    {
        this.name = name;
    }

    public void AddLayer(int neurons, ActivationFunction actFunction)
    {
        if (compiled)
        {
            Debug.LogWarning($"Model {name} is compiled. Cannot add layers.");
            return;
        }

        layers.Add(new Dense(neurons, actFunction));
    }

    public void Compile()
    {
        if (compiled)
        {
            Debug.LogWarning($"Model {name} is already compiled. Compile call ignored.");
            return;
        }

        // Initializing weights & biases
        for (int i = 0; i < layers.Count; i++)
        {
            Dense thisLayer = layers[i];
            if (i == layers.Count - 1)
            {
                
            }
        }

    }
}
