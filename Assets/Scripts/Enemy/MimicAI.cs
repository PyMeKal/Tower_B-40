using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class MimicLeg
{
    public readonly bool isSmall;
    public bool disabled;
    public Transform transform;
    public Transform bodyTransform;
    public Vector3 Position => transform.position;
    public Vector3 Direction => (target - bodyTransform.position).normalized;
    public bool connected = false;
    public Vector3 target;
    public readonly float speed;
    public readonly float pullForce;
    
    public MimicLeg(bool isSmall, Transform transform, Transform bodyTransform, float speed, float pullForce)
    {
        this.isSmall = isSmall;
        this.transform = transform;
        this.bodyTransform = bodyTransform;
        this.speed = speed;
        this.pullForce = pullForce;
    }

    public void MoveToTargetPoint()
    {
        /*
        Vector3 moveVector = (target - transform.position).normalized * speed;
        if ((target - Position).sqrMagnitude < moveVector.sqrMagnitude)
            moveVector = target - Position;
        transform.Translate(moveVector);
        */

        transform.position = Vector3.Lerp(Position, target, speed);
    }
}

public enum MimicState
{
    Sleep,
    Idle,
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

    [Header("Head")]
    [SerializeField] private Transform head;
    [SerializeField] private SpriteRenderer headSprite;
    [SerializeField] private Transform eye, pupil;
    
    [Header("Legs")]
    public List<MimicLeg> legs = new ();
    [SerializeField] private int numLegs;
    [SerializeField] private GameObject smallLegObj, legObj;
    [SerializeField][Range(0f, 1f)] private float smallLegChance;
    [SerializeField][Tooltip("Random.Range(-nSA, +nSA) for X&Y")] private float legSpawnArea;
    [SerializeField] private LayerMask wallLayers;

    [Header("\nAI")] 
    public PFGrid grid;
    public MimicState state;
    public Vector3 destination;
    public Queue<Vector3> pathQueue;

    void CreateLegs(int n)
    {
        for (int i = 0; i < n; i++)
        {
            MimicLeg thisNode;
            Vector3 pos = transform.position + new Vector3(Random.Range(-legSpawnArea, legSpawnArea), Random.Range(-legSpawnArea, legSpawnArea));
            if (Random.Range(0f, 1f) < smallLegChance)
            {
                /* Small Node
                - Small max tension/push
                - Small range
                */
                GameObject thisNodeObj = Instantiate(smallLegObj, pos, Quaternion.identity);
                thisNode = new MimicLeg(true, thisNodeObj.transform, transform, 0.25f, 1f);
            }
            else
            {
                /* Medium Node
                - Medium max tension/push
                - Medium range
                */
                GameObject thisNodeObj = Instantiate(legObj, pos, Quaternion.identity);
                thisNode = new MimicLeg(false, thisNodeObj.transform, transform, 0.05f, 2f);
            }
            legs.Add(thisNode);
        }
    }

    void WakeUp()
    {
        // Temporary..
        state = MimicState.Idle;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pfManager = GM.GetPFManager();
        grid = new ("mimic", GM.GetGM().standardAStarTilemap);
        CreateLegs(numLegs);
        state = MimicState.Search;
        playerTransform = GM.GetPlayer().transform;
        pathQueue = new Queue<Vector3>();
        SetLegTargetsStay();
        
        // headSprite = head.GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case MimicState.Sleep:
                break;
            case MimicState.Idle:
                break;
            case MimicState.Search:
                STATE_Search();
                break;
        }
        
        HeadBehaviour();
        EyeBehaviour();
        LegsBehaviour();
    }

    void HeadBehaviour()
    {
        Vector3 headLookAt;
        if (!pathQueue.TryPeek(out headLookAt))
            headLookAt = destination;

        headLookAt = Vector3.Lerp(playerTransform.position, headLookAt, 0.5f);

        float neck = 0.75f, headSpeed = 0.05f;
        Vector3 headTargetPos = (headLookAt - transform.position).normalized * neck;

        head.localPosition = Vector3.Lerp(head.localPosition, headTargetPos, headSpeed);
        
        headSprite.flipX = !(headLookAt.x > head.position.x);
    }

    void EyeBehaviour()
    {
        // For eye + pupil
        Vector3 distanceScale = new Vector3(0.1f, 0.2f);
        Vector3 offset = new Vector3(0, 0);

        Vector3 eyeLookAt = destination;
        Vector3 eyeTargetPosRaw = (eyeLookAt - head.position).normalized;
        
        // Applied distanceScale & offset
        Vector3 eyeTargetPos = new Vector3(eyeTargetPosRaw.x * distanceScale.x, eyeTargetPosRaw.y * distanceScale.y) + offset;

        float eyeSpeed = 0.05f;
        eye.localPosition = Vector3.Lerp(eye.localPosition, eyeTargetPos, eyeSpeed);
    }

    void LegsBehaviour()
    {
        foreach (var leg in legs)
        {
            if (leg.disabled)
            {
                // #0. Leg disabled
                leg.target = transform.position;
                leg.MoveToTargetPoint();
                leg.connected = false;
                
                //leg.transform.GetComponent<SpriteRenderer>().sortingOrder =
                //GetComponent<SpriteRenderer>().sortingOrder - 1;
                
                continue;
            }
            else
            {
                //leg.transform.GetComponent<SpriteRenderer>().sortingOrder =
                    //GetComponent<SpriteRenderer>().sortingOrder + 1;
            }
            
            if (leg.connected)
            {
                // #1. Leg connected to surface
                rb.AddForce(leg.Direction * leg.pullForce + leg.Direction * (leg.pullForce * Vector3.Distance(transform.position, leg.Position)));
                continue;
            }
            
            // #2. Leg not connected to surface
            leg.MoveToTargetPoint();
            float connectionThreshold = 0.1f;  // for Vec3.sqrMagnitude
            if (Vector3.SqrMagnitude(leg.target - leg.Position) < connectionThreshold)
            {
                leg.connected = true;
            }
        }
    }

    /// <summary>
    /// Resets the target of the least optimal leg
    /// Return codes are as follows:
    /// true: successful
    /// false: failed
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    bool SetLegTargetsMove(Vector3 target)
    {
        Vector3 targetDirection = (target - transform.position).normalized;
        Vector3 gravityForce = (rb.mass * rb.gravityScale * Physics2D.gravity);
        Vector3 netForce = gravityForce;
        legs.ForEach(leg => netForce += leg.connected ? (leg.Direction * leg.pullForce) : Vector3.zero);


        Vector3 netForceDirDeviation = targetDirection - netForce.normalized;
        
        const float DIRECTION_THRESHOLD = 0.25f;
        
        if (Vector3.SqrMagnitude(netForceDirDeviation) < DIRECTION_THRESHOLD)
        {
            // #1. netForce somewhat leads to target
            // return true;
        }
        
        // #2. netForce leads away from target
        MimicLeg leastOptimalLeg = legs[0];
        foreach (var leg in legs)
        {
            if(!leg.connected) continue;
            
            float thisDirectionDeviation = (targetDirection - leg.Direction).sqrMagnitude;
            float worstDirectionDeviation = (targetDirection - leastOptimalLeg.Direction).sqrMagnitude;
            if (thisDirectionDeviation > worstDirectionDeviation)
                leastOptimalLeg = leg;
        }
        
        // Set new target for least optimal leg
        float thetaInterval = 3.14f / 32f;
        float theta = 0f;
        float thetaDir = 1f;

        RaycastHit2D hit = new RaycastHit2D();

        do
        {
            // (netForce + F).normalized = targetDirection
            Vector3 newLegDir = targetDirection + netForceDirDeviation * 0.5f;

            float newLegDirRad = Mathf.Atan2(newLegDir.y, newLegDir.x) + theta;
            newLegDir.x = Mathf.Cos(newLegDirRad);
            newLegDir.y = Mathf.Sin(newLegDirRad);
            
            hit = Physics2D.Raycast(transform.position, newLegDir, 10f, wallLayers);
            theta = (Mathf.Abs(theta) + thetaInterval) * thetaDir;
            thetaDir *= -1f;
        } while (hit.collider == null && Mathf.Abs(theta) < Mathf.PI);

        if (hit.collider == null)
        {
            // target point not found
            return false;
        }

        leastOptimalLeg.target = hit.point;
        leastOptimalLeg.connected = false;
        return true;
    }

    void SetLegTargetsStay()
    {
        Vector3[] connectionPoints = new Vector3[numLegs];
        float theta = 0f;
        for (int i = 0; i < numLegs; i++)
        {
            float alphaInterval = 3.14f / 16f;
            float alpha = 0f;
            float alphaDir = 1f;

            RaycastHit2D hit = new RaycastHit2D();

            while (hit.collider == null && Mathf.Abs(alpha) < Mathf.PI * 2f)
            {
                Vector2 dir = new Vector2(Mathf.Cos(theta + alpha), Mathf.Sin(theta + alpha));
                hit = Physics2D.Raycast(transform.position, dir, 10f, wallLayers);

                alpha = (Mathf.Abs(alpha) + alphaInterval) * alphaDir;
                alphaDir *= -1f;
            }

            if (hit.collider == null)
            {
                legs[i].disabled = true;
                continue;
            }

            legs[i].target = hit.point;

            theta += Mathf.PI * 2f / numLegs;
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
            Vector2[] nodePath = 
                pfManager.GetDijkstraPath(nearestNode, nearestDestinationNode, debug:true).Select(node => node.position).ToArray();

            if (Vector3.Distance(nodePath[0], nodePath[1]) >
                Vector3.Distance(transform.position, nodePath[1]))
                nodePath = nodePath.Skip(1).ToArray();
            
            path.AddRange(grid.GetAStarPath(transform.position, nodePath[0]));
            
            for (int i = 0; i < nodePath.Length - 1; i++)
            {
                Vector3[] pathChunk = grid.GetAStarPath(nodePath[i], nodePath[i + 1],
                    wCost: 10, preventCornerCutting: true);
            
                path.AddRange(pathChunk);
            }
            
            path.AddRange(grid.GetAStarPath(path.Last(), target));
        }
        else
        {
            // #2. start node == end node
            path = grid.GetAStarPath(transform.position, target).ToList();
        }
        
        // Process path to only include key tiles around turns instead of the entire path
        /*
         *  --------
         *          \
         *           \
         *            \
         *             ------->
         *  would now be
         *
         *  -       -
         *           \
         *
         *             \
         *              -    ->
         */

        if (path.Count == 0)
        {
            // Don't do enqueues if path is empty
            return;
        }
        
        List<Vector3> pathProcessed = new List<Vector3> {path[0]};
        for (int i = 1; i < path.Count-1; i++)
        {
            var delta1 = path[i] - path[i - 1];
            var delta2 = path[i + 1] - path[i];

            if ((delta2 - delta1).sqrMagnitude > 0.01f)
            {
                pathProcessed.Add(path[i]);
            }
        }
        pathProcessed.Add(path.Last());
        // pathProcessed.ForEach(tile => print(tile));

        // Head straight towards 1st node if it's closer than 0st node
        
        pathProcessed.ForEach(tile => pathQueue.Enqueue(tile));
    }
    
    // Variables for STATE: Search
    [System.Serializable]
    public struct MimicSearchState
    {
        public float refreshPathClock;
        [HideInInspector] public float refreshPathClockTimer;
        [HideInInspector] public bool atDestination;

        public float setLegTargetClock;
        [HideInInspector] public float setLegTargetClockTimer;
    }

    [Header("Variables for State: Search")]
    public MimicSearchState searchState;
    
    void STATE_Search()
    {
        void ResetDestination()
        {
            destination = playerTransform.position;
            searchState.atDestination = false;
        }
        
        
        if (searchState.refreshPathClockTimer <= 0f)
        {
            searchState.refreshPathClockTimer = searchState.refreshPathClock;
            ResetDestination();
            EnqueuePath(playerTransform.position, true);
        }

        Vector3 target;
        searchState.atDestination = !pathQueue.TryPeek(out target);
        // #1. Destination reached (path queue empty)
        if (searchState.atDestination)
        {
            // print("Destination Reached");
        }
        // #2. Destination not reached
        else
        {
            Vector3 direction = target - transform.position;
            // rb.velocity = direction.normalized * 4f; // Placeholder

            if (searchState.setLegTargetClockTimer <= 0f)
            {
                searchState.setLegTargetClockTimer = searchState.setLegTargetClock;
                SetLegTargetsMove(target);
            }
            
            if (direction.sqrMagnitude < 0.5f)
            {
                pathQueue.Dequeue();
            }
        }
        
        // Update searchState
        searchState.refreshPathClockTimer -= Time.fixedDeltaTime;
        searchState.setLegTargetClockTimer -= Time.fixedDeltaTime;

    }
}
