using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartUI : MonoBehaviour
{
    // Start is called before the first frame update
    public Button startGame;
    public Button quitGame;

    private Button select1;
    private Button select2;
    private Button select3;
    private Button select4;
    private Button select5;
    private Button selectBack;
    GameObject SelectUI;



    void Start()
    {
        startGame.onClick.AddListener(StartGameClick);
        quitGame.onClick.AddListener(Quitgame);

        select1 = transform.Find("SelectBG/1").GetComponent<Button>();
        select2 = transform.Find("SelectBG/2").GetComponent<Button>();
        select3 = transform.Find("SelectBG/3").GetComponent<Button>();
        select4 = transform.Find("SelectBG/4").GetComponent<Button>();
        select5 = transform.Find("SelectBG/5").GetComponent<Button>();
        selectBack = transform.Find("SelectBG/Back").GetComponent<Button>();
        SelectUI = transform.Find("SelectBG").gameObject;


        select1.onClick.AddListener(() => { SelectLevel(1); });
        select2.onClick.AddListener(() => { SelectLevel(2); });
        select3.onClick.AddListener(() => { SelectLevel(3); });
        select4.onClick.AddListener(() => { SelectLevel(4); });
        select5.onClick.AddListener(() => { SelectLevel(5); });
        selectBack.onClick.AddListener(() => { SelectLevel(0); });

    }
    public void SelectLevel(int curIndex)
    {
        SceneManager.LoadScene(curIndex);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    void StartGameClick() 
    {
        //if (SceneManager.sceneCountInBuildSettings > SceneManager.GetActiveScene().buildIndex + 1)
        //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        SelectUI.SetActive(true);
    }
    void Quitgame()
    {
        Application.Quit();
    }
}
