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
            StartCoroutine(CheckInVisionCor());
        else if (mode == GameController.GamePlayMode.PlayerSeek)
            StopCoroutine(CheckInVisionCor());

    }

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

            foreach (GameObject mobVision in NewMobsSeesPlayer)
            {
                if (!MobsSeesPlayer.Contains(mobVision))
                {
                    //print("моб: " + mob1);
                    print("моб: StartCoroutine" + mobVision.ToString() + " StartCoroutine");
                    mobVision.GetComponent<VisionTrig>().mob.StartCoroutine("RayOnPlayerCor");
                    mobVision.GetComponent<VisionTrig>().mob.playerOnVisionTrig = true;
                }
            }

            foreach (GameObject mobVision in MobsSeesPlayer)
            {
                if (!NewMobsSeesPlayer.Contains(mobVision))
                {
                    print("моб: StopCoroutine" + mobVision.ToString() + " StopCoroutine");

                    mobVision.GetComponent<VisionTrig>().mob.StopCoroutine("RayOnPlayerCor");
                    mobVision.GetComponent<VisionTrig>().mob.playerOnVisionTrig = false;
                }
            }

            MobsSeesPlayer.Clear();
            MobsSeesPlayer.AddRange(NewMobsSeesPlayer);

        }


    }



}
