using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class PFGrid
{
    // Class for handling A* pathfinding for various agents.
    // In this case, the mimic will use this for PF across short distances (inter-node)
    public struct PFTile
    {
        // Yes, I named this "Tile" cause I already named PFNode for Dijkstra PF, because they're clearly vertices.
        // Call me a genius
        
        public Vector3 worldPosition;
        public bool walkable;

        public Vector3Int cameFrom;
        // G = distance from starting node
        // H = distance to end node
        // F = G + H (+ W)
        public int g, h, f;
        // W = wall proximity cost. 
        public int w;
    }

    public string name;
    public int sizeX, sizeY;
    public Tilemap tilemap;
    private BoundsInt cellBounds;
    
    private PFTile[,] tiles;

    private const int DIAGONAL = 14;
    private const int STRAIGHT = 10;

    public PFGrid(string name, Tilemap tilemap)
    {
        this.name = name;
        this.tilemap = tilemap;

        cellBounds = tilemap.cellBounds;
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

                tiles[x, y].worldPosition = GetWorldPositionTile(thisTilePos, true);
            }
        }
    }
    
    public Vector3Int GetTilePositionWorld(Vector3 pos)
    {
        return tilemap.WorldToCell(pos);
    }

    public Vector3Int GetTilePositionArray(Vector3 pos)
    {
        return new Vector3Int(Mathf.RoundToInt(pos.x + cellBounds.xMin),
            Mathf.RoundToInt(pos.y + cellBounds.yMin));
    }

    public Vector3Int GetArrayPositionTile(Vector3Int pos)
    {
        return new Vector3Int(pos.x - cellBounds.xMin, pos.y - cellBounds.yMin);
    }

    public Vector3Int GetArrayPositionWorld(Vector3 pos)
    {
        return GetArrayPositionTile(GetTilePositionWorld(pos));
    }

    public Vector3 GetWorldPositionTile(Vector3Int pos, bool centerTiles)
    {
        Vector3 worldPos = tilemap.CellToWorld(pos);
        if (centerTiles)
            worldPos += tilemap.cellSize * 0.5f;
        return worldPos;
    }

    public void ResetTiles()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                tiles[x, y].f = 0;
                tiles[x, y].g = 9999;
                tiles[x, y].h = 0;
                tiles[x, y].cameFrom = Vector3Int.zero;
            }
        }
    }

    /*
    private Vector3Int[] GetNeighbourTiles(Vector3Int pos)
    {
        // Ripped from my GOL code in python
        // Simple algorithm wise
        // Honestly quite shit performance wise with the list creation overhead
        
        List<Vector3Int> neighbours = new List<Vector3Int>();
        List<int> checkX = new List<int>(3) { -1, 0, 1 };
        List<int> checkY = new List<int>(3) { -1, 0, 1 };

        if (pos.x == 0)
            checkX.Remove(-1);
        if (pos.x == sizeX - 1)
            checkX.Remove(1);
        if (pos.y == 0)
            checkY.Remove(-1);
        if (pos.y == sizeY - 1)
            checkY.Remove(1);

        foreach (int deltaX in checkX)
        {
            foreach (var deltaY in checkY)
            {
                if(deltaX == 0 && deltaY == 0)
                    continue;
                
                neighbours.Add(pos + new Vector3Int(deltaX, deltaY));
            }
        }

        return neighbours.ToArray();
    }*/
    
    private Vector3Int[] GetNeighbourTiles(Vector3Int pos)
    {
        // Optimized Code feat. GPT4
        List<Vector3Int> neighbours = new List<Vector3Int>(8); // At most 8 neighbours

        for (int deltaX = -1; deltaX <= 1; deltaX++)
        {
            for (int deltaY = -1; deltaY <= 1; deltaY++)
            {
                // Skip the current tile
                if (deltaX == 0 && deltaY == 0)
                    continue;

                int newX = pos.x + deltaX;
                int newY = pos.y + deltaY;

                // Check for borders
                if (newX < 0 || newX >= sizeX || newY < 0 || newY >= sizeY)
                    continue;

                neighbours.Add(new Vector3Int(newX, newY, 0));
            }
        }

        return neighbours.ToArray();
    }

    private int GetDistanceCost(Vector3Int a, Vector3Int b)
    {
        int deltaX = Mathf.Abs(a.x - b.x);
        int deltaY = Mathf.Abs(a.y - b.y);

        return STRAIGHT * Mathf.Abs(deltaX - deltaY) + DIAGONAL * Mathf.Min(deltaX, deltaY);
    }

    private Vector3Int[] RetracePath(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> pathList = new List<Vector3Int> { end };

        Vector3Int thisPos = end;
        
        int maxStep = 9999;
        while (maxStep > 0)
        {
            maxStep--;

            thisPos = tiles[thisPos.x, thisPos.y].cameFrom;
            pathList.Add(thisPos);
            if(thisPos == start)
                break;
        }

        pathList.Reverse();

        return pathList.ToArray();
    }
    
    public Vector3[] GetAStarPath(Vector3 startWorldPos, Vector3 endWorldPos, int maxStep = 999, int wCost = 0, 
        bool preventCornerCutting = false)
    {
        Vector3Int start = GetArrayPositionWorld(startWorldPos);
        Vector3Int end = GetArrayPositionWorld(endWorldPos);

        if (start == end)
        {
            // Debug.LogWarning("Start equals end");
            return Array.Empty<Vector3>();
        }

        if (!tiles[end.x, end.y].walkable || !tiles[start.x, start.y].walkable)
        {
            // Debug.LogWarning("Start or end not reachable");
            return Array.Empty<Vector3>();
        }
            
        // Since we're working with arrays of structs, we should store each index for x&y instead of the tile data.
        // -> access PFGrid.tiles directly.
        // might use z for storing f costs. For now, only x and y is used in each Vector3Int.
        List<Vector3Int> openList = new List<Vector3Int>();
        List<Vector3Int> closedList = new List<Vector3Int>();
        
        ResetTiles();
        
        openList.Add(start);
        tiles[start.x, start.y].g = 0;
        tiles[start.x, start.y].h = GetDistanceCost(start, end);
        tiles[start.x, start.y].f = GetDistanceCost(start, end);
        Vector3Int currentPos = start;

        int step = 0;
        
        while (openList.Count > 0 && step < maxStep)
        {
            step++;
            
            Vector3Int[] neighbours = GetNeighbourTiles(currentPos);
            foreach (var neighbour in neighbours)
            {
                PFTile neighbourTile = tiles[neighbour.x, neighbour.y];  // value type. read only.
                
                if(closedList.Contains(neighbour)) continue;
                
                if (!neighbourTile.walkable)
                {
                    closedList.Add(neighbour);
                    continue;
                }
                
                // Calculate costs for this neighbour tile
                // -------------------------------------------------------
                PFTile currentTile = tiles[currentPos.x, currentPos.y];
                
                int g = (neighbour - currentPos).x * (neighbour - currentPos).y == 0 ? 
                    STRAIGHT + currentTile.g : DIAGONAL + currentTile.g;
                int h = GetDistanceCost(neighbour, end);
                if (tiles[neighbour.x, neighbour.y].g <= g)
                {
                    // Do not update g(and f) if it's original value smaller than new value
                    g = tiles[neighbour.x, neighbour.y].g;
                }
                else
                {
                    // Update g(and f) along with its cameFrom, which would now be currentPos
                    tiles[neighbour.x, neighbour.y].g = g;
                    tiles[neighbour.x, neighbour.y].cameFrom = currentPos;
                }

                int w = 0;
                if (wCost>0)
                {
                    Vector3Int[] neighbourNeighbours = GetNeighbourTiles(neighbour);
                    if (neighbourNeighbours.Any(n => !tiles[n.x, n.y].walkable))
                    {
                        w = wCost;
                        // tiles[neighbour.x, neighbour.y].w = 3;
                    }
                }
                
                
                int f = g + h + w;
                // -------------------------------------------------------
                
                // Not actually necessary.
                // tiles[neighbour.x, neighbour.y].h = h; 
                
                tiles[neighbour.x, neighbour.y].f = f;
                
                
                openList.Add(neighbour);

                if (neighbour == end)
                {
                    // Pathfinding Complete
                    Vector3Int[] arrayPath = RetracePath(start, end);
                    Vector3[] worldPath = arrayPath.Select(p => tiles[p.x, p.y].worldPosition).ToArray();
                    return worldPath;
                }
            }

            openList.Remove(currentPos);
            closedList.Add(currentPos);
            
            int minFCost = 9999;
            foreach (Vector3Int tilePos in openList)
            {
                if (preventCornerCutting)
                {
                    Vector3Int deltaPos = tilePos - currentPos;
                    if (deltaPos.x != 0 && deltaPos.y != 0 && 
                        (!tiles[tilePos.x, currentPos.y].walkable || !tiles[currentPos.x, tilePos.y].walkable))
                    {
                        continue;
                    }
                }
                
                if (tiles[tilePos.x, tilePos.y].f < minFCost && !closedList.Contains(tilePos))
                {
                    minFCost = tiles[tilePos.x, tilePos.y].f;
                    currentPos = tilePos;
                }
            }
        }
        
        Debug.LogWarning("Pathfinding Failed: from " + startWorldPos + " to " + endWorldPos);
        return Array.Empty<Vector3>();
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

    // public Tilemap aStarTilemap;

    void ClearCache()
    {
        prevNodeIndexes = new int[nodeCount];
        minDistanceSum = new float[nodeCount];
        nodeChecked = new bool[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            prevNodeIndexes[i] = -1;
            minDistanceSum[i] = Mathf.Infinity;
        }
    }
    
    
    void SetupNodes()
    {
        nodeCount = nodesFolderTransform.childCount;
        nodes = new PFNode[nodeCount];
        nodeIndexes = new Dictionary<PFNode, int>();
        distanceMatrix = new float[nodeCount, nodeCount];

        ClearCache();
        
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
                if (ensureEdgeLinks && !adjacentNodes[j].adjacentNodes.Contains(thisNode))
                {
                    adjacentNodes[j].adjacentNodes = adjacentNodes[j].adjacentNodes.Concat(new[] { thisNode }).ToArray();
                    // Update distanceMatrix accordingly
                    distanceMatrix[nodeIndexes[adjacentNodes[j]], i] = Vector2.Distance(thisNode.position, adjacentNodes[j].position);
                }
                
                distanceMatrix[i, nodeIndexes[adjacentNodes[j]]] =
                    Vector2.Distance(thisNode.position, adjacentNodes[j].position);
            }
        }
    }
    
    //-----------------------------------------------------------------------------------------------------------------

    public PFNode[] GetDijkstraPath(PFNode start, PFNode end, int maxStep=9999, bool debug = false)
    {
        void DebugDijkstra(List<PFNode> path)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                nodesFolderTransform.GetChild(i).GetComponent<SpriteRenderer>().color = Color.grey;
            }
            
            foreach (var node in path)
            {
                nodesFolderTransform.GetChild(nodeIndexes[node]).GetComponent<SpriteRenderer>().color = Color.blue;
            }
            nodesFolderTransform.GetChild(nodeIndexes[start]).GetComponent<SpriteRenderer>().color = Color.green;
            nodesFolderTransform.GetChild(nodeIndexes[end]).GetComponent<SpriteRenderer>().color = Color.red;
            
        }
        
        if (start == end)
        {
            // Debug.LogWarning("Starting node must not equal end node");
            return Array.Empty<PFNode>();
        }

        ClearCache();
        
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
                
                
            // print("- " + nodeIndexes[currentNode]);
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
                
                if(debug)
                    DebugDijkstra(path);

                return path.ToArray();
            }
        }

        // No path found within max step.
        Debug.LogWarning("No path found :(");
        Debug.Log(start.position + " -> " + end.position);
        return Array.Empty<PFNode>();
    }

    void DebugPF()
    {
        // Literally used for debugging.
        // If this works first try I will shit my pants

        PFNode start = nodes[Random.Range(0, nodeCount-1)];
        PFNode end = nodes[Random.Range(0, nodeCount-1)];

        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < nodeCount; i++)
            {
                nodesFolderTransform.GetChild(i).GetComponent<SpriteRenderer>().color = Color.grey;
            }
            
            
            PFNode[] path = GetDijkstraPath(start, end, 9999);
            foreach (PFNode node in path)
            {
                print(node.position);
                nodesFolderTransform.GetChild(nodeIndexes[node]).GetComponent<SpriteRenderer>().color = Color.green;
            }    
        }
    }

    // private PFGrid grid;
    // private Vector3[] path = Array.Empty<Vector3>();
    /*
    void DebugAStar()
    {
        Vector3 start = Vector3.zero;
        Vector3 end = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            path = grid.GetAStarPath(start, end, wCost:10, preventCornerCutting:true);
        }
    }
    */
    
    private void Start()
    {
        SetupNodes();

        // Debug
        // grid = new PFGrid("x0.5", aStarTilemap);
    }
    
    //
    // private void Update()
    // {
    //     DebugPF();
    //     
    //     /*DebugAStar();
    //     for (int i = 1; i < path.Length; i++)
    //     {
    //         Debug.DrawLine(path[i-1], path[i]);
    //     }*/
    // }
    
}
