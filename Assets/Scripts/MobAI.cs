using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MobAI : MonoBehaviour
{

    [Header("����� ������")]
    public Transform[] allWps;

    [Header("����� ������ ��������� ��� �� ����� ����������� (�� ������ ����)")]
    public Transform visionPoint;

    [Header("���� ��� ����������� ��������� (��� �����������)")]
    public LayerMask obstacleLayerMask;

    [Header("���� ������ ������")]
    public LayerMask playerVisionMask;

    [Header("����� ��� ��������� ��� ����� ������")]
    public Transform playerLastPointTrig;

    [Header("����� ���� ��� ��� (������������)")]
    public Transform targetPointVis;

    [Header("�������� ���� ��� ������ ������")]
    public float seekSpeed=1f;

    [Header("�������� ���� ��� ������ ���������")]
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


    Collider[] obstaclesColliders = new Collider[10];

    [Header("����� � ������� ����� ������ �������")]
    public SphereCollider obstacleSearchRadius;

    [Header("����� ���������� �������")]
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

    [Header("������������ ����� �� WayPoint")]
    public float moveDelayMin = 3f;
    public float moveDelayMax = 5f;


    private bool mobIsInSafePoint = false;

    [Header("����� �����")]
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
        //�������� ��� ��� ��������� � ���� ��������� ������
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

    //������������ ��� ����� ������� ����
    //target ��������� �� ���������� ���� ��� Navagent
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

    //�������� ���� �� ����������� ����� ������� � �����
    IEnumerator RayOnPlayerCor()
    {
        RaycastHit str;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            //���� � ������� ��������� ����
            if (playerOnVisionTrig)
            {
                Debug.DrawLine(visionPoint.position, playerCamera.position, Color.red);
                //���� ��� �����������
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
        print("��� ��� � ������");

    }

    public void GoToPlayerLastPoint()
    {
        playerLastPointTrig.position = player.transform.position;
        target = playerLastPointTrig;
        movementMode = MovementMode.GoToLastPoint;
        Move();
        print("��� ��� � ��������� ����� ��� ����� ������");


    }

    //����������� ��� ���������� WayPoint
    public void StartGoToNextWPCor(float delay)
    {
        WayPointCor = StartCoroutine(GoToNextWPCor(delay));
    }

    //�������� �� WayPoint
    IEnumerator GoToNextWPCor(float delay)
    {
        yield return new WaitForSeconds(delay);
        GoToNextWP();
    }
    public void GoToNextWP()
    {
        Transform newWP = currentWaypointtrig.transform;
        //������� ��������� ����� ����� �������
        while (newWP == currentWaypointtrig.transform)
            newWP = allWps[Random.Range(0, allWps.Length)].transform;

        print("��� ��� �  " + newWP);

        target = newWP;
        movementMode = MovementMode.CheckWayPoints;
        Move();
        WayPointCor = null;

    }

    //����� ��� ����� �� WayPoint
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
        //������������ �� ����� � ����� ��������� ������
        Collider[] hitColliders = Physics.OverlapSphere(visionPoint.position, 0f, playerVisionMask);
        if (hitColliders.Length > 0)
        {
            //�������� ��� ��� �� ��� � �������� �� �����
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

    //�������� ��� �� ����� ��������� � ������� (��� �����������)
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


    //�� ��������� ������� � ������� ���������� ������� ����� � ��������� ������� � �������� �������

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
        print("�� ������� ����� ����� �� ��������" + coliderObject);
        ifFoundCollider = false;
        return new RaycastHit();

    }


    //����� � �������� ����� ��� ����� ����������
    public void HideFromPlayer()
    {
        Vector3 playerCameraPosition = playerCamera.position;
        print("HideFromPlayer");
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);

        //���������� � ������� ������ ����
        int colliders = Physics.OverlapSphereNonAlloc(visionPoint.position, obstacleSearchRadius.radius, obstaclesColliders, obstacleLayerMask);

        for (int i = 0; i < colliders; i++)
        {
            //������� �� �����������
            RaycastHit raycastHit = HitBackSidePosition(playerCameraPosition, obstaclesColliders[i].gameObject);

            //������ ����������� �� ������ � �������������� ���������� �������
            Vector3 direction = obstaclesColliders[i].gameObject.transform.position - playerCameraPosition;

            if (ifFoundCollider)
            {
                NavMeshHit navPoint;
                //��������� ������� NavMesh 
                if (NavMesh.SamplePosition(raycastHit.transform.position, out navPoint, 10f, agent.areaMask))
                {
                    //��������� ����� navMesh
                    if (!NavMesh.FindClosestEdge(navPoint.position, out navPoint, agent.areaMask))
                    {
                        print("can not FindClosestEdge");
                    }

                    //������ �� ���� NavMesh ��������������� �����
                    Vector3 normalDirection = navPoint.normal;
                    NavMeshHit offsetNavPoint;
                    Vector3 position1 = navPoint.position + normalDirection * 0.2f;


                    //��������� ������� NavMesh � ����� �����
                    if (NavMesh.SamplePosition(position1, out offsetNavPoint, 10f, agent.areaMask))
                    {
                        //�������� ��� ����� ����� �� ��������� �� ����� ������ ��������� � �������
                        if (Physics.Linecast(playerCameraPosition, new Vector3(offsetNavPoint.position.x, playerCameraPosition.y, offsetNavPoint.position.z), obstacleLayerMask))
                        {
                            safePoint.position = offsetNavPoint.position;
                            target = safePoint;

                            movementMode = MovementMode.HideFromPlayer;
                            Move();
                            print("��� ��� � �������" + raycastHit.transform.gameObject);
                            SafePointCor = StartCoroutine(IfSafePointStillSafe(safePoint.position));

                            //���� ����� ������� ������ � ����
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



    //�������� ��� ��������� ����� ��� ������� ���� �� ��� �� ����� ������
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

    //�������� ���� ����� ��� �������� ��������� ������� ������ � ����
    IEnumerator WaitWhenGoToNearPoint()
    {
        yield return new WaitForSeconds(0.3f);
        WaitInCover();
    }


    //����� ��� ������ �������� �����
    public void WaitInCover()
    {
        movementMode = MovementMode.WaitInCover;
        agent.isStopped = true;
        mobAnimator.SetBool("isWalk", false);
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);

    }


    #endregion



    //�������� ��������
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

