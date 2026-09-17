using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeObject : MonoBehaviour
{
    [SerializeField] private GameObject objectB; // 교체될 오브젝트
    [SerializeField] private Transform newTransform;

    private PoolingManager pool;

    private void Start()
    {
        pool = PoolingManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ingredient"))
        {
            GameObject newObject = pool.GetObj(objectB);
            newObject.transform.position = newTransform.position; 

            Rigidbody rd = newObject.GetComponent<Rigidbody>();

            if(rd == null)
            {
                rd = newObject.AddComponent<Rigidbody>();
                rd.freezeRotation = true;
            }

            rd.linearVelocity = other.GetComponent<Rigidbody>().linearVelocity;
            pool.ReturnObjecte(other.gameObject);
        }
    }
}
