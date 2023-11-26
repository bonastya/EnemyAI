using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{
    [Header("Точки поиска")]
    public Transform[] all_wps;

    [Header("Точка откуда пускается луч во время препятствий (на голове)")]
    public Transform vision_point;

    [Header("Слои для определения видимости (что преграждает)")]
    public LayerMask layer_mask;

    [Header("Слой вижена моба")]
    public LayerMask mob_vision_mask;

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

    [HideInInspector] public WaypointTrig currentWaypointtrig ;

    bool isVisionPlayerCollision = false;


    public enum MovementMode
    {
        GoToPlayer,
        CheckWayPoints,
        GoToLastPoint,
        HideFromPlayer
    }


    void Start()
    {
        my_transform = transform;
        player = GameObject.FindWithTag("Player");
        player_camera = GameObject.FindWithTag("MainCamera").transform;
        mob_animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        currentWaypointtrig = all_wps[1].gameObject.GetComponent<WaypointTrig>();

        gameController = FindObjectOfType<GameController>();
     

        if(gameController.playMode == GameController.PlayMode.PlayerHide)
        {
            GoToNextWP();
        }
        else if (gameController.playMode == GameController.PlayMode.PlayerSeek)
        {
            HideFromPlayer();
        }

        StartCoroutine("SetDestinationCor");


    }

    private void Update()
    {
        if (gameController.playMode == GameController.PlayMode.PlayerHide)
        {
            CheckIfPlayerInVision();
        }
        else if (gameController.playMode == GameController.PlayMode.PlayerSeek)
        {
            HideFromPlayer();
        }


    }


    private void CheckIfPlayerInVision()
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
                    print("ни с чем не столкнулся");
                    if (movement_mode != MovementMode.GoToPlayer)
                        SeesPlayer();

                }
                else if (movement_mode == MovementMode.GoToPlayer)
                {
                    //print("cneryekcz j " + str.collider.gameObject.name.ToString());
                    GoToPlayerLastPoint();
                }
            }
            

            //else print("cneryekcz j " + str.collider.gameObject.name.ToString());
        }
    }



    public void SeesPlayer()
    {
        if (currentWaypointtrig) StopCoroutine("GoToNextWPCor");
        target = player.transform;
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

    public void StartGoToNextWPCor(float delay)
    {
        StartCoroutine(GoToNextWPCor(delay));

    }    
    
    IEnumerator GoToNextWPCor( float delay)
    {
        yield return new WaitForSeconds(delay);
        GoToNextWP();
    }
    public void GoToNextWP()
    {
        
        Transform newWP= currentWaypointtrig.transform;

        while (newWP == currentWaypointtrig.transform)
            newWP = all_wps[Random.Range(0, all_wps.Length)].transform;

        print("я иду к  " + newWP);

        target = newWP;
        movement_mode = MovementMode.CheckWayPoints;
        Move();
        print("Следующая точка");

    }



    //если этот режим то
    //проверять видит ли меня игрок постоянно
    //если видит то выбирать ближайшую точку и идти туда
    //иначе соять

    //выбрать ближайшую точку
    //



    public void HideFromPlayer()
    {

        Transform safePoint;




        safePoint=








        target = safePoint;
        movement_mode = MovementMode.HideFromPlayer;
        Move();
        print("Моб идёт к укрытию");


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
