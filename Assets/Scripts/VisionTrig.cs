using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionTrig : MonoBehaviour
{
    MobAI mob;
    int num = 0;
    bool isColliding =false;

    private void Start()
    {
        mob = transform.root.gameObject.GetComponent<MobAI>();

    }



/*private void OnTriggerEnter(Collider other)
    {
        if (isColliding) return;
        else
            isColliding = true;
        num++;
        print("Игрок попал в триггер видимости " + num);
        mob.StartCoroutine("RayOnPlayerCor");
        mob.playerOnVisionTrig = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isColliding) return;
        else
            isColliding = false;
        num--;
        print("Игрок вышел из триггера видимости " + num);
        mob.StopCoroutine("RayOnPlayerCor");
        mob.playerOnVisionTrig = false;
        if (mob.movement_mode == MobAI.MovementMode.GoToPlayer)
            mob.GoToPlayerLastPoint();
    }*/



}
