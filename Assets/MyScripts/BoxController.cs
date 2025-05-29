using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    // Start is called before the first frame update
    public BoxController _other;
    public CatController cat;
    public Transform outPoint;
    void Start()
    {
        outPoint = transform.Find("outpoint");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == ("Cat"))
        {
            cat = collision.transform.GetComponent<CatController>();
            cat.curBox = this;
            cat.isInBoxForward = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.tag == ("Cat"))
        {
            if (cat!=null)
            {
                cat.isInBoxForward = false;
            }
        }
    }
    public void StartIn() 
    {
        cat.transform.gameObject.SetActive(false);
        transform.GetComponent<Animator>().Play("boxin");
    }

    public void PlayerInOver() 
    {
        cat.transform.position = _other.outPoint.position;
        _other.cat = cat;
        _other.transform.GetComponent<Animator>().Play("boxout");
    }
    public void PlayerOutOver() 
    {
        cat.gameObject.SetActive(true);
    }
}
