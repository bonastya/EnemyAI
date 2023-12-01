using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTrig : MonoBehaviour
{
    bool seesPlayer;
    public GameObject gameOverPanel;

    MobAI mob;

    public GameObject AttackPannel;
    HealthControl healthControl;

    Coroutine damageCor;


    void Start()
    {
        mob=transform.root.gameObject.GetComponent<MobAI>();
        healthControl = Object.FindObjectOfType<HealthControl>();
    }


    private void OnTriggerEnter(Collider colider)
    {
        if (colider.gameObject.tag == "Player")
        {
            print("OnTriggerEnter сердце");
            AttackPannel.SetActive(true);

            if (damageCor != null)
                StopCoroutine(damageCor);

            seesPlayer = true;
            damageCor =StartCoroutine(SeesCheckCor());
  
        }

    }

    private void OnTriggerExit(Collider colider)
    {

        if (colider.gameObject.tag == "Player")
        {
            seesPlayer = false;
            AttackPannel.SetActive(false);
        }

    }

    IEnumerator SeesCheckCor()
    {

        print("SeesCheckCor сердце");

        while (seesPlayer)
        {
            if (mob.movement_mode == MobAI.MovementMode.GoToPlayer)
            {
                healthControl.HealthDecrease();

                yield return new WaitForSeconds(3f);

            }
        }

        damageCor = null;
    }


}
