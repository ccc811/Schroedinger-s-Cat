using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    // Start is called before the first frame update
    public BoxController _other;
    public CatController cat;
    public Transform outPoint;
    public Transform boxMovePos;
    public Vector3 boxMoveStart;
    public bool boxCanMove;
    private bool isMove = false;



    void Start()
    {
        boxMoveStart = transform.position;
        outPoint = transform.Find("outpoint");
        boxCanMove = true;
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
    public void BoxMove() 
    {
        if (boxCanMove && boxMovePos!=null)
        {
            if (!isMove) 
            {
                transform.position = boxMovePos.position;
                isMove = true;
            }              
            else
            {
                transform.position = boxMoveStart;
                isMove = false;

            }
        }
    }
    public void StartIn() 
    {
        if (cat.curLookBox)
            cat.curLookBox.boxCanMove=true;
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
        if (cat.curLookBox)
            cat.curLookBox.boxCanMove = true;


    }
}
