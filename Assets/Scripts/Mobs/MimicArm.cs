using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class MimicArm : MonoBehaviour
{
    public enum MimicArmState
    {
        Idle,
        Aim,
        Strike,
    }

    public MimicArmState state;
    
    public Transform clawTransform, jointTransform;
    private Vector3 clawTargetPosition;
    public float clawSpeed;
    public Vector3 idlePosition;
    public float clawIdleAngle;
    public float clawOpenAmount;  // 0 = closed, 1 = fully open

    private LineRenderer line1, line2;  // line1: body to joint, link2: joint to claw

    public float limbLength;  // Length of limb
    
    
    void Start()
    {
        state = MimicArmState.Idle;
    }

    
    void FixedUpdate()
    {
        switch (state)
        {
            case MimicArmState.Idle:
                Idle();
                break;
            case MimicArmState.Aim:
                Aim();
                break;
            case MimicArmState.Strike:
                Strike();
                break;
        }
        
        ArmPhysicsBehaviour();
    }

    void ArmPhysicsBehaviour()
    {
        Vector3 GetJointPosition(Vector3 clawLocalPosition)
        {
            Vector3 midPoint = clawLocalPosition / 2f;
            float clawPositionAngle = Mathf.Atan2(clawLocalPosition.y, clawLocalPosition.x);

            float jointPositionAngle = 0f;
            
            if (clawPositionAngle <= Mathf.PI * 0.5f || clawPositionAngle > Mathf.PI * 1.5f)
            {
                jointPositionAngle = clawPositionAngle - Mathf.PI * 0.5f;
            }
            else
            {
                jointPositionAngle = clawPositionAngle + Mathf.PI * 0.5f;
            }

            float clawDistance = clawLocalPosition.magnitude;

            if (limbLength < clawDistance * 0.5f)
            {
                Debug.LogWarning("Limb fucked");
                return Vector3.zero;
            }
            float jointDistanceFromMidpoint = Mathf.Sqrt(limbLength * limbLength - clawDistance * 0.5f * clawDistance * 0.5f);

            Vector3 jointPositionDirection = new Vector3(Mathf.Cos(jointPositionAngle), Mathf.Sin(jointPositionAngle));
            
            Vector3 jointPosition = midPoint + jointPositionDirection * jointDistanceFromMidpoint;

            return jointPosition;
        }

        clawTransform.localPosition = clawTargetPosition;
        jointTransform.position = GetJointPosition(clawTargetPosition);
    }
    
    void Idle()
    {
        clawTargetPosition = idlePosition;
    }

    void Aim()
    {
        
    }

    void Strike()
    {
        
    }
}
