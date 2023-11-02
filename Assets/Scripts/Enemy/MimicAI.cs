using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
    public readonly float force;
    
    public MimicLeg(bool isSmall, Transform transform, Transform bodyTransform, float speed, float force)
    {
        this.isSmall = isSmall;
        this.transform = transform;
        this.bodyTransform = bodyTransform;
        this.speed = speed;
        this.force = force;
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


[BurstCompile]
public class MimicAI : MonoBehaviour
{
    private Rigidbody2D rb;

    private PFManager pfManager;
    private Transform playerTransform;

    public Transform gizmo;

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
    [SerializeField] private float smallLegForce, legForce;
    [SerializeField] private LayerMask wallLayers;

    [Header("\nAI")] 
    public PFGrid grid;
    public MimicState state;
    public Vector3 destination;
    private Vector3 target;
    private Queue<Vector3> pathQueue;
    
    [SerializeField] private LayerMask sensorLayers;
    private bool playerInSight;
    private Vector3 probablePlayerPos;

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
                thisNode = new MimicLeg(true, thisNodeObj.transform, transform, 0.25f, smallLegForce);
            }
            else
            {
                /* Medium Node
                - Medium max tension/push
                - Medium range
                */
                GameObject thisNodeObj = Instantiate(legObj, pos, Quaternion.identity);
                thisNode = new MimicLeg(false, thisNodeObj.transform, transform, 0.05f, legForce);
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
        state = MimicState.Idle;
        playerTransform = GM.GetPlayer().transform;
        pathQueue = new Queue<Vector3>();
        SetLegTargetsStay();

        moveVars.Reset();
        idleState.Reset();
        chaseState.Reset();
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case MimicState.Sleep:
                break;
            case MimicState.Idle:
                STATE_Idle();
                break;
            case MimicState.Search:
                //STATE_Search();
                break;
            case MimicState.Chase:
                STATE_Chase();
                break;
        }
        
        Sight();
        
        HeadBehaviour();
        EyeBehaviour();
        LegsBehaviour();

        gizmo.position = probablePlayerPos;
    }

    void PlayerSightEnter()
    {
        playerInSight = true;
        probablePlayerPos = playerTransform.position;
        
        // Change STATE to Chase
        state = MimicState.Chase;
        chaseState.Reset();
    }

    void PlayerSightExit()
    {
        playerInSight = false;
    }
    
    void Sight()
    {
        /*
        int rayCount = 13;
        float range = 20f;
        float deltaAngle = Mathf.PI / 12f;
        
        // RaycastHit2D[] hits = new RaycastHit2D[rayCount];

        float eyeAngle = Mathf.Atan2(eye.position.y, eye.position.x);
        bool playerInSightNow = false;  // Used to determine whether the player is in sight in this Sight() call
        for (int i = 0; i < rayCount; i++)
        {
            float thisRayAngle = eyeAngle + deltaAngle * (-(rayCount - 1) / 2f + i);
            Vector2 dir = new Vector2(Mathf.Cos(thisRayAngle), Mathf.Sin(thisRayAngle));
            var hit = Physics2D.Raycast(eye.position, dir, range, sensorLayers);
            Debug.DrawRay(eye.position, dir);
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                if(!playerInSight)
                    PlayerSightEnter();

                if (!playerInSightNow)
                {
                    playerInSightNow = true;
                    
                    // Update AI's player position
                    probablePlayerPos = playerTransform.position;
                }
            }
        }

        if (!playerInSightNow && playerInSight)
        {
            PlayerSightExit();
        }
        */
        
        // Theta disabled for now

        var eyePosition = eye.position;
        var playerPosition = playerTransform.position;
        // Vector3 eyeDir = eyePosition.normalized;
        Vector3 playerDir = (playerPosition - eyePosition).normalized;
        
        
        // float theta = Mathf.Acos(Vector3.Dot(eyeDir, playerDir));

        // float maxTheta = 2 * Mathf.PI / 3f;
        float maxDist = 15f;
        float dist = Vector3.Distance(eyePosition, playerPosition);
        RaycastHit2D hit = Physics2D.Raycast(eyePosition, playerDir, dist, wallLayers);
        // print((theta >= maxTheta) + " " + (dist > maxDist) + " " + (hit.collider != null));
        // print(theta);
        if (dist > maxDist || hit.collider != null) // theta >= maxTheta ||  
        {
            if (playerInSight)
            {
                playerInSight = false;
                PlayerSightExit();
            }
        }
        else if(!playerInSight)
        {
            playerInSight = true;
            PlayerSightEnter();
        }
    }
    
    void HeadBehaviour()
    {
        Vector3 headLookAt;
        if (playerInSight)
            headLookAt = probablePlayerPos;
        else if (!pathQueue.TryPeek(out headLookAt))
            headLookAt = destination;

        headLookAt = Vector3.Lerp(destination, headLookAt, 0.5f);

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

        float eyeSpeed = 0.1f;
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
                
                continue;
            }
            
            if (leg.connected)
            {
                // #1. Leg connected to surface
                Vector2 force = leg.Direction * leg.force;

                force *= Vector2.Dot(force, target - transform.position) > 0f
                    ? 1f
                    : -1f;
                
                rb.AddForce(force);
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
        legs.ForEach(leg => netForce += leg.connected ? (leg.Direction * leg.force) : Vector3.zero);


        float dotProduct = Vector3.Dot(targetDirection,netForce.normalized);

        const float minDot = 1.7f / 2f;  // roughly cos(30deg)
        
        if (Mathf.Abs(dotProduct) > minDot)
        {
            // #1. netForce somewhat leads to target
            // return true;
        }
        
        // #2. netForce leads away from target
        MimicLeg leastOptimalLeg = legs[0];
        float leastOptimalDot = 1f;
        foreach (var leg in legs)
        {
            if(!leg.connected) continue;
            /*
            float thisDirectionDeviation = (targetDirection - leg.Direction).sqrMagnitude;
            float worstDirectionDeviation = (targetDirection - leastOptimalLeg.Direction).sqrMagnitude;
            if (thisDirectionDeviation > worstDirectionDeviation)
                leastOptimalLeg = leg;
                */
            float thisDot = Vector3.Dot(targetDirection, leg.Direction);
            if (Mathf.Abs(leastOptimalDot) > Mathf.Abs(thisDot))
            {
                leastOptimalLeg = leg;
                leastOptimalDot = thisDot;
            }
        }
        
        // Set new target for least optimal leg
        float thetaInterval = 3.14f / 32f;
        float theta = 0f;
        float thetaDir = 1f;

        RaycastHit2D hit = new RaycastHit2D();

        do
        {
            // (netForce + F).normalized = targetDirection
            Vector3 newLegDir = 
                Random.value > 0.5f
                ? -targetDirection
                : targetDirection;

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

    private Vector3 stasisPosition;
    void SetLegTargetsStay()
    {
        Vector3[] connectionPoints = new Vector3[numLegs];
        target = stasisPosition;
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
        PFNode[] nodes = pfManager.pfGraph.nodes;
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
        
        List<Vector3> path = new List<Vector3>();
        PFNode nearestNode = GetNearestNode(transform.position);
        PFNode nearestDestinationNode = GetNearestNode(target);
        // Head straight to target with A* if it's visible and within range
        float closeEnoughRange = 12f;
        if (Physics2D.Raycast(transform.position, target - transform.position, closeEnoughRange, wallLayers).collider ==
            null)
        {
            // #0. target visible from current position
            path = grid.GetAStarPath(transform.position, target).ToList();
        }
        else if(nearestNode != nearestDestinationNode)
        {
            // #1. start node != end node
            Vector2[] nodePath = 
                pfManager.pfGraph.GetDijkstraPath(nearestNode, nearestDestinationNode, debug:true).Select(node => node.position).ToArray();

            if (nodePath.Length == 0)
            {
                Debug.LogWarning("Error in graph pathfinding :(");
                return;
            }
            
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

       
        
        pathProcessed.ForEach(tile => pathQueue.Enqueue(tile));
    }
    // ------------------------------------------------------------------------------------------------
    // Variables for STATE: Search
    [System.Serializable]
    public struct MimicMoveVariables
    {
        public float refreshPathClock;
        [HideInInspector] public float refreshPathClockTimer;

        public float setLegTargetClock;
        [HideInInspector] public float setLegTargetClockTimer;

        public void Reset()  // I can apparently add functions to structs in C# lmfao
        {
            refreshPathClockTimer = refreshPathClock;
            setLegTargetClockTimer = setLegTargetClock;
        }
    }

    [Header("Variables for moving from point A to B")]
    public MimicMoveVariables moveVars;
    
    float MoveToDestination(Vector3 thisDestination)  // Returns distance to destination
    {
        if (moveVars.refreshPathClockTimer <= 0f)
        {
            moveVars.refreshPathClockTimer = moveVars.refreshPathClock;
            EnqueuePath(thisDestination, true);
        }

        target = thisDestination;
        if(pathQueue.TryPeek(out target))
        {
            Vector3 direction = target - transform.position;
            // rb.velocity = direction.normalized * 4f; // Placeholder

            if (moveVars.setLegTargetClockTimer <= 0f)
            {
                moveVars.setLegTargetClockTimer = moveVars.setLegTargetClock;
                SetLegTargetsMove(target);
            }
        
            if (direction.sqrMagnitude < 0.5f)
            {
                pathQueue.Dequeue();
            }
        }
   
        // Update moveVars
        moveVars.refreshPathClockTimer -= Time.fixedDeltaTime;
        moveVars.setLegTargetClockTimer -= Time.fixedDeltaTime;

        return Vector3.Distance(thisDestination, transform.position);
    }
    // ------------------------------------------------------------------------------------------------
    // Preserved variables for STATE: Idle
    [System.Serializable]
    public struct MimicIdleState
    {
        public float stasisClockMax, stasisClockMin;
        [HideInInspector] public float stasisClock;
        public float timeOut;
        [HideInInspector] public float timeOutTimer;

        [HideInInspector] public Vector3 currentDestination;

        public bool isStatic;

        public void Reset()
        {
            stasisClock = Random.Range(stasisClockMin, stasisClockMax);
            timeOutTimer = timeOut;
            currentDestination = Vector3.zero;
            isStatic = false;
        }
    }

    void EnterIdleState()
    {
        idleState.Reset();
        state = MimicState.Idle;
    }
    
    [SerializeField] private MimicIdleState idleState;
    
    public void STATE_Idle()
    {
        Vector3 GetRandomDestination()
        {
            float pi = Mathf.PI;

            float rayLength = 10f;
            
            int rayCount = Random.Range(3, 7);
            Vector3[] candidates = new Vector3[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                float angle = Random.Range(-pi / 3, pi / 3) + (Random.value >= 0.5f ? 1f : 0f) * pi;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, rayLength, wallLayers);
                
                candidates[i] += hit.collider != null
                    ? hit.point + ((Vector2)transform.position - hit.point).normalized * 1f
                    : (Vector3)hit.point;
            }
    
            return candidates[Random.Range(0, rayCount)];
        }
        
        if (idleState is { stasisClock: > 0f, isStatic: false })   // WHAT
        {
            stasisPosition = transform.position;
            SetLegTargetsStay();
            idleState.isStatic = true;
        }
        else if (idleState is { stasisClock: <= 0f, isStatic: true })
        {
            idleState.isStatic = false;
            idleState.currentDestination = GetRandomDestination();
            destination = idleState.currentDestination;
        }
        else if (idleState is { stasisClock: <= 0f, isStatic: false })
        {
            float dist = MoveToDestination(idleState.currentDestination);
            if (dist <= 2f)
            {
                idleState.stasisClock = Random.Range(idleState.stasisClockMin, idleState.stasisClockMax);
            }
        }
        else
        {
            idleState.stasisClock -= Time.fixedDeltaTime;
        }

        idleState.timeOutTimer -= Time.fixedDeltaTime;
        if (idleState is { timeOutTimer: <= 0f, isStatic: false })
        {
            idleState.Reset();
        }
    }
    
    // ------------------------------------------------------------------------------------------------
    // Chase
    [System.Serializable]
    struct MimicChaseState
    {
        public float timeOut;
        
        public List<Vector3> playerPosHistory;
        public float timeSinceSightExit;
        public Vector3 finalPlayerSighting;
        
        public void Reset()
        {
            var playerPos = GM.GetPlayerPosition();
            int cap = 60;
            playerPosHistory = new List<Vector3>(cap);
            for (int i = 0; i < cap; i++)
                playerPosHistory.Add(playerPos);
            timeSinceSightExit = 0f;
            finalPlayerSighting = playerPos;
        }

        public void UpdatePlayerPosHistory()
        {
            playerPosHistory.RemoveAt(0);
            playerPosHistory.Add(GM.GetPlayerPosition());
        }
    }

    [SerializeField] private MimicChaseState chaseState;
    
    void STATE_Chase()
    {
        Vector3 GuessPlayerPosition()
        {
            Vector3 generalDir = chaseState.playerPosHistory.Last() - chaseState.playerPosHistory.First();
            float t = chaseState.timeSinceSightExit;
            float fadeT = Mathf.Sqrt(t+1) - 1f;
            
            Vector3 v = generalDir / (Time.fixedDeltaTime * chaseState.playerPosHistory.Count);

            RaycastHit2D hit = Physics2D.Raycast(chaseState.finalPlayerSighting, generalDir, 
                fadeT * v.magnitude, wallLayers);
            if (hit.collider == null)
            {
                return fadeT * v + chaseState.finalPlayerSighting;
            }
            
            return hit.point + ((Vector2)transform.position - hit.point).normalized * 0.25f;
        }
        
        
        
        if (playerInSight)
        {
            chaseState.UpdatePlayerPosHistory();
            probablePlayerPos = playerTransform.position;
            chaseState.finalPlayerSighting = probablePlayerPos;
            chaseState.timeSinceSightExit = 0f;
        }
        else
        {
            chaseState.timeSinceSightExit += Time.fixedDeltaTime;
            if (chaseState.timeSinceSightExit > chaseState.timeOut)
            {
                EnterIdleState();
                chaseState.Reset();
            }
            probablePlayerPos = GuessPlayerPosition();
        }
        
        destination = probablePlayerPos;
        
        MoveToDestination(destination);
    }
}
