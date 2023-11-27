using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public InVisionDetecting Player;
    public MobAI[] Mobs;

    public GamePlayMode playMode;
    public enum GamePlayMode
    {
        PlayerHide,
        PlayerSeek
    }

    public void  ChangeMode(GamePlayMode mode)
    {
        playMode = mode;
        foreach (MobAI mob in Mobs)
        {
            mob.ChangePlayMode(mode);
        }

        Player.ChangePlayMode(mode);

    }


    void Start()
    {
        
        ChangeMode(playMode);
    }


    void Update()
    {

    }
}

