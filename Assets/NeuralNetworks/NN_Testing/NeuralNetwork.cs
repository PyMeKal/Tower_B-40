using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.Serialization.Json;
using Unity.Burst;
using UnityEditor.Experimental.GraphView;
using Random = UnityEngine.Random;  // Praise jetbrains

using System.IO;


// [BurstCompile]
[System.Serializable]
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
    
    // Feat. GPT4. Clamped to prevent overflow
    public static Func<float, float> relu = x => Mathf.Max(0f, x);
    public static Func<float, float> sigmoid = x => 1f / (1f + Mathf.Exp(-Mathf.Clamp(x, -10f, 10f)));
    public static Func<float, float> swish = x => x / (1f + Mathf.Exp(-Mathf.Clamp(x, -10f, 10f)));
    public static Func<float, float> tanh = x => {
        if (x > 10) return 1f;
        if (x < -10) return -1f;
        return (Mathf.Exp(2 * x) - 1f) / (Mathf.Exp(2 * x) + 1f);
    };
    
    [System.Serializable]
    public class Dense
    {
        public int neurons;
        public ActivationFunction activationFunction;
        public float[,] weights;
        public float[] weightsFlat;
        public float[] biases;

        public Func<float, float> actFuncDel;
        
        public Dense(int neurons, ActivationFunction activationFunction, int prevNeurons=0)
        {
            this.neurons = neurons;
            this.activationFunction = activationFunction;
            biases = new float[neurons];

            if (prevNeurons > 0)
                weightsFlat = new float[neurons * prevNeurons];
            else
                weightsFlat = new float[]{};
            
            SetActFuncDel();
        }

        public void SetActFuncDel()
        {
            actFuncDel = activationFunction switch
            {
                ActivationFunction.ReLU => relu,
                ActivationFunction.Sigmoid => sigmoid,
                ActivationFunction.Swish => swish,
                ActivationFunction.Tanh => tanh,
                _ => x => x,  // Default
            };
        }
        
        /* Alternate Constructor. Unused.
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
        */

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
                    Debug.LogWarning($"NaN occured (y) in Dense calculation (Potential overflow):" +
                                     $" sum: {sum}, act-func: {activationFunction.ToString()}\n" +
                                     $"inputVector: {String.Join(", ", inputVector.Select(f => f.ToString()))}");
                }
                outputVector[n] = y;
            }
            return outputVector;
        }

        public void UnpackFlatWeights()
        {
            int prevNeurons = weightsFlat.Length / neurons;
            int index = 0;
            weights = new float[neurons, prevNeurons];
            for (int n = 0; n < neurons; n++)
            {
                for (int c = 0; c < prevNeurons; c++)
                {
                    weights[n, c] = weightsFlat[index];
                    index++;
                }
            }
        }
    }
    
    public string name;
    public List<Dense> layers = new List<Dense>();
    public bool compiled = false;
    // private Dictionary<string, int> inputLabels, outputLabels;
    
    public static readonly string DefaultDirectory = Application.dataPath + "/NeuralNetworks";
    
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
        layers.Add(layers.Count == 0
            ? new Dense(neurons, actFunction)
            : new Dense(neurons, actFunction, layers.Last().neurons));
    }

    public void Compile(bool initializeWnB=true, float scale=1f)
    {
        
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
                    thisLayer.weights[n, c] = Random.Range(-scale, scale);
                }
                thisLayer.biases[n] = Random.Range(-scale, scale);
            }
        }
        // First layer is the input layer. Don't do relu on it.
        for (int n = 0; n < layers[0].neurons; n++)
            layers[0].biases[n] = Random.Range(-scale, scale);
    }

    void PackFlatWeights()
    {
        // Create weightsFlat for saving/loading in Json
        for (int i = 1; i < layers.Count; i++)
        {
            layers[i].weightsFlat = new float[layers[i].neurons * layers[i - 1].neurons];
            int index = 0;
            for (int n = 0; n < layers[i].neurons; n++)
            {
                for (int c = 0; c < layers[i - 1].neurons; c++)
                {
                    layers[i].weightsFlat[index] = layers[i].weights[n, c];
                    index++;
                }
            }
        }
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
            if (layer == layers[0])
                logit = inputVector.Select((t, c) => layers[0].actFuncDel(t + layers[0].biases[c])).ToArray();
            else
                logit = layer.Forward(logit);
        }

        return logit;
    }

    public void Mutate(float rangeWeights, float rangeBiases, float reshuffleChance=0.05f, 
        float reshuffleScaleWeight=1.5f, float reshuffleScaleBias=1f)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            // Biases
            for (int n = 0; n < layers[i].biases.Length; n++)
            {
                if (Random.Range(0f, 1f) <= reshuffleChance)
                {
                    layers[i].biases[n] = Random.Range(-reshuffleScaleBias, reshuffleScaleBias);
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
                            layers[i].weights[n, c] = Random.Range(-reshuffleScaleWeight, reshuffleScaleWeight);
                            continue;
                        }
                        layers[i].weights[n, c] += Random.Range(-rangeWeights, rangeWeights);
                    }
                }
            }
            
        }
        Compile(false);
    }

    /*
    public Dictionary<string, int> GetInputLabels()
    {
        return inputLabels;
    }
    public void SetInputLabels(string[] labels)
    {
        inputLabels = new Dictionary<string, int>();
        for (int index = 0; index < labels.Length; index++)
        {
            string label = labels[index];
            inputLabels.Add(label, index);
        }
    }
    public Dictionary<string, int> GetOutputLabels()
    {
        return outputLabels;
    }
    public void SetOutputLabels(string[] labels)
    {
        outputLabels = new Dictionary<string, int>();
        for (int index = 0; index < labels.Length; index++)
        {
            string label = labels[index];
            outputLabels.Add(label, index);
        }
    }
    */
    
    
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

    
    
    public string SaveModel(string directory="DEFAULT")
    {
        PackFlatWeights();

        if (directory is "" or "DEFAULT" or "default")
            directory = DefaultDirectory;
        
        // Saves model as json
        string dir = directory + "/model_" + name + "/";
        Directory.CreateDirectory(dir);
        for (int i = 0; i < layers.Count; i++)
        {
            string serialized = JsonUtility.ToJson(layers[i]);
            string thisDirectory = dir + $"layer_{i}.json";
            File.WriteAllText(thisDirectory, serialized);
        }
        File.WriteAllText(dir + "INFO.txt", $"{name}, {layers.Count}, \nname, Layers.Count");
        return dir;
    }
    
    public static NeuralNetwork LoadModel(string fullDirectory)
    {
        string[] info = File.ReadAllText(fullDirectory + "INFO.txt").Split(", ");
        string name = info[0];
        int layerCount = int.Parse(info[1]);
        NeuralNetwork loaded = new NeuralNetwork(name);
        for (int i = 0; i < layerCount; i++)
        {
            string jsonContent = File.ReadAllText(fullDirectory + $"layer_{i}.json");
            Dense thisLayer = JsonUtility.FromJson<Dense>(jsonContent);
            thisLayer.UnpackFlatWeights();
            thisLayer.SetActFuncDel();
            loaded.layers.Add(thisLayer);
        }
        loaded.Compile(false);
        return loaded;
    }
    
}
