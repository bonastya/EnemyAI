using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InVisionDetecting : MonoBehaviour
{

    public Transform player_camera;
    bool isInVision = false;

    [Header("Слой вижена моба (конуса)")]
    public LayerMask mob_vision_mask;

    private List<GameObject> MobsSeesPlayer;
    private List<GameObject> NewMobsSeesPlayer;
    void Start()
    {
        MobsSeesPlayer = new List<GameObject>();
        NewMobsSeesPlayer = new List<GameObject>();

    }

    public void ChangePlayMode(GameController.GamePlayMode mode)
    {
        if(mode == GameController.GamePlayMode.PlayerHide)
        {
            StartCoroutine(CheckInVisionCor());
        }
            
        else if (mode == GameController.GamePlayMode.PlayerSeek)
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


            Collider[] hitColliders = Physics.OverlapSphere(player_camera.transform.position, 0f, mob_vision_mask);
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
                    mobVision.GetComponent<VisionTrig>().mob.StartCoroutine("RayOnPlayerCor");
                    mobVision.GetComponent<VisionTrig>().mob.playerOnVisionTrig = true;
                }
            }

            //остановить корутины у мобов которых нет, но были на прошлом шаге
            foreach (GameObject mobVision in MobsSeesPlayer)
            {
                if (!NewMobsSeesPlayer.Contains(mobVision))
                {
                    mobVision.GetComponent<VisionTrig>().mob.StopCoroutine("RayOnPlayerCor");
                    mobVision.GetComponent<VisionTrig>().mob.playerOnVisionTrig = false;
                }
            }

            MobsSeesPlayer.Clear();
            MobsSeesPlayer.AddRange(NewMobsSeesPlayer);

        }


    }



}
