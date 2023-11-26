using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public Transform Player;
    public MobAI[] Mobs;

    public PlayMode playMode;
    public enum PlayMode
    {
        PlayerHide,
        PlayerSeek
    }


    void Start()
    {
        playMode = PlayMode.PlayerSeek;
    }


/*    void ChangeMode(PlayMode playMode)
    {
        if(playMode == PlayMode.PlayerSeek)
        {




        }
        else if (playMode == PlayMode.PlayerHide)
        {




        }
    }*/






    void Update()
    {
        
    }
}
