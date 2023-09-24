using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MimicNode
{
    public Vector3 pos;
    public bool isSmall;
    public Transform nodeTransform;
    public MimicNode(Vector3 pos, bool isSmall, Transform nodeTransform)
    {
        this.pos = pos;
        this.isSmall = isSmall;
        this.nodeTransform = nodeTransform;
    }
}

public enum MimicState
{
    Sleep,
    Search,
    Chase,
    Flee,
    Dead
}

public class MimicAI : MonoBehaviour
{
    private Rigidbody2D rb;

    private PFManager pfManager;
    private Transform playerTransform;
    
    [Header("Nodes")]
    public List<MimicNode> nodes = new List<MimicNode>();
    [SerializeField] private int numNodes;
    [SerializeField] private GameObject smallNodeObj, nodeObj;
    [SerializeField][Range(0f, 1f)] private float smallNodeChance;
    [SerializeField][Tooltip("Random.Range(-nSA, +nSA) for X&Y")] private float nodeSpawnArea;

    [Header("\nAI")]
    public MimicState state;
    public Vector3 destination;
    private Queue<Vector3> pathQueue;

    void CreateNodes(int n)
    {
        for (int i = 0; i < n; i++)
        {
            MimicNode thisNode;
            Vector3 pos = transform.position + new Vector3(Random.Range(-nodeSpawnArea, nodeSpawnArea), Random.Range(-nodeSpawnArea, nodeSpawnArea));
            if (Random.Range(0f, 1f) > smallNodeChance)
            {
                /* Small Node
                - Small max tension/push
                - Small range
                */
                GameObject thisNodeObj = GameObject.Instantiate(smallNodeObj, pos, Quaternion.identity, transform);
                thisNode = new MimicNode(pos, true, thisNodeObj.transform);
            }
            else
            {
                /* Medium Node
                - Medium max tension/push
                - Medium range
                */
                GameObject thisNodeObj = GameObject.Instantiate(nodeObj, pos, Quaternion.identity, transform);
                thisNode = new MimicNode(pos, false, thisNodeObj.transform);
            }
            nodes.Add(thisNode);
        }
    }

    void WakeUp()
    {
        // Temporary..
        state = MimicState.Search;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pfManager = GM.GetPFManager();
        CreateNodes(numNodes);
        state = MimicState.Sleep;
        playerTransform = GM.GetPlayer().transform;
    }

    void Update()
    {
        switch (state)
        {
            case MimicState.Sleep:
                break;
            case MimicState.Search:
                STATE_Search();
                break;
        }
    }

    PFNode GetNearestNode(Vector3 position)
    {
        PFNode[] nodes = pfManager.nodes;
        PFNode nearestNode = nodes[0];
        float nearestNodeDistSqr = Mathf.Infinity;
        for(int i = 0; i < nodes.Length; i++)
        {
            Vector2 delta = new Vector2(nodes[i].position.x - position.x, nodes[i].position.y - position.y);
            float dSqr = delta.sqrMagnitude;
            if (dSqr < nearestNodeDistSqr)
            {
                nearestNode = nodes[i];
                nearestNodeDistSqr = dSqr;
            }
        }

        return nearestNode;
    }

    void STATE_Search()
    {
        destination = playerTransform.position;
        PFNode nearestNode = GetNearestNode(transform.position);
        PFNode nearestDestinationNode = GetNearestNode(destination);

        PFNode[] nodePath = pfManager.GetDijkstraPath(nearestNode, nearestDestinationNode);
        Vector3[][] paths = new Vector3[nodePath.Length][];

        PFGrid grid = new PFGrid("mimic", GM.GetGM().standardAStarTilemap);
        
        for (int i = 0; i < nodePath.Length; i++)
        {
            paths[i] = grid.GetAStarPath(nodePath[i].position, nodePath[i + 1].position,
                wCost: 10, preventCornerCutting: true);
            //for(int j = 0; j < )
        }
    }
}
