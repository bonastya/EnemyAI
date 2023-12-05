using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerControl : MonoBehaviour
{
    [Header ("Movement")]
    public Camera playerCamera;

    //скорость игрока
    public float speed = 2.0F;

    //скорость поворота и лимит камеры
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    float rotationX = 0;


    [Header("Health Control")]
    //начальное кол-во жизней
    public int StartHarts;

    //панель и спрайт жизней
    public GameObject heartPanel;
    public Sprite heartSprite;

    //текущее кол-во жизней
    private int heartsNum;

    private List<GameObject> hearts = new List<GameObject>();
    private float offset = 50;

    //панель конца игры
    public GameObject gameOverPanel;




    [Header("In Vision Detecting")]
    //public Transform player_camera;
    bool isInVision = false;

    [Header("Слой вижена моба (конуса)")]
    public LayerMask mob_vision_mask;

    private List<GameObject> MobsSeesPlayer;
    private List<GameObject> NewMobsSeesPlayer;


    private MobAI[] Mobs;

    [Header("Game mode")]
    public GamePlayMode playMode;
    public enum GamePlayMode
    {
        PlayerHide,
        PlayerSeek
    }


    Rigidbody rb;
    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        //спавн сердечек по количеству StartHarts
        for (int i = 0; i < StartHarts; i++)
        {
            GameObject imgObject = new GameObject("Heart");
            RectTransform trans = imgObject.AddComponent<RectTransform>();
            trans.transform.SetParent(heartPanel.transform);

            trans.anchoredPosition = new Vector2(-offset * i, 0f);
            trans.sizeDelta = new Vector2(50, 50);
            Image image = imgObject.AddComponent<Image>();

            image.sprite = heartSprite;

            hearts.Add(imgObject);
        }

        heartsNum = hearts.Count;


        MobsSeesPlayer = new List<GameObject>();
        NewMobsSeesPlayer = new List<GameObject>();

        Mobs = Object.FindObjectsOfType<MobAI>();
        ChangeMode(playMode);

    }


    void Update()
    {
        //движение игрока
        CharacterController controller = GetComponent<CharacterController>();

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        float curSpeed = speed * Input.GetAxis("Vertical");

        Vector3 right = transform.TransformDirection(Vector3.right);
        float curSpeedHor = speed * Input.GetAxis("Horizontal");
        controller.SimpleMove(forward * curSpeed+ right * curSpeedHor);

        //поворот камеры
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }


    public void ChangeMode(GamePlayMode mode)
    {
        playMode = mode;
        foreach (MobAI mob in Mobs)
        {
            mob.ChangePlayMode(mode);
        }

        ChangePlayMode(mode);

    }


    #region in vision detecting

    public void ChangePlayMode(GamePlayMode mode)
    {
        if (mode == GamePlayMode.PlayerHide)
        {
            StartCoroutine(CheckInVisionCor());
        }

        else if (mode == GamePlayMode.PlayerSeek)
        {
            StopCoroutine(CheckInVisionCor());
        }

    }



    //проверка  что игрок находится в зоне видимости мобов (вариант нахождения всех мобов на будущее)
    IEnumerator CheckInVisionCor()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);


            Collider[] hitColliders = Physics.OverlapSphere(playerCamera.transform.position, 0f, mob_vision_mask);
            isInVision = (hitColliders.Length > 0);
            NewMobsSeesPlayer.Clear();

            foreach (Collider col in hitColliders)
            {
                GameObject mob = col.gameObject;
                NewMobsSeesPlayer.Add(mob);
            }

            //запустить корутины у мобов которых не было на прошлом шаге
            foreach (GameObject mobVision in NewMobsSeesPlayer)
            {
                if (!MobsSeesPlayer.Contains(mobVision))
                {
                    mobVision.GetComponentInParent<MobAI>().StartCoroutine("RayOnPlayerCor");
                    mobVision.GetComponentInParent<MobAI>().playerOnVisionTrig = true;
                }
            }

            //остановить корутины у мобов которых нет, но были на прошлом шаге
            foreach (GameObject mobVision in MobsSeesPlayer)
            {
                if (!NewMobsSeesPlayer.Contains(mobVision))
                {
                    mobVision.GetComponentInParent<MobAI>().StopCoroutine("RayOnPlayerCor");
                    mobVision.GetComponentInParent<MobAI>().playerOnVisionTrig = false;
                }
            }

            MobsSeesPlayer.Clear();
            MobsSeesPlayer.AddRange(NewMobsSeesPlayer);

        }


    }

    #endregion



    #region health control

    public void HealthDecrease()
    {
        hearts[heartsNum - 1].SetActive(false);
        heartsNum--;
        if (heartsNum <= 0)
        {
            Die();
        }
    }

    public void HealthIncrease()
    {
        hearts[heartsNum].SetActive(true);
        heartsNum++;
    }

    void Die()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0;

    }

    #endregion 

}