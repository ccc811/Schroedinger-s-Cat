using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController _instance;

    Transform Fruits;
    Transform FruitsWin;
    int curBerryNumber = 0;
    GameObject pauseUI;
    GameObject WinUI;
    GameObject SelectUI;

    public List<BoxController> boxGroup;
    public KeyCode PauseKey = KeyCode.Space;  // pause
    public KeyCode ResetKey = KeyCode.R;  // reset

    private Button startButton;
    private Button resetButton;
    private Button pauseButton;

    private Button winBack;
    private Button winNext;
    private Button winReset;

    private Button select1;
    private Button select2;
    private Button select3;
    private Button select4;
    private Button select5;
    private Button selectBack;


    // Start is called before the first frame update
    private void Awake()
    {
        _instance = this;
    }
    void Start()
    {
        Fruits = transform.Find("Fruits");
        pauseUI = transform.Find("PauseBG").gameObject;

        startButton = transform.Find("PauseBG/Pause").GetComponent<Button>();
        resetButton = transform.Find("PauseBG/Reset").GetComponent<Button>();
        pauseButton = transform.Find("BG/Pause").GetComponent<Button>();

        winBack = transform.Find("WinBG/Back").GetComponent<Button>();
        winNext = transform.Find("WinBG/Next").GetComponent<Button>();
        winReset = transform.Find("WinBG/Reset").GetComponent<Button>();

        select1 = transform.Find("SelectBG/1").GetComponent<Button>();
        select2 = transform.Find("SelectBG/2").GetComponent<Button>();
        select3 = transform.Find("SelectBG/3").GetComponent<Button>();
        select4 = transform.Find("SelectBG/4").GetComponent<Button>();
        select5 = transform.Find("SelectBG/5").GetComponent<Button>();
        selectBack = transform.Find("SelectBG/Back").GetComponent<Button>();


        FruitsWin = transform.Find("WinBG/FS");
        WinUI = transform.Find("WinBG").gameObject;
        SelectUI = transform.Find("SelectBG").gameObject;



        startButton.onClick.AddListener(OnClickStartButton);
        resetButton.onClick.AddListener(OnClickResetButton);
        pauseButton.onClick.AddListener(OnClickStartButton);

        winBack.onClick.AddListener(OnClickWinBackButton);
        winNext.onClick.AddListener(OnClickNextButton);
        winReset.onClick.AddListener(OnClickResetButton);

        select1.onClick.AddListener(()=> { SelectLevel(1); });
        select2.onClick.AddListener(()=> { SelectLevel(2); });
        select3.onClick.AddListener(()=> { SelectLevel(3); });
        select4.onClick.AddListener(()=> { SelectLevel(4); });
        select5.onClick.AddListener(()=> { SelectLevel(5); });
        selectBack.onClick.AddListener(()=> { SelectLevel(0); });
    }
    public void OnClickStartButton() 
    {
        if (PauseUI())
        {
            foreach (var item in boxGroup)
            {
                item.BoxMove();
            }
        }
    }
    public void OnClickNextButton() 
    {
        if (SceneManager.sceneCountInBuildSettings > SceneManager.GetActiveScene().buildIndex + 1)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        else
        {
            SceneManager.LoadScene(0);
        }
    }
    public void SelectLevel(int curIndex) 
    {
        SceneManager.LoadScene(curIndex);
    }
    public void OnClickResetButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void OnClickWinBackButton()
    {
        SelectUI.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(PauseKey))
        {
            if (PauseUI())
            {
                foreach (var item in boxGroup)
                {
                    item.BoxMove();
                }
            }
        }
    }
    public void CurLevelWin() 
    {
        WinUI.SetActive(true);
    }
    public void PickBerry() 
    {
        curBerryNumber++;
        for (int i = 0; i < curBerryNumber; i++)
        {
            Fruits.GetChild(i).GetChild(0).gameObject.SetActive(false);
        }
        for (int i = 0; i < curBerryNumber; i++)
        {
            FruitsWin.GetChild(i).GetChild(0).gameObject.SetActive(false);
        }
    }

    public bool PauseUI() 
    {
        pauseUI.SetActive(!pauseUI.activeSelf);
        return pauseUI.activeSelf;
    }
}
