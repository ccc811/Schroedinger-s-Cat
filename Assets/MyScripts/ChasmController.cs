using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChasmController : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag=="Cat")
        {
            collision.transform.GetComponent<CatController>().TakeDamage();
        }
    }
}
