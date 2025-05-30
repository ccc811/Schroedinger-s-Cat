using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyController: MonoBehaviour
{
    // Start is called before the first frame update
    // public BoxController _other;
    public CatController cat;
    public GameObject doorOpen;
    public GameObject doorClose;
    // public Transform outPoint;
    void Start()
    {
        // outPoint = transform.Find("outpoint");
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == ("Cat"))
        {
            doorOpen.SetActive(true);
            doorClose.SetActive(false);
            transform.gameObject.SetActive(false);
        }
    }

    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    if (collision.transform.tag == ("Cat"))
    //    {
    //        if (cat != null)
    //        {
    //            cat.isInDoor = false;
    //        }
    //    }
    //}
}
