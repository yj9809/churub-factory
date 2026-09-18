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
        if (other.gameObject.activeInHierarchy && other.TryGetComponent<Item>(out var item)
            && item.Type == ItemType.Ingredient && !item.IsStored
            && other.TryGetComponent<Rigidbody>(out var sourceBody))
        {
            GameObject newObject = pool.GetObj(objectB);
            newObject.transform.position = newTransform.position; 

            Rigidbody rd = newObject.GetComponent<Rigidbody>();

            if(rd == null)
            {
                rd = newObject.AddComponent<Rigidbody>();
                rd.freezeRotation = true;
            }

            rd.linearVelocity = sourceBody.linearVelocity;
            pool.ReturnObjecte(other.gameObject);
        }
    }
}
