using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafePointTrig : MonoBehaviour
{

    public float moveDelayMin = 3f;
    public float moveDelayMax = 5f;

    private MobAI mob;


    private void OnTriggerEnter(Collider colider)
    {
        if (colider.gameObject.tag == "Mob")
        {
            if (transform == colider.gameObject.GetComponent<MobAI>().target)
            {
                mob = colider.gameObject.GetComponent<MobAI>();
                mob.WaitInCover();
            }
        }
    }



}
