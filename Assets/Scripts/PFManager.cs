using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PFGrid
{
    // Class for handling A* pathfinding for various agents.
    // In this case, the mimic will use this for PF across short distances (inter-node)
    public struct PFTile
    {
        // Yes, I named this "Tile" cause I already named PFNode for Dijkstra PF, when they're clearly vertices.
        // Call me a genius
        
        public Vector3 worldPosition;
        public bool walkable;
        public int g, h, f;
    }

    public string name;
    public int sizeX, sizeY;
    public Tilemap tilemap;

    private PFTile[,] tiles;

    public PFGrid(string name, Tilemap tilemap)
    {
        this.name = name;
        this.tilemap = tilemap;

        var cellBounds = tilemap.cellBounds;
        sizeX = cellBounds.xMax - cellBounds.xMin;
        sizeY = cellBounds.yMax - cellBounds.yMin;
        
        tiles = new PFTile[sizeX, sizeY];
        
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                // x, y variable for indexes in array PFTile[,] tiles (=Array Position)
                // NOT tile positions in tilemap (=Tile Position)
                
                Vector3Int thisTilePos = new Vector3Int(x + cellBounds.xMin, y + cellBounds.yMin);
                if (!tilemap.HasTile(thisTilePos))
                {
                    tiles[x, y].walkable = true;
                }

                tiles[x, y].worldPosition = tilemap.CellToWorld(thisTilePos);
            }
        }
    }
    
    public Vector3Int GetTilePositionWorld(Vector3 pos)
    {
        return tilemap.WorldToCell(pos);
    }

    public Vector3Int GetTilePositionArray(Vector3 pos)
    {
        return new Vector3Int(Mathf.RoundToInt(pos.x + tilemap.cellBounds.xMin),
            Mathf.RoundToInt(pos.y + tilemap.cellBounds.yMin));
    }
    
    public Vector3[] GetAStarPath(Vector3 start, Vector3 end, int maxStep = 999)
    {
        
    }
}

public class PFManager : MonoBehaviour
{
    // Using Dijkstra's algorithm for graph based pathfinding across longer distances
    
    [SerializeField] private Transform nodesFolderTransform;
    public PFNode[] nodes;  // Determines PFNode index
    public bool ensureEdgeLinks;  // Checks if adjacent vertices both have each other in their respective adjacentNodes array 
    private Dictionary<PFNode, int> nodeIndexes;
    private int[] prevNodeIndexes;  // Previous PFNode the search algorithm came from (Dijkstra's)
    private float[] minDistanceSum;  // Shortest distance found so far for each vertex
    private bool[] nodeChecked;  // True if node has been checked
    public float[,] distanceMatrix;  // Stores distances between PFNodes
    public int nodeCount;
    

    void SetupNodes()
    {
        nodeCount = nodesFolderTransform.childCount;
        nodes = new PFNode[nodeCount];
        nodeIndexes = new Dictionary<PFNode, int>();
        distanceMatrix = new float[nodeCount, nodeCount];

        prevNodeIndexes = new int[nodeCount];
        minDistanceSum = new float[nodeCount];
        nodeChecked = new bool[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            prevNodeIndexes[i] = -1;
            minDistanceSum[i] = Mathf.Infinity;
        }


        // #1 Setup nodes array
        for (int i = 0; i < nodeCount; i++)
        {
            Transform nodeTransform = nodesFolderTransform.GetChild(i);
            PFNodeInterface nodeInterface = nodeTransform.GetComponent<PFNodeInterface>();
            nodes[i] = nodeInterface.node;
            nodeInterface.index = i;
            nodeIndexes.Add(nodes[i], i);
            nodeTransform.GetComponent<SpriteRenderer>().color = Color.grey;
        }

        for (int i = 0; i < nodeCount; i++)
        {
            PFNode thisNode = nodes[i];
            PFNode[] adjacentNodes = nodes[i].adjacentNodes;
            
            // print(thisNode.position);
            
            for (int j = 0; j < adjacentNodes.Length; j++)
            {
                // Ensure nodes are connected both ways
                if (ensureEdgeLinks && !adjacentNodes[i].adjacentNodes.Contains(thisNode))
                {
                    adjacentNodes[i].adjacentNodes = adjacentNodes[i].adjacentNodes.Concat(new[] { thisNode }).ToArray();
                    // Update distanceMatrix accordingly
                    distanceMatrix[nodeIndexes[adjacentNodes[j]], i] = Vector2.Distance(thisNode.position, adjacentNodes[j].position);
                }
                
                distanceMatrix[i, nodeIndexes[adjacentNodes[j]]] =
                    Vector2.Distance(thisNode.position, adjacentNodes[j].position);
            }
        }
    }
    
    //-----------------------------------------------------------------------------------------------------------------

    public PFNode[] GetShortestPath(PFNode start, PFNode end, int maxStep=999)
    {
        int step = 0;
        PFNode currentNode = start;
        minDistanceSum[nodeIndexes[start]] = 0f;
        int startNodeIndex = nodeIndexes[start];
        
        while (step < maxStep)
        {
            step++;;
            int currentNodeIndex = nodeIndexes[currentNode];
            
            foreach (var adjacentNode in currentNode.adjacentNodes)
            {
                // # 1. Update MDS for this adjacent node
                // What the [fuck] [kind [of code] is this]
                int adjacentNodeIndex = nodeIndexes[adjacentNode];
                
                if(adjacentNodeIndex == currentNodeIndex) print("What the fuck");
                
                if(nodeChecked[adjacentNodeIndex]) continue;
                
                if (minDistanceSum[currentNodeIndex] +
                    distanceMatrix[currentNodeIndex, adjacentNodeIndex] < minDistanceSum[adjacentNodeIndex])
                {
                    minDistanceSum[adjacentNodeIndex] = minDistanceSum[currentNodeIndex] +
                                                distanceMatrix[currentNodeIndex,
                                                    adjacentNodeIndex];
                    prevNodeIndexes[adjacentNodeIndex] = currentNodeIndex;
                }
                
            }
            nodeChecked[currentNodeIndex] = true;

            float shortestMDS = Mathf.Infinity;
            for (int i = 0; i < nodeCount; i++)
            {
                if(nodeChecked[i]) continue;

                if (minDistanceSum[i] < shortestMDS)
                {
                    shortestMDS = minDistanceSum[i];
                    currentNode = nodes[i];
                }
            }
                
                
            print("- " + nodeIndexes[currentNode]);
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
        PFNode end = nodes[10];

        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < nodeCount; i++)
            {
                nodesFolderTransform.GetChild(i).GetComponent<SpriteRenderer>().color = Color.grey;
            }
            
            
            PFNode[] path = GetShortestPath(start, end, 9999);
            foreach (PFNode node in path)
            {
                print(node.position);
                nodesFolderTransform.GetChild(nodeIndexes[node]).GetComponent<SpriteRenderer>().color = Color.green;
            }    
        }
    }
    
    private void Start()
    {
        SetupNodes();
    }

    public Tilemap tilemap;
    
    private void Update()
    {
        // DebugPF();
        print(tilemap.cellBounds.xMin + " / " + tilemap.cellBounds.yMin);
    }
}
