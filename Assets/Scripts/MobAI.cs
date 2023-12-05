using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MobAI : MonoBehaviour
{

    [Header("Точки поиска")]
    public Transform[] allWps;

    [Header("Точка откуда пускается луч во время препятствий (на голове моба)")]
    public Transform visionPoint;

    [Header("Слои для определения видимости (что преграждает)")]
    public LayerMask obstacleLayerMask;

    [Header("Слой вижена игрока")]
    public LayerMask playerVisionMask;

    [Header("Метка где последний раз видел игрока")]
    public Transform playerLastPointTrig;

    [Header("Метка куда идёт моб (визуализация)")]
    public Transform targetPointVis;

    [Header("Скорость моба при режиме поиска")]
    public float seekSpeed=1f;

    [Header("Скорость моба при режиме скрывания")]
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

    [Header("Сфера в которой нужно искать укрытия")]
    public SphereCollider obstacleSearchRadius;

    [Header("Точка найденного укрытия")]
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

    [Header("Длительность паузы на WayPoint")]
    public float moveDelayMin = 3f;
    public float moveDelayMax = 5f;


    private bool mobIsInSafePoint = false;

    [Header("Экран атаки")]
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
        //проверка что моб находится в зоне видимости игрока
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

    //используется для обоих режимов игры
    //target указывает на актуальную цель для Navagent
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

    //проверка есть ли препятствия между игроком и мобом
    IEnumerator RayOnPlayerCor()
    {
        RaycastHit str;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            //если в области видимости моба
            if (playerOnVisionTrig)
            {
                Debug.DrawLine(visionPoint.position, playerCamera.position, Color.red);
                //если нет препятствий
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
        print("Моб идёт к игроку");

    }

    public void GoToPlayerLastPoint()
    {
        playerLastPointTrig.position = player.transform.position;
        target = playerLastPointTrig;
        movementMode = MovementMode.GoToLastPoint;
        Move();
        print("Моб идёт к последней точке где видел игрока");


    }

    //запускается при достижении WayPoint
    public void StartGoToNextWPCor(float delay)
    {
        WayPointCor = StartCoroutine(GoToNextWPCor(delay));
    }

    //задержка на WayPoint
    IEnumerator GoToNextWPCor(float delay)
    {
        yield return new WaitForSeconds(delay);
        GoToNextWP();
    }
    public void GoToNextWP()
    {
        Transform newWP = currentWaypointtrig.transform;
        //выбрать случайную точку кроме текущей
        while (newWP == currentWaypointtrig.transform)
            newWP = allWps[Random.Range(0, allWps.Length)].transform;

        print("Моб идёт к  " + newWP);

        target = newWP;
        movementMode = MovementMode.CheckWayPoints;
        Move();
        WayPointCor = null;

    }

    //когда моб дошёл до WayPoint
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
        //пересекается ли точка с зоной видимости игрока
        Collider[] hitColliders = Physics.OverlapSphere(visionPoint.position, 0f, playerVisionMask);
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

    //проверка что на линии видимости с игроком (нет препятствий)
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


    //по начальной позиции и объекту возвращает позицию удара о коллайдер объекта с обратной стороны

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
        print("Не удалось найти точку за объектом" + coliderObject);
        ifFoundCollider = false;
        return new RaycastHit();

    }


    //поиск и проверка точки где можно спрятаться
    public void HideFromPlayer()
    {
        Vector3 playerCameraPosition = playerCamera.position;
        print("HideFromPlayer");
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);

        //коллайдеры в радиусе поиска моба
        int colliders = Physics.OverlapSphereNonAlloc(visionPoint.position, obstacleSearchRadius.radius, obstaclesColliders, obstacleLayerMask);

        for (int i = 0; i < colliders; i++)
        {
            //позиция за коллайдером
            RaycastHit raycastHit = HitBackSidePosition(playerCameraPosition, obstaclesColliders[i].gameObject);

            //расчёт направления от игрока к предполагаемой безопасной позиции
            Vector3 direction = obstaclesColliders[i].gameObject.transform.position - playerCameraPosition;

            if (ifFoundCollider)
            {
                NavMeshHit navPoint;
                //ближайшая позиция NavMesh 
                if (NavMesh.SamplePosition(raycastHit.transform.position, out navPoint, 10f, agent.areaMask))
                {
                    //ближайшая грань navMesh
                    if (!NavMesh.FindClosestEdge(navPoint.position, out navPoint, agent.areaMask))
                    {
                        print("can not FindClosestEdge");
                    }

                    //отступ от края NavMesh перпендикулярно грани
                    Vector3 normalDirection = navPoint.normal;
                    NavMeshHit offsetNavPoint;
                    Vector3 position1 = navPoint.position + normalDirection * 0.2f;


                    //ближайшая позиция NavMesh к новой точке
                    if (NavMesh.SamplePosition(position1, out offsetNavPoint, 10f, agent.areaMask))
                    {
                        //проверка что новая точка не находится на линии прямой видимости с игроком
                        if (Physics.Linecast(playerCameraPosition, new Vector3(offsetNavPoint.position.x, playerCameraPosition.y, offsetNavPoint.position.z), obstacleLayerMask))
                        {
                            safePoint.position = offsetNavPoint.position;
                            target = safePoint;

                            movementMode = MovementMode.HideFromPlayer;
                            Move();
                            print("Моб идёт к укрытию" + raycastHit.transform.gameObject);
                            SafePointCor = StartCoroutine(IfSafePointStillSafe(safePoint.position));

                            //если точка слишком близко к мобу
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



    //проверка что выбранная точка для укрытия моба всё ещё не видна игроку
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

    //задержка если точка для прятания появилась слишком близко к мобу
    IEnumerator WaitWhenGoToNearPoint()
    {
        yield return new WaitForSeconds(0.3f);
        WaitInCover();
    }


    //когда моб достиг скрытной точки
    public void WaitInCover()
    {
        movementMode = MovementMode.WaitInCover;
        agent.isStopped = true;
        mobAnimator.SetBool("isWalk", false);
        if (SafePointCor != null)
            StopCoroutine(SafePointCor);

    }


    #endregion



    //анимация движения
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

