using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerControl : MonoBehaviour
{
    [Header ("Movement")]
    public Camera playerCamera;

    //player's speed
    public float speed = 2.0F;

    //rotation speed and camera limit
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    float rotationX = 0;


    [Header("Health Control")]
    //start number of lives
    public int StartHarts;

    //panel and sprite of lives
    public GameObject heartPanel;
    public Sprite heartSprite;

    //current number of lives
    private int heartsNum;

    private List<GameObject> hearts = new List<GameObject>();
    private float offset = 50;

    //End of game panel
    public GameObject gameOverPanel;


    bool isInVision = false;

    [Header("layer of mob vision")]
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


        //spawn hearts by the number of StartHarts
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
        //player movement
        CharacterController controller = GetComponent<CharacterController>();

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        float curSpeed = speed * Input.GetAxis("Vertical");

        Vector3 right = transform.TransformDirection(Vector3.right);
        float curSpeedHor = speed * Input.GetAxis("Horizontal");
        controller.SimpleMove(forward * curSpeed+ right * curSpeedHor);

        //camera rotation
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



    //checking that the player is in the vision zone of mobs (finding all mobs for the future)
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

            //start coroutines for mobs that did not exist in the last step
            foreach (GameObject mobVision in NewMobsSeesPlayer)
            {
                if (!MobsSeesPlayer.Contains(mobVision))
                {
                    mobVision.GetComponentInParent<MobAI>().StartCoroutine("RayOnPlayerCor");
                    mobVision.GetComponentInParent<MobAI>().playerOnVisionTrig = true;
                }
            }

            //stop the coroutines of mobs that do not exist, but were at the last step
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