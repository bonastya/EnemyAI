using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{

    // ���� safe position ����� ��������� ��� ��� ��� �� �������� � �������� (�����) �� �� ����������� OnTriggerEnter � SafePointTrig, � ��� �� ������� ��� �� �����
    //������ ��� �� ����� ����� safe �����


    [Header("����� ������")]
    public Transform[] all_wps;

    [Header("����� ������ ��������� ��� �� ����� ����������� (�� ������)")]
    public Transform vision_point;

    [Header("���� ��� ����������� ��������� (��� �����������)")]
    public LayerMask layer_mask;

    [Header("���� ������ ������")]
    public LayerMask player_vision_mask;
    [Header("���� �����������")]
    public LayerMask obstacles_mask;

    [Header("����� ��� ��������� ��� ����� ������")]
    public Transform player_last_point_trig;

    [Header("����� ���� ���")]
    public Transform target_point_vis;

    [HideInInspector] public GameController gameController;
    [HideInInspector] public GameObject player;
    [HideInInspector] public bool playerOnVisionTrig;

    [HideInInspector] public MovementMode movement_mode; //0 - �� �����, 1 - ��� � ��������� �����, 2 - ����� � ��� � ������

    [HideInInspector] public Transform target;

    Transform player_camera;
    Transform my_transform;
    NavMeshAgent agent;
    Animator mob_animator;

    [HideInInspector] public WaypointTrig currentWaypointtrig;

    bool isInPlayerVision = false;

    GameController.GamePlayMode mobPlayMode;



    Collider[] obstaclesColliders = new Collider[10];

    [Header("����� � ������� ����� ������ �������")]
    public SphereCollider obstacleSearchRadius;

    [Header("����� ���������� �������")]
    public Transform safePoint;

    [Header("Lower is better hiding")]
    public float HideSensitivity =0;



    private Coroutine SafePointCor;


    public enum MovementMode
    {
        GoToPlayer,
        CheckWayPoints,
        GoToLastPoint,
        HideFromPlayer,
        WaitInCover
    }


    void Start()
    {
        my_transform = transform;
        target = transform;
        player = GameObject.FindWithTag("Player");
        player_camera = GameObject.FindWithTag("MainCamera").transform;
        mob_animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentWaypointtrig = all_wps[1].gameObject.GetComponent<WaypointTrig>();

        gameController = FindObjectOfType<GameController>();

        print("gameController.playMode " + gameController.playMode);
        if (gameController.playMode == GameController.GamePlayMode.PlayerHide)
        {
            print("GoToNextWP" );
            GoToNextWP();
        }
        else if (gameController.playMode == GameController.GamePlayMode.PlayerSeek)
        {
            CheckIfInVisionOfPlayer();
        }

        StartCoroutine("SetDestinationCor");


    }





    private void Update()
    {
        //��������� ��� ��� ��������� � ���� ��������� ������

        if (mobPlayMode == GameController.GamePlayMode.PlayerSeek)
        {
            CheckIfInVisionOfPlayer();
        }

        
    }


    public void ChangePlayMode(GameController.GamePlayMode playMode)
    {
        if (playMode == GameController.GamePlayMode.PlayerHide)
        {
            mobPlayMode = playMode;




        }
        else if (playMode == GameController.GamePlayMode.PlayerSeek)
        {
            mobPlayMode = playMode;
            StopCoroutine("RayOnPlayerCor");

        }
    }

/*    private void CheckIfPlayerInVision()
    {
        Collider[] hitColliders = Physics.OverlapSphere(player_camera.transform.position, 0f, mob_vision_mask);
        if (hitColliders.Length > 0)
        {
            
            //�������� ��� ����� �� ��� � �������� �� �����
            if (!isVisionPlayerCollision)
            {
                isVisionPlayerCollision = true;
                Debug.Log("Colliding with: " + hitColliders[0]);
                StartCoroutine("RayOnPlayerCor");
                playerOnVisionTrig = true;
            }

        }
        else
        {
            if (isVisionPlayerCollision)
            {
                isVisionPlayerCollision = false;
                Debug.Log("Not colliding");
                StopCoroutine("RayOnPlayerCor");
                playerOnVisionTrig = false;
            }
        }
    }*/


    private void CheckIfInVisionOfPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(vision_point.position, 0f, player_vision_mask);
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

    IEnumerator SetDestinationCor()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            agent.SetDestination(target.position);
            target_point_vis.position = target.position;
        }
    }

    IEnumerator RayOnPlayerCor()
    {
        RaycastHit str;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            if (playerOnVisionTrig)
            {
                Debug.DrawLine(vision_point.position, player_camera.position, Color.red);

                if (!Physics.Linecast(vision_point.position, player_camera.position, out str, layer_mask))
                {
                    //print("�� � ��� �� ����������");
                    if (movement_mode != MovementMode.GoToPlayer)
                        SeesPlayer();
                }
                else if (movement_mode == MovementMode.GoToPlayer)
                {
                    GoToPlayerLastPoint();
                }
            }


        }
    }

    IEnumerator IsHideFromPlayerRayCor()
    {
        RaycastHit str;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            Debug.DrawLine(vision_point.position, player_camera.position, Color.green);

            if (!Physics.Linecast(vision_point.position, player_camera.position, out str, layer_mask))
            {
                //print("�� � ��� �� ����������");
                if (movement_mode != MovementMode.HideFromPlayer)
                    HideFromPlayer();

            }
            /*else
            {
                if (movement_mode != MovementMode.WaitInCover)
                    WaitInCover();
            }*/
           

        }
    }


    public void SeesPlayer()
    {
        if (currentWaypointtrig) StopCoroutine("GoToNextWPCor");
        target.position = player.transform.position;
        movement_mode = MovementMode.GoToPlayer;
        Move();
        print("��� � ������");

    }

    public void GoToPlayerLastPoint()
    {
        player_last_point_trig.position = player.transform.position;
        target = player_last_point_trig;
        movement_mode = MovementMode.GoToLastPoint;
        Move();
        print("��� ��� � ��������� ����� ��� ����� ������");


    }

    //����������� ��� ���������� WayPoint
    public void StartGoToNextWPCor(float delay)
    {
        StartCoroutine(GoToNextWPCor(delay));
    }

    IEnumerator GoToNextWPCor(float delay)
    {
        yield return new WaitForSeconds(delay);
        GoToNextWP();
    }
    public void GoToNextWP()
    {

        Transform newWP = currentWaypointtrig.transform;

        while (newWP == currentWaypointtrig.transform)
            newWP = all_wps[Random.Range(0, all_wps.Length)].transform;

        print("� ��� �  " + newWP);

        target = newWP;
        movement_mode = MovementMode.CheckWayPoints;
        Move();
        print("��������� �����");

    }



    //���� ���� ����� ��
    //��������� ����� �� ���� ����� ���������
    //���� ����� �� �������� ��������� ����� � ���� ����
    //����� �����

    //������� ��������� �����
    //



    public void HideFromPlayer()
    {
        Vector3 playerCameraPosition = player_camera.position;
        print("HideFromPlayer");
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);


        int colliders = Physics.OverlapSphereNonAlloc(vision_point.position, obstacleSearchRadius.radius, obstaclesColliders, obstacles_mask);

        for(int i=0; i< colliders; i++)
        {
            //Vector3 playerHeightColliderPosition = new Vector3(obstaclesColliders[i].gameObject.transform.position.x, playerCameraPosition.y, obstaclesColliders[i].gameObject.transform.position.z);

            //������ �����������
            Vector3 direction = obstaclesColliders[i].gameObject.transform.position - playerCameraPosition;
            Vector3 playerHeightdirection = new Vector3(direction.x, playerCameraPosition.y, direction.z);
            RaycastHit[] hits;

            hits = Physics.RaycastAll(playerCameraPosition, playerHeightdirection, Mathf.Infinity);

            if (hits.Length>0)
            {
                Debug.DrawRay(playerCameraPosition, playerHeightdirection * 1000, Color.yellow);

                
                foreach (RaycastHit hit in hits)
                {
                    //print("������������ � hit.transform.gameObject " + hit.transform.gameObject);

                    if (hit.transform.gameObject == obstaclesColliders[i].gameObject)
                    {
                        NavMeshHit navPoint;
                        if(NavMesh.SamplePosition(hit.transform.position, out navPoint, 10f, agent.areaMask))
                        {
                            //Debug.DrawLine(playerCameraPosition, new Vector3(navPoint.position.x, playerCameraPosition.y, navPoint.position.z), Color.red, 10f);
                            print("navPoint.position " + navPoint.position);


                            if (!NavMesh.FindClosestEdge(navPoint.position, out navPoint, agent.areaMask))
                            {
                                print("can not FindClosestEdge");
                                
                            }




                            Vector3 direction1 = navPoint.normal;
                            NavMeshHit navPoint1;
                            Vector3 position1 = navPoint.position + direction1 * 0.2f;



                            if(Vector3.Dot(navPoint.normal, (playerCameraPosition - navPoint.position).normalized) < HideSensitivity)
                            {
                                if (NavMesh.SamplePosition(position1, out navPoint1, 10f, agent.areaMask))
                                {
                                    Debug.DrawLine(playerCameraPosition, new Vector3(navPoint.position.x, playerCameraPosition.y, navPoint.position.z), Color.red, 10f);
                                    print("navPoint1.position " + navPoint1.position);
                                    if (Physics.Linecast(playerCameraPosition, new Vector3(navPoint1.position.x, playerCameraPosition.y, navPoint1.position.z), layer_mask))
                                    {

                                        safePoint.position = navPoint1.position;
                                        target = safePoint;

                                        movement_mode = MovementMode.HideFromPlayer;
                                        Move();
                                        print("��� ��� � �������" + hit.transform.gameObject);
                                        SafePointCor = StartCoroutine(IfSafePointStillSafe(safePoint.position));

                                        if (safePoint.GetComponent<SafePointTrig>().ifPlayerColiding())
                                        {
                                            print("collider is near");
                                            StartCoroutine(WaitWhenGoToNearPoint());
                                            
                                        }
                                           
                                        return;
                                    }
                                }

                            }
  
                        }

                    }
                }

            }
            else
            {
                Debug.DrawRay(playerCameraPosition, direction * 1000, Color.white);
                Debug.Log("Did not Hit");
            }

        } 

    }



    IEnumerator IfSafePointStillSafe(Vector3 safePosition)
    {
        while (Physics.Linecast(player_camera.position, new Vector3(safePosition.x, player_camera.position.y, safePosition.z), layer_mask))
        {
            yield return new WaitForSeconds(0.1f);
            print("Save point is safe");

        }
        if (movement_mode == MovementMode.HideFromPlayer)
            HideFromPlayer();
        SafePointCor = null;

        print("Save point is not safe");
    }

    IEnumerator WaitWhenGoToNearPoint()
    {
        yield return new WaitForSeconds(0.3f);
        WaitInCover();
    }

    public void WaitInCover()
    {
        movement_mode = MovementMode.WaitInCover;
        agent.isStopped = true;
        mob_animator.SetBool("isWalk", false);
        if(SafePointCor!=null)
            StopCoroutine(SafePointCor);
        print("StopCoroutine(IfSafePointStillSafe)");

    }



    public void Stop(string animName)
    {
        movement_mode = MovementMode.CheckWayPoints;
        agent.isStopped = true;
        mob_animator.SetBool("isWalk", false);
        mob_animator.SetTrigger(animName);

    }

    public void Move()
    {
        if (!mob_animator.GetCurrentAnimatorStateInfo(0).IsName("walk"))
            mob_animator.SetTrigger("isWalk");

        mob_animator.SetBool("isWalk", true);
        agent.isStopped = false;


    }

}

