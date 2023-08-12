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
        public readonly ActivationFunction activationFunction;
        public float[,] weights;
        public float[] biases;

        private Func<float, float> relu = x => Mathf.Max(0f, x);
        
        // Feat. GPT4. Clamped to prevent overflow
        private Func<float, float> sigmoid = x => 1f / (1f + Mathf.Exp(-Mathf.Clamp(x, -10f, 10f)));
        private Func<float, float> swish = x => x / (1f + Mathf.Exp(-Mathf.Clamp(x, -10f, 10f)));
        private Func<float, float> tanh = x => {
            if (x > 10) return 1f;
            if (x < -10) return -1f;
            return (Mathf.Exp(2 * x) - 1f) / (Mathf.Exp(2 * x) + 1f);
        };

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
                if (float.IsNaN(y))
                {
                    Debug.LogWarning($"NaN occured (y) in Dense calculation: sum: {sum}, act-func: {activationFunction.ToString()}\n" +
                                     $"inputVector: {String.Join(", ", inputVector.Select(f => f.ToString()))}");
                }
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
        //--------------------------------------------
        
        layers.Add(new Dense(neurons, actFunction));
    }

    public void Compile(bool initializeWnB=true)
    {
        if (compiled)
        {
            Debug.LogWarning($"Model {name} is already compiled. Compile call ignored.");
            return;
        }
        //--------------------------------------------
        
        compiled = true;

        if (!initializeWnB)
            return;  // Skip initialization
        
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
        
        //--------------------------------------------
        
        float[] logit = inputVector;
        foreach (Dense layer in layers)
        {
            if(layer == layers[0])
                logit = inputVector.Select((t, c) => layers[0].actFuncDel(t + layers[0].biases[c])).ToArray();
            else
                logit = layer.Forward(logit);
        }

        return logit;
    }

    public void Mutate(float rangeWeights, float rangeBiases, float reshuffleChance=0.05f)
    {
        float reshuffleAbsRangeWeight = 1.5f;
        float reshuffleAbsRangeBias = 1f;
        
        
        for (int i = 0; i < layers.Count; i++)
        {
            // Biases
            for (int n = 0; n < layers[i].biases.Length; n++)
            {
                if (Random.Range(0f, 1f) <= reshuffleChance)
                {
                    layers[i].biases[n] = Random.Range(-reshuffleAbsRangeBias, reshuffleAbsRangeBias);
                    continue;
                }
                layers[i].biases[n] += Random.Range(-rangeBiases, rangeBiases);
            }
            
            // Weights
            if (i > 0)
            {
                for (int n = 0; n < layers[i].neurons; n++)
                {
                    for (int c = 0; c < layers[i - 1].neurons; c++)
                    {
                        if (Random.Range(0f, 1f) <= reshuffleChance)
                        {
                            layers[i].weights[n, c] = Random.Range(-reshuffleAbsRangeWeight, reshuffleAbsRangeWeight);
                            continue;
                        }
                        layers[i].weights[n, c] += Random.Range(-rangeWeights, rangeWeights);
                    }
                }
            }
            
        }
    }

    public NeuralNetwork DeepCopy(string newName)
    {
        NeuralNetwork newModel = new NeuralNetwork(newName);
        List<Dense> newLayers = new List<Dense>();
        foreach (var layer in layers)
        {
            Dense newLayer = new Dense(layer.neurons, layer.activationFunction);
            
            // Making sure arrays are not aliased. Feat. GPT4
            if (layer != layers[0])
            {
                // Input layer has no weights!
                float[,] newWeights = new float[layer.weights.GetLength(0), layer.weights.GetLength(1)];
                Array.Copy(layer.weights, newWeights, layer.weights.Length);
                newLayer.weights = newWeights;
            }

            newLayer.biases = (float[])layer.biases.Clone();
            
            newLayers.Add(newLayer);
        }

        newModel.layers = newLayers;
        newModel.Compile(false);
        // Debug.Log("Created new model: layer count = " + newLayers.Count);
        return newModel;
    }
}
