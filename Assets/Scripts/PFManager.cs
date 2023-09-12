using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PFManager : MonoBehaviour
{
    // Using Dijkstra's algorithm for graph based pathfinding across longer distances
    
    [SerializeField] private Transform nodesFolderTransform;
    public PFNode[] nodes;  // Determines PFNode index
    private PFNode[] prevNodes;  // Previous PFNode the search algorithm came from (Dijkstra's)
    public float[,] distanceMatrix;  // Stores distances between PFNodesd
    public int nodeCount;
    

    void SetupNodes()
    {
        nodeCount = nodesFolderTransform.childCount;
        nodes = new PFNode[nodeCount];
        distanceMatrix = new float[nodeCount, nodeCount];

        // #1 Setup nodes array
        for (int i = 0; i < nodeCount; i++)
        {
            Transform nodeTransform = nodesFolderTransform.GetChild(i);
            nodes[i] = nodeTransform.GetComponent<PFNodeInterface>().node;
        }

        for (int i = 0; i < nodeCount; i++)
        {
            PFNode thisNode = nodes[i];
            PFNode[] adjacentNodes = nodes[i].adjacentNodes;

            for (int j = 0; j < adjacentNodes.Length; j++)
            {
                distanceMatrix[i, GetPFNodeIndex(adjacentNodes[j])] =
                    Vector2.Distance(thisNode.position, adjacentNodes[j].position);
            }
        }
    }
    
    int GetPFNodeIndex(PFNode node)
    {
        for (int i = 0; i < nodeCount; i++)
        {
            if (nodes[i] == node) return i;
        }

        // No matching PFNode found
        Debug.LogWarning("No matching PFNode found");
        return -1;
    }
    
    //-----------------------------------------------------------------------------------------------------------------
    
    private void Start()
    {
        SetupNodes();
    }
}
