using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWings : MonoBehaviour
{
    private Vector3 initialLocalPos;
    [HideInInspector] public Vector3 targetPos;
    [SerializeField] private Transform playerSprite;
    [SerializeField] private float hoverMagnitude, hoverFrequency, followSpeed;
    [HideInInspector] public float t;
    void Start()
    {
        initialLocalPos = transform.position - playerSprite.position;
    }

    void FixedUpdate()
    {
        t += Time.deltaTime;
        Vector3 directionVector = playerSprite.localEulerAngles.y == 0 ? new Vector3(1f, 1f, 1f) : new Vector3(-1f, 1f, 1f);
        targetPos = playerSprite.position + Vector3.Scale(initialLocalPos, directionVector) + new Vector3(0, Mathf.Sin(t*hoverFrequency)*hoverMagnitude);

        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed);

        //Rotation
        if(Vector2.Distance(transform.position, targetPos) < 1f)
            transform.localEulerAngles = playerSprite.localEulerAngles;
        else
        {
            if(transform.position.x < targetPos.x)
                transform.localEulerAngles = new Vector3(0,0,0);
            else
                transform.localEulerAngles = new Vector3(0,180f,0);
        }
    }
}
