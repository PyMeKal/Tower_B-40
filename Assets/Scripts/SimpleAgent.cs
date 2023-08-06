using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAgent : MonoBehaviour
{
    public NeuralNetwork brain;

    public float computeClock;

    private float computeClockTimer;
    
    //Movement

    public Vector2 setVelocity = new Vector2();
    
    // Start is called before the first frame update
    void Start()
    {
        brain = new NeuralNetwork(gameObject.name + "_brain");
        brain.AddLayer(2, NeuralNetwork.ActivationFunction.Sigmoid);  // for x y coords
        brain.AddLayer(4, NeuralNetwork.ActivationFunction.Sigmoid);
        brain.AddLayer(2, NeuralNetwork.ActivationFunction.Tanh); // output -> velocity [-1, 1]
        brain.Compile();
        computeClockTimer = computeClock;
    }
    
    // Update is called once per frame
    void Update()
    {
        computeClockTimer -= Time.deltaTime;
        if (computeClockTimer <= 0f)
        {
            computeClockTimer = computeClock;
            var position = transform.position;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            float[] output = brain.Compute(new float[] { position.x, position.y });
            setVelocity = new Vector2(output[0], output[1]) * ;
        }
    }
}
