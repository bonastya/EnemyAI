using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MobAI : MonoBehaviour
{

    [Header("Search way points")]
    public Transform[] allWps;

    [Header("The point for Linecast for obstacles (on the head of the mob)")]
    public Transform visionPoint;

    [Header("Layers for detect visibility (what is obstacle)")]
    public LayerMask obstacleLayerMask;

    [Header("Player's vision layer")]
    public LayerMask playerVisionMask;

    [Header("Point where the player was last seen")]
    public Transform playerLastPointTrig;

    [Header("Point where the mob goes (visualization)")]
    public Transform targetPointVis;

    [Header("The speed of the mob in search mode")]
    public float seekSpeed=1f;

    [Header("The speed of the mob in hiding mode")]
    public float hideSpeed=4f;

    [HideInInspector] public PlayerControl playerControl;
    [HideInInspector] public GameObject player;
    [HideInInspector] public bool playerOnVisionTrig;

    [HideInInspector] public MovementMode movementMode; 
    [HideInInspector] public Transform target;

    Transform playerCamera;
    NavMeshAgent agent;
    Animator mobAnimator;

    [HideInInspector] public GameObject currentWaypointtrig;
    bool isInPlayerVision = false;
    PlayerControl.GamePlayMode mobPlayMode;


    Collider[] obstaclesColliders = new Collider[50];

    [Header("Sphere in which find obstacles to hide")]
    public SphereCollider obstacleSearchRadius;

    [Header("The point of the found shelter")]
    public Transform safePoint;

    private Coroutine SafePointCor;
    private Coroutine WayPointCor;

    bool ifFoundCollider = false;

    public enum MovementMode
    {
        GoToPlayer,
        CheckWayPoints,
        GoToLastPoint,
        HideFromPlayer,
        WaitInCover
    }

    [Header("Duration of the pause on Waypoint")]
    public float moveDelayMin = 3f;
    public float moveDelayMax = 5f;


    private bool mobIsInSafePoint = false;

    [Header("Attack Screen")]
    public GameObject AttackPannel;

    bool seesPlayer;
    Coroutine damageCor;


    void Start()
    {
        target = transform;
        player = GameObject.FindWithTag("Player");
        playerCamera = GameObject.FindWithTag("MainCamera").transform;
        mobAnimator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentWaypointtrig = allWps[1].gameObject;

        playerControl = FindObjectOfType<PlayerControl>();

        print("playerControl.playMode " + playerControl.playMode);
        if (playerControl.playMode == PlayerControl.GamePlayMode.PlayerHide)
        {
            print("GoToNextWP");
            StartGoToNextWPCor(0);
        }
        else if (playerControl.playMode == PlayerControl.GamePlayMode.PlayerSeek)
        {
            CheckIfInVisionOfPlayer();
        }

        StartCoroutine("SetDestinationCor");

    }


    private void Update()
    {
        //checking that mob is in player's vision zone
        if (mobPlayMode == PlayerControl.GamePlayMode.PlayerSeek)
        {
            CheckIfInVisionOfPlayer();
        }
    }


    public void ChangePlayMode(PlayerControl.GamePlayMode playMode)
    {
        if (playMode == PlayerControl.GamePlayMode.PlayerHide)
        {
            mobPlayMode = playMode;
            agent.speed = seekSpeed;

        }
        else if (playMode == PlayerControl.GamePlayMode.PlayerSeek)
        {
            mobPlayMode = playMode;
            agent.speed = hideSpeed;
            StopCoroutine("RayOnPlayerCor");

        }
    }

    //Used for both game modes
    //target indicates the current target for Navagent
    IEnumerator SetDestinationCor()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            agent.SetDestination(target.position);
            targetPointVis.position = target.position;

        }
    }


    #region player hide mode

    //checking for obstacles between the player and the mob
    IEnumerator RayOnPlayerCor()
    {
        RaycastHit str;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            //if in the vision zone of the mob
            if (playerOnVisionTrig)
            {
                Debug.DrawLine(visionPoint.position, playerCamera.position, Color.red);
                //if there are no obstacles
                if (!Physics.Linecast(visionPoint.position, playerCamera.position, out str, obstacleLayerMask))
                {
                    if (movementMode != MovementMode.GoToPlayer)
                        SeesPlayer();
                }
                else if (movementMode == MovementMode.GoToPlayer)
                {
                    GoToPlayerLastPoint();
                }
            }
        }
    }



    public void SeesPlayer()
    {

        if (WayPointCor != null)
            StopCoroutine(WayPointCor);

        target = player.transform;
        movementMode = MovementMode.GoToPlayer;
        Move();
        print("Mob goes to the player");

    }

    public void GoToPlayerLastPoint()
    {



        playerLastPointTrig.position = player.transform.position;

        NavMeshHit navPoint;
        //the nearest NavMesh position 
        if (NavMesh.SamplePosition(playerLastPointTrig.position, out navPoint, 100f, agent.areaMask))
        {
            playerLastPointTrig.position = navPoint.position;
            target = playerLastPointTrig;
            movementMode = MovementMode.GoToLastPoint;
            Move();
            print("Mob goes to the last point where it saw the player");
        }
        else
        {
            print("can not find nearest NavMesh position");
            StartGoToNextWPCor(Random.Range(moveDelayMin, moveDelayMax));
        }


    }

    //starts when in WayPoint
    public void StartGoToNextWPCor(float delay)
    {
        WayPointCor = StartCoroutine(GoToNextWPCor(delay));
    }

    //delay at WayPoint
    IEnumerator GoToNextWPCor(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Transform newWP = currentWaypointtrig.transform;
        //select a random point but not the current one
        while (newWP == currentWaypointtrig.transform)
            newWP = allWps[Random.Range(0, allWps.Length)].transform;

        print("Mob goes to  " + newWP);

        target = newWP;
        movementMode = MovementMode.CheckWayPoints;
        Move();
        WayPointCor = null;
    }


    //When mob came to WayPoint
    public void Stop()
    {
        movementMode = MovementMode.CheckWayPoints;
        agent.isStopped = true;
        mobAnimator.SetBool("isWalk", false);

    }

    #endregion


    #region player seek mode


    private void CheckIfInVisionOfPlayer()
    {
        //if point is in player's vision zone
        Collider[] hitColliders = Physics.OverlapSphere(visionPoint.position, 0f, playerVisionMask);
        if (hitColliders.Length > 0)
        {
            //checking that mob was not in the trigger before
            if (!isInPlayerVision)
            {
                isInPlayerVision = true;
                Debug.Log("Colliding with: " + hitColliders[0]);
                StartCoroutine("IsHideFromPlayerRayCor");
            }
        }
        else
        {
            if (isInPlayerVision)
            {
                isInPlayerVision = false;
                Debug.Log("Not colliding");
                StopCoroutine("IsHideFromPlayerRayCor");

            }
        }
    }

    //Checking that there are no obstacles in the line of sight with the player
    IEnumerator IsHideFromPlayerRayCor()
    {
        RaycastHit str;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            Debug.DrawLine(visionPoint.position, playerCamera.position, Color.green);

            if (!Physics.Linecast(visionPoint.position, playerCamera.position, out str, obstacleLayerMask))
            {

                if (movementMode != MovementMode.HideFromPlayer)
                    HideFromPlayer();

            }



        }
    }


    //by the start position and gameObject returns the position of icollision with the collider from the reverse side

    public RaycastHit HitBackSidePosition(Vector3 startPosition, GameObject coliderObject)
    {

        Vector3 direction = (coliderObject.transform.position - startPosition);
        Vector3 oneHeightdirection = new Vector3(direction.x, startPosition.y, direction.z);

        RaycastHit[] hits;

        hits = Physics.RaycastAll(startPosition, oneHeightdirection, Mathf.Infinity);

        RaycastHit firstSideHit = new RaycastHit();

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.gameObject == coliderObject)
            {
                firstSideHit = hit;

                Vector3 offsetPoint = startPosition + (100f * oneHeightdirection);
                Ray firstRay = new Ray(startPosition, oneHeightdirection);

                Vector3 reverseOrigin = firstRay.origin + (firstRay.direction * 100f);
                RaycastHit reverseHit;
                Ray reverseRay = new Ray(reverseOrigin, (firstRay.direction * -1));
                firstSideHit.collider.Raycast(reverseRay, out reverseHit, 100f);

                Debug.DrawLine(offsetPoint, reverseHit.point, Color.red, 4f);
                ifFoundCollider = true;

                return reverseHit;

            }
        }
        print("Couldn't find the point behind the object" + coliderObject);
        ifFoundCollider = false;
        return new RaycastHit();

    }


    //Finding and checking the point where can hide
    public void HideFromPlayer()
    {
        Vector3 playerCameraPosition = playerCamera.position;
        print("HideFromPlayer");
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);

        //colliders in search radius of the mob
        int colliders = Physics.OverlapSphereNonAlloc(visionPoint.position, obstacleSearchRadius.radius, obstaclesColliders, obstacleLayerMask);

        for (int i = 0; i < colliders; i++)
        {
            //position behind the collider
            RaycastHit raycastHit = HitBackSidePosition(playerCameraPosition, obstaclesColliders[i].gameObject);

            //direction from the player to the assumed safe position
            Vector3 direction = obstaclesColliders[i].gameObject.transform.position - playerCameraPosition;

            if (ifFoundCollider)
            {
                NavMeshHit navPoint;
                //the nearest NavMesh position 
                if (NavMesh.SamplePosition(raycastHit.transform.position, out navPoint, 10f, agent.areaMask))
                {
                    //the nearest edge of navMesh
                    if (!NavMesh.FindClosestEdge(navPoint.position, out navPoint, agent.areaMask))
                    {
                        print("can not FindClosestEdge");
                    }

                    //perpendicular offset from the NavMesh edge
                    Vector3 normalDirection = navPoint.normal;
                    NavMeshHit offsetNavPoint;
                    Vector3 position1 = navPoint.position + normalDirection * 0.2f;


                    //the closest NavMesh position to the new point
                    if (NavMesh.SamplePosition(position1, out offsetNavPoint, 10f, agent.areaMask))
                    {
                        //checking that new point is not in line of sight with player
                        if (Physics.Linecast(playerCameraPosition, new Vector3(offsetNavPoint.position.x, playerCameraPosition.y, offsetNavPoint.position.z), obstacleLayerMask))
                        {
                            safePoint.position = offsetNavPoint.position;
                            target = safePoint;

                            movementMode = MovementMode.HideFromPlayer;
                            Move();
                            print("Mob goes to the obstacle " + raycastHit.transform.gameObject);
                            SafePointCor = StartCoroutine(IfSafePointStillSafe(safePoint.position));

                            //if the point is too close to the mob
                            if (mobIsInSafePoint)
                            {
                                StartCoroutine(WaitWhenGoToNearPoint());
                            }
                            return;
                        }

                    }

                }

            }

        }

    }



    //checking that the selected point for hiding the mob is still not visible to the player
    IEnumerator IfSafePointStillSafe(Vector3 safePosition)
    {
        while (Physics.Linecast(playerCamera.position, new Vector3(safePosition.x, playerCamera.position.y, safePosition.z), obstacleLayerMask))
        {
            yield return new WaitForSeconds(0.1f);
        }
        if (movementMode == MovementMode.HideFromPlayer)
            HideFromPlayer();
        SafePointCor = null;

    }

    //delay if the hiding point appears too close to the mob
    IEnumerator WaitWhenGoToNearPoint()
    {
        yield return new WaitForSeconds(0.3f);
        WaitInCover();
    }


    //When the mob came to the safe point
    public void WaitInCover()
    {
        movementMode = MovementMode.WaitInCover;
        agent.isStopped = true;
        mobAnimator.SetBool("isWalk", false);
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);

    }


    #endregion



    //walk animation
    public void Move()
    {

        mobAnimator.SetBool("isWalk", true);
        agent.isStopped = false;

    }

    #region waypoint movement, safe point logic, attack

    private void OnTriggerEnter(Collider colider)
    {
        if (colider.gameObject.tag == "WayPoint")
        {
            if (target == colider.gameObject.transform)
            {
                
                Stop();
                currentWaypointtrig = colider.gameObject;
                StartGoToNextWPCor(Random.Range(moveDelayMin, moveDelayMax));
            }
        }


        if (colider.gameObject.tag == "SafePoint")
        {
            if (target == colider.gameObject.transform)
            {
                WaitInCover();
            }
            mobIsInSafePoint = true;
        }


        if (playerControl.playMode == PlayerControl.GamePlayMode.PlayerHide)
        {
            if (colider.gameObject.tag == "PlayerBody")
            {
                AttackPannel.SetActive(true);

                if (damageCor != null)
                    StopCoroutine(damageCor);

                seesPlayer = true;
                damageCor = StartCoroutine(DamageCor());

            }
        }


    }


    public Collider col;

    private void OnTriggerExit(Collider colider)
    {
        if (colider.gameObject.tag == "SafePoint")
        {
            mobIsInSafePoint = false;
        }

        if (playerControl.playMode == PlayerControl.GamePlayMode.PlayerHide)
        {
            if (colider.gameObject.tag == "PlayerBody")
            {
                seesPlayer = false;
                AttackPannel.SetActive(false);
            }
        }

    }

    //Player takes 1 damage in 3 seconds
    IEnumerator DamageCor()
    {
        while (seesPlayer)
        {
            if (movementMode == MobAI.MovementMode.GoToPlayer)
            {
                playerControl.HealthDecrease();

                yield return new WaitForSeconds(3f);

            }
        }

        damageCor = null;
    }


    #endregion


}

