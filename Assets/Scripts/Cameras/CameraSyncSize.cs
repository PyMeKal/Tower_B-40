using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSyncSize : MonoBehaviour
{
    public Camera syncTarget;
    void Awake()
    {
        GetComponent<Camera>().orthographicSize = syncTarget.orthographicSize;
    }
}
