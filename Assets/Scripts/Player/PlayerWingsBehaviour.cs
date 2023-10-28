using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.U2D;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class PlayerWing
{
    public enum PlayerWingState
    {
        idle,
        followMouse,
    }
    
    
    public readonly int id;
    public PlayerWingState state;
    public Material material;
    public Vector3 position;
    public Vector3 idlePosition;
    private Vector3 offsetIdlePosition;
    private Vector3 offsetIdleVelocity;
    public Vector3 targetPosition;
    private Transform playerTransform;
    private PlayerMovement playerMovement;
    private Rigidbody2D playerRb;
    public float speed;
    public float range;
    private float travelDistanceCoeff;

    private GameObject meshObject;
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private List<Vector3> vertices;
    private List<Vector3> positionHistory;

    public PlayerWing(int id, GameObject meshObject, Material material, Vector3 idlePosition, float speed, float range, float travelDistanceCoeff)
    {
        this.id = id;
        this.state = PlayerWingState.idle;

        this.meshObject = meshObject;
        
        this.material = material;
        this.idlePosition = idlePosition;
        this.speed = speed;
        this.range = range;
        this.travelDistanceCoeff = travelDistanceCoeff;
        playerTransform = GM.GetPlayer().transform;
        playerRb = playerTransform.GetComponent<Rigidbody2D>();
        playerMovement = playerTransform.GetComponent<PlayerMovement>();

        var cap = 60;
        positionHistory = new List<Vector3>(60);
        for (int i = 0; i < cap; i++)
            positionHistory.Add(position);
        
        position = idlePosition;
        offsetIdlePosition = idlePosition;

        mesh = new Mesh();
        meshFilter = meshObject.GetComponent<MeshFilter>();
        meshRenderer = meshObject.GetComponent<MeshRenderer>();
        vertices = new List<Vector3>();
        for (int i = 0; i < 3; i++)
        {
            vertices.Add(idlePosition + new Vector3(-i*0.05f, 0f));
        }

        state = PlayerWingState.idle;
    }

    public void FollowMouse()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        targetPosition = Vector3.Lerp(Vector3.zero, mousePosition - playerTransform.position, travelDistanceCoeff);
        position = Vector3.Lerp(position, targetPosition, speed);

        if (Vector3.SqrMagnitude(playerTransform.position - position) > range * range)
        {
            position = position.normalized * range;
        }


        positionHistory.RemoveAt(0);
        positionHistory.Add(position);

        float minLength1 = 0.05f, minLength2 = 0.075f;
        int delay1 = 1, delay2 = 4; 
        
        vertices[0] = position;
        vertices[1] = (
            positionHistory[^delay1] // Index-from-end operation???
            + (mousePosition - position).normalized * -minLength1
            );
        vertices[2] = (
            positionHistory[^delay2] 
            + (mousePosition - position).normalized * -minLength1
        );

        
    }
    
    public void Idle()
    {
        int xMultiplier = playerMovement.facingRight ? 1 : -1;

        float offsetAccelerationRange = 0.0005f;
        var offsetAcceleration =
            new Vector3(Random.Range(-offsetAccelerationRange, offsetAccelerationRange),
                        Random.Range(-offsetAccelerationRange, offsetAccelerationRange));

        var d = idlePosition - offsetIdlePosition;
        var wingPullAccel = d.normalized * (d.sqrMagnitude * 0.05f);
        offsetAcceleration += wingPullAccel;
        
        offsetIdleVelocity += offsetAcceleration;
        
        // Damping
        float maxOffsetVelocity = 0.2f * Time.fixedDeltaTime;
        offsetIdleVelocity = offsetIdleVelocity.sqrMagnitude > maxOffsetVelocity * maxOffsetVelocity
            ? offsetIdleVelocity.normalized * maxOffsetVelocity
            : offsetIdleVelocity;
        
        offsetIdlePosition += offsetIdleVelocity;

        float offsetMaxDist = 0.5f;
        if (Vector3.SqrMagnitude(idlePosition - offsetIdlePosition) > offsetMaxDist * offsetMaxDist)
            offsetIdlePosition = idlePosition + (offsetIdlePosition - idlePosition).normalized * offsetMaxDist;
        
        var correctIdlePosition = offsetIdlePosition;
        correctIdlePosition.x *= xMultiplier;  // "correct" as in corrected for facing right or left
        position = correctIdlePosition;
        
        Vector3[] vertexTargets =
        {
            position,
            position + new Vector3(-0.1f * xMultiplier, 0.2f, 0f),
            position + new Vector3(-0.3f * xMultiplier, 0.5f, 0f),
        };

        float[] vertexSpeeds =
        {
            speed,
            speed * 0.75f,
            speed * 0.5f,
        };
        
        // I know this is stupid code but this line is essential for smoothly following the player
        Vector3 playerDS = playerRb.velocity * Time.fixedDeltaTime;
        
        for (int i = 0; i < 3; i++)
        {
            vertices[i] -= playerDS;
            vertices[i] = Vector3.Lerp(vertices[i], vertexTargets[i], vertexSpeeds[i]);
        }
    }

    public void DrawMesh()
    {
        Vector3 pos1, pos2, pos3;
        float width = 0.1f;
        pos1 = vertices[2];
        pos2 = vertices[1];
        pos3 = vertices[0];

        Vector3 pos2A = ((pos1 - pos2).normalized + (pos3 - pos2).normalized).normalized * width + pos2;
        Vector3 pos2B = -((pos1 - pos2).normalized + (pos3 - pos2).normalized).normalized * width + pos2;
        
        
        mesh.vertices = new Vector3[]
        {
            pos1,
            pos2A,
            pos3,
            pos2B,
        };

        mesh.triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
        };

        meshFilter.mesh = mesh;
        meshRenderer.material = material;
    }
}

public class PlayerWingsBehaviour : MonoBehaviour
{
    public GameObject wing1meshObject, wing2meshObject;

    public PlayerWing wing1, wing2;

    public Material wing1Material, wing2Material;

    public Vector3 wing1Offset = new Vector3(-0.5f, 0.5f);
    public Vector3 wing2Offset = new Vector3(-0.25f, 0.5f);
    
    void Start()
    {
        wing1 = new PlayerWing(1, wing1meshObject, wing1Material, wing1Offset, 0.5f, 7f, 0.75f);
        wing2 = new PlayerWing(1, wing2meshObject, wing2Material, wing2Offset, 0.5f, 7f, 0.75f);
    }
    
    private void FixedUpdate()
    {
        switch (wing1.state)
        {
            case PlayerWing.PlayerWingState.idle:
                wing1.Idle();
                break;
            case PlayerWing.PlayerWingState.followMouse:
                wing1.FollowMouse();
                break;
        }
        
        switch (wing2.state)
        {
            case PlayerWing.PlayerWingState.idle:
                wing2.Idle();
                break;
            case PlayerWing.PlayerWingState.followMouse:
                wing2.FollowMouse();
                break;
        }
    }

    private void Update()
    {
        wing1.state = Input.GetMouseButton(0) ? PlayerWing.PlayerWingState.followMouse : PlayerWing.PlayerWingState.idle;
        wing2.state = Input.GetMouseButton(1) ? PlayerWing.PlayerWingState.followMouse : PlayerWing.PlayerWingState.idle;
        
        
        wing1.DrawMesh();
        wing2.DrawMesh();;
    }
}
