using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
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
    public StateMachine stateMachine;
    private MimicAI mimicAI;
    
    public Transform clawTransform, jointTransform;
    public Vector3 clawTargetPosition;
    public float clawTargetAngle;
    public float clawSpeed;
    public Vector3 idlePosition;
    public float clawIdleAngle;
    public float clawOpenAmount;  // 0 = closed, 1 = fully open

    [SerializeField] private LineRenderer line1, line2;  // line1: body to joint, link2: joint to claw

    public float limbLength;  // Length of limb
    
    
    void Start()
    {
        mimicAI = GetComponent<MimicAI>();
        
        state = MimicArmState.Idle;

        IdleState idleState = new IdleState(this, idlePosition, clawIdleAngle, clawSpeed);
        stateMachine = new StateMachine();
        stateMachine.ChangeState(idleState);
    }

    
    void FixedUpdate()
    {
        stateMachine.Update();
        ArmPhysicsBehaviour();
        UpdateLineRenderers();
    }

    void ArmPhysicsBehaviour()
    {
        Vector3 GetJointPosition(Vector3 clawLocalPosition, bool facingRight)
        {
            // Simple vector based single-joint IK
            
            Vector3 midPoint = clawLocalPosition / 2f;
            float clawPositionAngle = Mathf.Atan2(clawLocalPosition.y, clawLocalPosition.x);

            float jointPositionAngle = 0f;

            float directionMultiplier = facingRight ? 1f : -1f;
            
            if (clawPositionAngle <= Mathf.PI * 0.5f || clawPositionAngle > Mathf.PI * 1.5f)
            {
                jointPositionAngle = clawPositionAngle - Mathf.PI * 0.5f * directionMultiplier;
            }
            else
            {
                jointPositionAngle = clawPositionAngle + Mathf.PI * 0.5f * directionMultiplier;
            }

            float clawDistance = clawLocalPosition.magnitude;

            if (limbLength < clawDistance * 0.5f)
            {
                Debug.LogWarning("Limb fucked. Using midpoint to claw position");
                return clawLocalPosition * 0.5f;
            }
            float jointDistanceFromMidpoint = Mathf.Sqrt(limbLength * limbLength - clawDistance * 0.5f * clawDistance * 0.5f);

            Vector3 jointPositionDirection = new Vector3(Mathf.Cos(jointPositionAngle), Mathf.Sin(jointPositionAngle));
            
            Vector3 jointPosition = midPoint + jointPositionDirection * jointDistanceFromMidpoint;

            return jointPosition;
        }

        var localPosition = clawTransform.localPosition;
        localPosition = Vector3.Lerp(localPosition, clawTargetPosition, clawSpeed);
        localPosition += (Vector3)GetComponent<Rigidbody2D>().velocity * (-1f * Time.fixedDeltaTime);
        clawTransform.localPosition = localPosition;
        clawTransform.localEulerAngles = new Vector3(0, 0, clawTargetAngle * Mathf.Rad2Deg);
        jointTransform.localPosition = Vector3.Lerp(jointTransform.localPosition, GetJointPosition(localPosition, mimicAI.FacingRight), clawSpeed);
    }

    void UpdateLineRenderers()
    {
        line1.SetPosition(0, transform.position);
        line1.SetPosition(1, jointTransform.position);
        line2.SetPosition(0, jointTransform.position);
        line2.SetPosition(1, clawTransform.position);
    }
    
    private class IdleState : IState
    {
        private MimicArm arm;
        private readonly Vector3 idleClawPosition;
        private readonly float clawTargetAngle;
        private float idleClawSpeed;

        public IdleState(MimicArm arm, Vector3 idleClawPosition, float clawTargetAngle, float idleClawSpeed)
        {
            this.arm = arm;
            this.idleClawPosition = idleClawPosition;
            this.clawTargetAngle = clawTargetAngle;
            this.idleClawSpeed = idleClawSpeed;
        }
        
        public void Enter()
        {
            
        }

        public void Update()
        {
            Vector3 currentClawTargetPosition = arm.mimicAI.FacingRight ? idleClawPosition : new Vector3(-idleClawPosition.x, idleClawPosition.y);

            var position = arm.transform.position;
            RaycastHit2D clawPositionRay = Physics2D.Raycast(position, currentClawTargetPosition,
                currentClawTargetPosition.magnitude, arm.mimicAI.wallLayers);

            if (clawPositionRay)
                currentClawTargetPosition = clawPositionRay.point - (Vector2)position;
            
            arm.clawTargetPosition = currentClawTargetPosition;
            arm.clawTargetAngle = arm.mimicAI.FacingRight ? clawTargetAngle : Mathf.PI - clawTargetAngle;
        }

        public void Exit()
        {
            
        }
    }
}
