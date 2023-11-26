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

}
