using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWings : MonoBehaviour
{
    private Vector3 initialLocalPos;
    [HideInInspector] public Vector3 targetPos;
    [SerializeField] private float hoverMagnitude, hoverFrequency;
    [HideInInspector] public float t;
    void Start()
    {
        initialLocalPos = transform.localPosition;
    }

    void Update()
    {
        t += Time.deltaTime;
        Vector3 directionVector = transform.parent.localEulerAngles.y == 0 ? new Vector3(1f, 1f, 1f) : new Vector3(-1f, 1f, 1f);
        targetPos = transform.parent.position + Vector3.Scale(initialLocalPos, directionVector) + new Vector3(0, Mathf.Sin(t*hoverFrequency)*hoverMagnitude);

        transform.position = Vector3.Lerp(transform.position, targetPos, 1f);
    }
}
