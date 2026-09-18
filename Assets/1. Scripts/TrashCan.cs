using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private Player player;
    private Animator animator;

    private void Start()
    {
        player = GameManager.Instance.P;
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!player.Inventory.IsEmpty)
            {
                ClearInventory(player.Inventory);
            }
        }
    }
    private void ClearInventory(CarrierInventory inventory)
    {
        animator.SetTrigger("TrashCan");
        while (inventory.TryPop(out var item))
        {
            if (item == null) continue;
            PoolingManager.Instance.ReturnObjecte(item.gameObject);
        }
    }
}
