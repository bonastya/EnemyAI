using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthControl : MonoBehaviour
{

    public int StartHarts;

    public GameObject heartPanel;
    public Sprite heartSprite;

    private int heartsNum;


    private List<GameObject> hearts = new List<GameObject>();
    private float offset=50;


    public GameObject gameOverPanel;


    void Start()
    {


       for(int i=0; i< StartHarts; i++)
        {
            GameObject imgObject = new GameObject("Heart");
            RectTransform trans = imgObject.AddComponent<RectTransform>();
            trans.transform.SetParent(heartPanel.transform); 

            trans.anchoredPosition = new Vector2(-offset*i, 0f); 
            trans.sizeDelta = new Vector2(50, 50); 
            Image image = imgObject.AddComponent<Image>();

            image.sprite = heartSprite;

            hearts.Add(imgObject);
        }

        heartsNum = hearts.Count;



    }


    public void HealthDecrease()
    {
        print("минус сердце");
        hearts[heartsNum - 1].SetActive(false);
        heartsNum--;
        if (heartsNum <= 0)
        {
            Die();
        }
    }

    public void HealthIncrease()
    {
        hearts[heartsNum].SetActive(true);
        heartsNum++;
    }

    void Die()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0;
        
    }


}
