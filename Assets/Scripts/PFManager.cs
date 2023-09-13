using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;

public class PFManager : MonoBehaviour
{
    // Using Dijkstra's algorithm for graph based pathfinding across longer distances
    
    [SerializeField] private Transform nodesFolderTransform;
    public PFNode[] nodes;  // Determines PFNode index
    private Dictionary<PFNode, int> nodeIndexes;
    private int[] prevNodeIndexes;  // Previous PFNode the search algorithm came from (Dijkstra's)
    private float[] minDistanceSum;  // Shortest distance found so far for each vertex
    public float[,] distanceMatrix;  // Stores distances between PFNodesd
    public int nodeCount;
    

    void SetupNodes()
    {
        nodeCount = nodesFolderTransform.childCount;
        nodes = new PFNode[nodeCount];
        nodeIndexes = new Dictionary<PFNode, int>();
        distanceMatrix = new float[nodeCount, nodeCount];

        prevNodeIndexes = new int[nodeCount];
        minDistanceSum = new float[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            prevNodeIndexes[i] = -1;
            minDistanceSum[i] = Mathf.Infinity;
        }


        // #1 Setup nodes array
        for (int i = 0; i < nodeCount; i++)
        {
            Transform nodeTransform = nodesFolderTransform.GetChild(i);
            nodes[i] = nodeTransform.GetComponent<PFNodeInterface>().node;
            nodeIndexes.Add(nodes[i], i);
        }

        for (int i = 0; i < nodeCount; i++)
        {
            PFNode thisNode = nodes[i];
            PFNode[] adjacentNodes = nodes[i].adjacentNodes;
            
            // print(thisNode.position);
            
            for (int j = 0; j < adjacentNodes.Length; j++)
            {
                distanceMatrix[i, nodeIndexes[adjacentNodes[j]]] =
                    Vector2.Distance(thisNode.position, adjacentNodes[j].position);
            }
        }
    }
    
    //-----------------------------------------------------------------------------------------------------------------
    
    private void Start()
    {
        SetupNodes();
    }

    public PFNode[] GetShortestPath(PFNode start, PFNode end, int maxStep=999)
    {
        int step = 0;
        PFNode currentNode = start;
        minDistanceSum[nodeIndexes[start]] = 0f;
        int startNodeIndex = nodeIndexes[start];
        
        while (step < maxStep)
        {
            step++;

            PFNode nextNode = currentNode.adjacentNodes[0];
            float nextNodeMDS = Mathf.Infinity;  // MDS for Min Distance Sum
            foreach (var adjacentNode in currentNode.adjacentNodes)
            {
                // # 1. Update MDS for this adjacent node
                // What the [fuck] [kind [of code] is this]
                int currentNodeIndex = nodeIndexes[currentNode];
                int adjacentNodeIndex = nodeIndexes[adjacentNode];
                
                if (minDistanceSum[currentNodeIndex] +
                    distanceMatrix[currentNodeIndex, adjacentNodeIndex] < minDistanceSum[adjacentNodeIndex])
                {
                    minDistanceSum[adjacentNodeIndex] = minDistanceSum[currentNodeIndex] +
                                                distanceMatrix[currentNodeIndex,
                                                    adjacentNodeIndex];
                    prevNodeIndexes[adjacentNodeIndex] = currentNodeIndex;
                }
                
                // MDS for this adjacent node
                float thisMDS = minDistanceSum[adjacentNodeIndex];
                
                // if this adjacent node is the closest from current node
                if (thisMDS < nextNodeMDS)
                {
                    nextNodeMDS = thisMDS;
                    nextNode = adjacentNode;
                }
            }

            currentNode = nextNode;
            if (currentNode == end)
            {
                // Destination Reached!
                List<PFNode> path = new List<PFNode>();
                path.Add(end);
                int prevNodeIndex = prevNodeIndexes[nodeIndexes[currentNode]];
                while (prevNodeIndex != startNodeIndex)
                {
                    path.Add(nodes[prevNodeIndex]);
                    prevNodeIndex = prevNodeIndexes[prevNodeIndex];
                }
                path.Add(start);
                
                path.Reverse();

                return path.ToArray();
            }
        }

        // No path found within max step.
        Debug.LogWarning("No path found :(");
        return Array.Empty<PFNode>();
    }

    void DebugPF()
    {
        // Literally used for debugging.
        // If this works first try I will shit my pants

        PFNode start = nodes[0];
        PFNode end = nodes[nodeCount - 1];

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PFNode[] path = GetShortestPath(start, end);
            foreach (PFNode node in path)
            {
                print(node.position);
            }    
        }
    }
    private void Update()
    {
        DebugPF();
    }
}
