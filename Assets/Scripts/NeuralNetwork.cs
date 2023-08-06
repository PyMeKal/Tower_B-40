using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;
using Unity.Burst;
using UnityEditor.Experimental.GraphView;
using Random = UnityEngine.Random;  // Praise jetbrains

// [BurstCompile]
public class NeuralNetwork
{
    public enum ActivationFunction
    {
        Sigmoid,
        ReLU,
        Swish,
        Tanh,
        Linear,
    }
    public class Dense
    {
        public readonly int neurons;
        private readonly ActivationFunction activationFunction;
        public float[,] weights;
        public float[] biases;

        private Func<float, float> relu = x => Mathf.Max(0f, x);
        private Func<float, float> sigmoid = x => 1f / (1f + Mathf.Exp(-x));
        private Func<float, float> swish = x => x / (1f + Mathf.Exp(-x));
        private Func<float, float> tanh = x => (Mathf.Exp(2 * x) - 1) / (Mathf.Exp(2 * x) + 1);

        public readonly Func<float, float> actFuncDel;
        
        public Dense(int neurons, ActivationFunction activationFunction)
        {
            this.neurons = neurons;
            this.activationFunction = activationFunction;
            biases = new float[neurons];
            actFuncDel = activationFunction switch
            {
                ActivationFunction.ReLU => relu,
                ActivationFunction.Sigmoid => sigmoid,
                ActivationFunction.Swish => swish,
                ActivationFunction.Tanh => tanh,
                _ => x => x,  // Default
            };
        }
        
        public Dense(int neurons, string activationFunction)
        {
            this.neurons = neurons;
            //this.activationFunction = activationFunction;
            biases = new float[neurons];
            switch (activationFunction)
            {
                case "relu":
                    this.activationFunction = ActivationFunction.ReLU;
                    actFuncDel = relu;
                    break;
                case "sigmoid":
                    this.activationFunction = ActivationFunction.Sigmoid;
                    actFuncDel = sigmoid;
                    break;
                case "swish":
                    this.activationFunction = ActivationFunction.Swish;
                    actFuncDel = swish;
                    break;
                case "tanh":
                    this.activationFunction = ActivationFunction.Tanh;
                    actFuncDel = tanh;
                    break;
                default:
                    this.activationFunction = ActivationFunction.Linear;
                    actFuncDel = x => x;
                    break;
            }
        }

        public float[] Forward(float[] inputVector)
        {
            float[] outputVector = new float[neurons];
            for (int n = 0; n < neurons; n++)
            {
                // Apparently this is a LINQ expression. Looks slick.
                float sum = inputVector.Select((t, c) => t * weights[n, c]).Sum();
                
                float y = actFuncDel(sum + biases[n]);
                /*
                y = activationFunction switch  // Compact switch expression
                {
                    ActivationFunction.ReLU => y < 0 ? 0f : y,
                    ActivationFunction.Sigmoid => 1f / (1 + Mathf.Exp(-y)),
                    ActivationFunction.Swish => y / (1 + Mathf.Exp(-y)),
                    _ => y  // Default
                };
                */
                outputVector[n] = y;
            }
            return outputVector;
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

        // Initializing weights & biases for all layers except input
        for (int i = 1; i < layers.Count; i++)
        {
            Dense thisLayer = layers[i];
            thisLayer.weights = new float[thisLayer.neurons, layers[i - 1].neurons];
            
            for (int n = 0; n < thisLayer.neurons; n++)  // n for neuron
            {
                for (int c = 0; c < layers[i - 1].neurons; c++)  // c for connection
                {
                    // Random within interval: [-1, 1]
                    thisLayer.weights[n, c] = Random.Range(-1f, 1f);
                }
                thisLayer.biases[n] = Random.Range(-1f, 1f);
            }
        }
        // First layer is the input layer. Don't do relu on it.
        for (int n = 0; n < layers[0].neurons; n++)
            layers[0].biases[n] = Random.Range(-1f, 1f);
        compiled = true;
    }

    public float[] Compute(float[] inputVector)
    {
        if (!compiled)
        {
            Debug.LogWarning($"Model {name} is not compiled. Compute call ignored.");
            return new float[]{};
        }

        if (layers[0].neurons != inputVector.Length)
        {
            throw new Exception(
                $"ERROR: Input vector size ({inputVector.Length}) does not match input layer size ({layers[0].neurons}).");
            
        }

        float[] logit = inputVector;
        foreach (Dense layer in layers)
        {
            if(layer == layers[0])
                logit = inputVector.Select((t, c) => layers[0].actFuncDel(t + layers[0].biases[c])).ToArray();
            else
            {
                logit = layer.Forward(logit);
            }
            // Debug.Log(logit);
        }

        return logit;
    }

    public void Mutate(float rangeWeights, float rangeBiases)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            if (i == 0)
            {
                // Input Layer
                layers[i].biases = layers[i].biases.Select(b => b + Random.Range(-rangeBiases, rangeBiases)).ToArray();
            }
            else
            {
                for(int n = 0; n < layers[i].neurons; n++)
                {
                    for (int c = 0; c < layers[i - 1].neurons; c++)
                        layers[i].weights[n, c] += Random.Range(-rangeWeights, rangeWeights);
                }
                layers[i].biases = layers[i].biases.Select(b => b + Random.Range(-rangeBiases, rangeBiases)).ToArray();
            }
            
        }
    }
}
