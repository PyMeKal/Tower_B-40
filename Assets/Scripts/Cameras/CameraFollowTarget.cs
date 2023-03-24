using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    public Transform target;
    public float speed;
    public Vector3 offset;
    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, target.position + offset, speed);
    }
}
