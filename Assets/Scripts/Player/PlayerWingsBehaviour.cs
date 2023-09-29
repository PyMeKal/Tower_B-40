using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.U2D;

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
    public Transform objectTransform;
    public Material material;
    public Vector3 position;
    public Vector3 idlePosition;
    public Vector3 targetPosition;
    private Transform playerTransform;
    public float speed;
    public float range;
    private float travelDistanceCoeff;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private List<Vector3> vertices;

    public PlayerWing(int id, Transform objectTransform, Material material, Vector3 idlePosition, float speed, float range, float travelDistanceCoeff)
    {
        this.id = id;
        this.state = PlayerWingState.idle;
        this.objectTransform = objectTransform;
        this.material = material;
        this.idlePosition = idlePosition;
        this.speed = speed;
        this.range = range;
        this.travelDistanceCoeff = travelDistanceCoeff;
        playerTransform = GM.GetPlayer().transform;

        position = idlePosition;
        objectTransform.position = Vector3.zero;

        mesh = new Mesh();
        meshFilter = objectTransform.GetComponent<MeshFilter>();
        meshRenderer = objectTransform.GetComponent<MeshRenderer>();
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
        targetPosition = Vector3.Lerp(playerTransform.position, mousePosition, travelDistanceCoeff);
        position = Vector3.Lerp(position, targetPosition, speed);

        if (Vector3.SqrMagnitude(playerTransform.position - position) > range * range)
        {
            var position1 = playerTransform.position;
            position = position1 + (position - position1).normalized * range;
        }
        
        vertices.RemoveAt(0);
        vertices.Add(position);
    }

    public void Idle()
    {
        var position1 = playerTransform.position;
        var idleWorldPos = idlePosition + position1;
        position = idleWorldPos;

        Vector3[] vertexTargets = new[]
        {
            idleWorldPos,
            idleWorldPos + new Vector3(-0.1f, 0.2f, 0f),
            idleWorldPos + new Vector3(-0.3f, 0.6f, 0f),
        };

        float[] vertexSpeeds = new[]
        {
            speed,
            speed * 0.75f,
            speed * 0.5f,
        };

        for (int i = 0; i < 3; i++)
        {
            vertices[i] = Vector3.Lerp(vertices[i], vertexTargets[i], vertexSpeeds[i]);
        }
    }

    public void DrawWingMeshMove()
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
    public Transform wing1Transform;  // In front of player
    public Transform wing2Transform;  // Behind player

    public PlayerWing wing1, wing2;

    public Material wing1Material, wing2Material;

    void Start()
    {
        Vector3 wing1Offset = new Vector3(-0.5f, 0.5f);
        wing1 = new PlayerWing(1, wing1Transform, wing1Material, wing1Offset, 0.5f, 7f, 0.75f);
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
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            wing1.state = PlayerWing.PlayerWingState.followMouse;
        }
        else
        {
            wing1.state = PlayerWing.PlayerWingState.idle;
        }
        
        
        wing1.DrawWingMeshMove();
    }
}
