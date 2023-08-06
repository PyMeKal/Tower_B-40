using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAgent : MonoBehaviour
{
    public NeuralNetwork brain;
    public Agent agent;

    public float computeClock, baseSpeed;

    private float computeClockTimer, t;
    
    //Movement

    private Vector2 setVelocity;
    
    // Start is called before the first frame update
    void Start()
    {
        brain = new NeuralNetwork(gameObject.name + "_brain");
        brain.AddLayer(3, NeuralNetwork.ActivationFunction.Linear);  // for x y coords
        brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
        brain.AddLayer(16, NeuralNetwork.ActivationFunction.ReLU);
        brain.AddLayer(2, NeuralNetwork.ActivationFunction.Tanh); // output -> velocity [-1, 1]
        brain.Compile();

        agent = new Agent(gameObject, 0f, brain);
        
        computeClockTimer = computeClock;
    }
    
    // Update is called once per frame
    void Update()
    {
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
            setVelocity = new Vector2(output[0], output[1]) * baseSpeed;
        }
        transform.Translate(setVelocity*Time.deltaTime);
    }
}
