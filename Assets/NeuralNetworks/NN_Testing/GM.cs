using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM : MonoBehaviour
{
    public float physicsSpeedMutlitplier = 1f;
    
    // Start is called before the first frame update
    void Start()
    {
        physicsSpeedMutlitplier = Mathf.Max(0f, physicsSpeedMutlitplier);
        Time.timeScale = physicsSpeedMutlitplier;
    }
}
