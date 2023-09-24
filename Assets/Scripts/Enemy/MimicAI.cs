using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEditor;
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
    public PFGrid grid;
    public MimicState state;
    public Vector3 destination;
    public Queue<Vector3> pathQueue;

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
        grid = new ("mimic", GM.GetGM().standardAStarTilemap);
        CreateNodes(numNodes);
        state = MimicState.Search;
        playerTransform = GM.GetPlayer().transform;
        pathQueue = new Queue<Vector3>();
    }

    void FixedUpdate()
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

    void EnqueuePath(Vector3 target, bool clear=false)
    {
        if(clear)
            pathQueue.Clear();
        
        PFNode nearestNode = GetNearestNode(transform.position);
        PFNode nearestDestinationNode = GetNearestNode(target);
        List<Vector3> path = new List<Vector3>();
        
        if (nearestNode != nearestDestinationNode)
        {
            // #1. start node != end node
            PFNode[] nodePath = pfManager.GetDijkstraPath(nearestNode, nearestDestinationNode);
        
            for (int i = 0; i < nodePath.Length - 1; i++)
            {
                Vector3[] pathChunk = grid.GetAStarPath(nodePath[i].position, nodePath[i + 1].position,
                    wCost: 10, preventCornerCutting: true);
            
                path.AddRange(pathChunk);
            }
        }

        // Process path to only include key tiles around turns instead of the entire path
        /*
         *  --------
         *          \
         *           \
         *            \
         *             -------
         *  would now be
         *
         *  -       -
         *           \
         *
         *             \
         *              -    -
         */
        else
        {
            // #2. start node == end node
            path = grid.GetAStarPath(transform.position, target).ToList();
        }
        print(path.Count);
        List<Vector3> pathProcessed = new List<Vector3> {path[0]};
        for (int i = 1; i < path.Count-1; i++)
        {
            Vector3 delta1, delta2;
            delta1 = path[i] - path[i - 1];
            delta2 = path[i + 1] - path[i];

            if ((delta2 - delta1).sqrMagnitude > 0.01f)
            {
                pathProcessed.Add(path[i]);
            }
        }
        pathProcessed.Add(path.Last());
        // pathProcessed.ForEach(tile => print(tile));
        pathProcessed.ForEach(tile => pathQueue.Enqueue(tile));
    }
    
    // Variables for STATE: Search
    [System.Serializable]
    public struct MimicSearchState
    {
        public float refreshPathClock;
        [HideInInspector] public float refreshPathClockTimer;
        [HideInInspector] public bool atDestination;
    }

    [Header("Variables for State: Search")]
    public MimicSearchState searchState;
    
    void STATE_Search()
    {
        if (searchState.refreshPathClockTimer <= 0f && searchState.atDestination)
        {
            searchState.refreshPathClockTimer = searchState.refreshPathClock;
            EnqueuePath(playerTransform.position, true);
        }

        Vector3 target;
        searchState.atDestination = !pathQueue.TryPeek(out target);
        print("Queue Length: " + pathQueue.Count);
        // #1. Destination reached (path queue empty)
        if (searchState.atDestination)
        {
            print("Destination Reached");
        }
        // #2. Destination not reached
        else
        {
            Vector3 direction = target - transform.position;
            rb.velocity = direction.normalized * 2f; // Placeholder
            if (direction.sqrMagnitude < 0.5f)
            {
                pathQueue.Dequeue();
            }
        }
        
        // Update searchState
        searchState.refreshPathClockTimer -= Time.fixedDeltaTime;

    }
}
