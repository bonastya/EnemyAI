using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointTrig : MonoBehaviour
{

    public float moveDelayMin = 3f;
    public float moveDelayMax = 5f;

    public MobAI mob;

    public string pointAnimName = "idle";

    private void OnTriggerEnter(Collider colider)
    {
         if(colider.gameObject.tag == "Mob")
        {
            if(transform == colider.gameObject.GetComponent<MobAI>().target)
            {
                mob = colider.gameObject.GetComponent<MobAI>();
                mob.Stop();
                mob.currentWaypointtrig = this;
                mob.StartGoToNextWPCor(Random.Range(moveDelayMin, moveDelayMax));
            }
        }
    }



}
