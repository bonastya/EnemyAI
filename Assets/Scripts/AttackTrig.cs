using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTrig : MonoBehaviour
{
    bool seesPlayer;
    public GameObject gameOverPanel;

    MobAI mob;

    void Start()
    {
        mob=transform.root.gameObject.GetComponent<MobAI>();
    }


    private void OnTriggerEnter(Collider other)
    {
        StartCoroutine(SeesCheckCor());
        seesPlayer = true;
    }

    private void OnTriggerExit(Collider other)
    {
        StopCoroutine(SeesCheckCor());
        seesPlayer = false;
    }

    IEnumerator SeesCheckCor()
    {
        yield return new WaitForSeconds(0.1f);

        while (seesPlayer)
        {
            yield return new WaitForSeconds(0.1f);
            if (mob.movement_mode == MobAI.MovementMode.GoToPlayer)
            {
                seesPlayer = false;
                //Time.timeScale = 0;
                gameOverPanel.SetActive(true);
            }
        }

    }





}
