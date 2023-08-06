using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEyes : MonoBehaviour
{
    public Transform eyesSprite, spriteObj;
    [SerializeField] private Vector3 basePosition = Vector3.zero;
    // See CameraFollowTarget.cs
    private Vector3 aimedPosition;
    [SerializeField] private float maxCalculatedDistance;
    [SerializeField] private float speed;
    [SerializeField] private float distanceCoeff;
    [SerializeField] private Vector3 axisCoeff;

    void FixedUpdate()
    {
        Vector3 cameraPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 delta = cameraPos - transform.position;

        delta = delta.normalized * Mathf.Clamp(delta.magnitude, 0f, maxCalculatedDistance);

        // Used to take sprite orientation into account. 1 if facing right, -1 if facing left.
        float directionCoeff = Mathf.Abs(spriteObj.localEulerAngles.y) < 0.1f ? 1.0f : -1.0f;

        // Unused
        // Vector3 basePosWithDirection = new Vector3(basePosition.x*directionCoeff, basePosition.y);
        aimedPosition = Vector3.Lerp(basePosition, delta, distanceCoeff);
        
        aimedPosition.x *= directionCoeff;
        
        eyesSprite.localPosition = Vector3.Lerp(basePosition, Vector3.Scale(aimedPosition, axisCoeff), speed);
        // print(basePosition);
    }
}
