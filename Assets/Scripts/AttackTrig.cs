using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTrig : MonoBehaviour
{
    bool seesPlayer;
    public GameObject gameOverPanel;

    MobAI mob;

    public GameObject AttackPannel;
    private PlayerControl player;
    private PlayerControl gameController;

    Coroutine damageCor;


    void Start()
    {
        mob=transform.root.gameObject.GetComponent<MobAI>();
        player = Object.FindObjectOfType<PlayerControl>();

        gameController = FindObjectOfType<PlayerControl>();
    }


    private void OnTriggerEnter(Collider colider)
    {
        if (gameController.playMode == PlayerControl.GamePlayMode.PlayerHide)
        {
            if (colider.gameObject.tag == "Player")
            {
                AttackPannel.SetActive(true);

                if (damageCor != null)
                    StopCoroutine(damageCor);

                seesPlayer = true;
                damageCor = StartCoroutine(DamageCor());

            }
        }
       

    }

    private void OnTriggerExit(Collider colider)
    {
        if (gameController.playMode == PlayerControl.GamePlayMode.PlayerHide)
        {
            if (colider.gameObject.tag == "Player")
            {
                seesPlayer = false;
                AttackPannel.SetActive(false);
            }
        }

    }

    IEnumerator DamageCor()
    {
        while (seesPlayer)
        {
            if (mob.movementMode == MobAI.MovementMode.GoToPlayer)
            {
                player.HealthDecrease();

                yield return new WaitForSeconds(3f);

            }
        }

        damageCor = null;
    }


}
