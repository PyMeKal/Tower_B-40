using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EvolutionUI : MonoBehaviour
{
    public int generation;
    public float bestReward, clock;
    public int agentCount;

    public TextMeshProUGUI text;

    // Update is called once per frame
    void Update()
    {
        text.text = $"Generation {generation} : {clock}\n" +
                    $"Best Reward: {bestReward}\n" +
                    $"\n" +
                    $"Agent Count: {agentCount}";
    }
}
