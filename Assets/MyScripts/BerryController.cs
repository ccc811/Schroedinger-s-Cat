using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BerryController : MonoBehaviour
{
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == ("Cat"))
        {
            UIController._instance.PickBerry();
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
