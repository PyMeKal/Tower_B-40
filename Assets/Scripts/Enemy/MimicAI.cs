using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 pos;
    public bool isSmall;
    public Transform nodeTransform;
    public Node(Vector3 pos, bool isSmall, Transform nodeTransform)
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
    [Header("Nodes")]
    public List<Node> nodes = new List<Node>();
    [SerializeField] private int numNodes;
    [SerializeField] private GameObject smallNodeObj, nodeObj;
    [SerializeField][Range(0f, 1f)] private float smallNodeChance;
    [SerializeField][Tooltip("Random.Range(-nSA, +nSA) for X&Y")] private float nodeSpawnArea;

    [Header("\nAI")]
    public MimicState state;
    public Vector3 destination;

    void CreateNodes(int n)
    {
        for (int i = 0; i < n; i++)
        {
            Node thisNode;
            Vector3 pos = transform.position + new Vector3(Random.Range(-nodeSpawnArea, nodeSpawnArea), Random.Range(-nodeSpawnArea, nodeSpawnArea));
            if (Random.Range(0f, 1f) > smallNodeChance)
            {
                /* Small Node
                - Small max tension/push
                - Small range
                */
                GameObject thisNodeObj = GameObject.Instantiate(smallNodeObj, pos, Quaternion.identity, transform);
                thisNode = new Node(pos, true, thisNodeObj.transform);
            }
            else
            {
                /* Medium Node
                - Medium max tension/push
                - Medium range
                */
                GameObject thisNodeObj = GameObject.Instantiate(nodeObj, pos, Quaternion.identity, transform);
                thisNode = new Node(pos, false, thisNodeObj.transform);
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
        CreateNodes(numNodes);
        state = MimicState.Sleep;
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

    void STATE_Search()
    {
        
    }
}
