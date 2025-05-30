using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public static UIController _instance;

    Transform Fruits;
    int curBerryNumber = 0;
    GameObject pauseUI;

    public List<BoxController> boxGroup;
    public KeyCode PauseKey = KeyCode.Space;  // ÔÝÍ£

    // Start is called before the first frame update
    private void Awake()
    {
        _instance = this;
    }
    void Start()
    {
        Fruits = transform.Find("Fruits");
        pauseUI = transform.Find("PauseBG").gameObject;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(PauseKey))
        {
            print(12312312);
            if (PauseUI())
            {
                foreach (var item in boxGroup)
                {
                    item.BoxMove();
                }
            }
        }
    }
    public void PickBerry() 
    {
        curBerryNumber++;
        for (int i = 0; i < curBerryNumber; i++)
        {
            Fruits.GetChild(i).GetChild(0).gameObject.SetActive(false);
        }
    }

    public bool PauseUI() 
    {
        pauseUI.SetActive(!pauseUI.activeSelf);
        return pauseUI.activeSelf;
    }
}
