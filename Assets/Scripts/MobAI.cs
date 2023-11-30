using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{

    // если safe position сфера спавнится так что моб не перестаёт её касаться (рядом) то не срабатывает OnTriggerEnter в SafePointTrig, и моб не считает что он дошёл
    //иногда моб не может найти safe точку


    [Header("Точки поиска")]
    public Transform[] all_wps;

    [Header("Точка откуда пускается луч во время препятствий (на голове)")]
    public Transform vision_point;

    [Header("Слои для определения видимости (что преграждает)")]
    public LayerMask layer_mask;

    [Header("Слой вижена игрока")]
    public LayerMask player_vision_mask;
    [Header("Слой препятствий")]
    public LayerMask obstacles_mask;

    [Header("Метка где последний раз видел игрока")]
    public Transform player_last_point_trig;

    [Header("Метка куда идёт")]
    public Transform target_point_vis;

    [HideInInspector] public GameController gameController;
    [HideInInspector] public GameObject player;
    [HideInInspector] public bool playerOnVisionTrig;

    [HideInInspector] public MovementMode movement_mode; //0 - не видит, 1 - идёт к последней точке, 2 - видит и идёт к игроку

    [HideInInspector] public Transform target;

    Transform player_camera;
    Transform my_transform;
    NavMeshAgent agent;
    Animator mob_animator;

    [HideInInspector] public WaypointTrig currentWaypointtrig;

    bool isInPlayerVision = false;

    GameController.GamePlayMode mobPlayMode;



    Collider[] obstaclesColliders = new Collider[10];

    [Header("Сфера в которой нужно искать укрытия")]
    public SphereCollider obstacleSearchRadius;

    [Header("Точка найденного укрытия")]
    public Transform safePoint;

    /*[Header("Lower is better hiding")]
    public float HideSensitivity =0;*/



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
            //print("GoToNextWP" );
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
        //проверить что моб находится в зоне видимости игрока

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
            
            //проверка что игрок не был в триггере до этого
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
            //проверка что моб не был в триггере до этого
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
                    //print("ни с чем не столкнулся");
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
                //print("ни с чем не столкнулся");
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
        print("Иду к игроку");

    }

    public void GoToPlayerLastPoint()
    {
        player_last_point_trig.position = player.transform.position;
        target = player_last_point_trig;
        movement_mode = MovementMode.GoToLastPoint;
        Move();
        print("Моб идёт к последней точке где видел игрока");


    }

    //запускается при достижении WayPoint
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

        print("я иду к  " + newWP);

        target = newWP;
        movement_mode = MovementMode.CheckWayPoints;
        Move();
        print("Следующая точка");

    }





    //позиции и колайдеру возвращает  позицию удара с обратной стороны

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

                Debug.DrawLine(offsetPoint, reverseHit.point, Color.red, 10f);


                print("point " + reverseHit.point );
                return reverseHit;

            }
        }
        print("no point ");

        return new RaycastHit();

    }



    public void HideFromPlayer()
    {
        Vector3 playerCameraPosition = player_camera.position;
        print("HideFromPlayer");
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);


        int colliders = Physics.OverlapSphereNonAlloc(vision_point.position, obstacleSearchRadius.radius, obstaclesColliders, obstacles_mask);
        print("colliders " + colliders);

        for (int i=0; i< colliders; i++)
        {
            
            RaycastHit raycastHit = HitBackSidePosition(playerCameraPosition, obstaclesColliders[i].gameObject);

            //расчёт направления
            Vector3 direction = obstaclesColliders[i].gameObject.transform.position - playerCameraPosition;
            
            if (raycastHit.collider!=null)
            {
                NavMeshHit navPoint;
                if (NavMesh.SamplePosition(raycastHit.transform.position, out navPoint, 10f, agent.areaMask))
                {
                    print("navPoint.position " + navPoint.position);

                    if (!NavMesh.FindClosestEdge(navPoint.position, out navPoint, agent.areaMask))
                    {
                        print("can not FindClosestEdge");
                    }

                    Vector3 direction1 = navPoint.normal;
                    NavMeshHit navPoint1;
                    Vector3 position1 = navPoint.position + direction1 * 0.2f;

                    if (NavMesh.SamplePosition(position1, out navPoint1, 10f, agent.areaMask))
                    {
                        print("navPoint1.position " + navPoint1.position);
                        if (Physics.Linecast(playerCameraPosition, new Vector3(navPoint1.position.x, playerCameraPosition.y, navPoint1.position.z), layer_mask))
                        {
                            safePoint.position = navPoint1.position;
                            target = safePoint;

                            movement_mode = MovementMode.HideFromPlayer;
                            Move();
                            print("Моб идёт к укрытию" + raycastHit.transform.gameObject);
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

