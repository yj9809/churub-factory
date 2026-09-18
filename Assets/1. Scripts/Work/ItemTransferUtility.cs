using DG.Tweening;
using UnityEngine;

public static class ItemTransferUtility
{
    public static bool TryMove(ItemBuffer source, ItemBuffer destination, Transform parent,
        bool useWorldPosition = false)
    {
        if (source == null || destination == null || parent == null || !source.TryPeek(out var item)
            || item == null || !item.TryGetComponent<BoxCollider>(out var collider))
            return false;

        float height = collider.size.y * destination.Count;
        if (!source.TryMoveTo(destination, out item))
            return false;

        if (useWorldPosition)
        {
            item.transform.DOKill();
            item.transform.SetParent(parent);
            item.transform.DOMove(parent.position, 0.2f).SetEase(Ease.InBack);
        }
        else
        {
            MoveToStack(item, parent, height);
        }
        return true;
    }

    public static bool TryCollect(Item item, ItemBuffer destination, Transform parent,
        int? stackIndex = null, bool animate = true)
    {
        if (item == null || destination == null || parent == null
            || !item.TryGetComponent<BoxCollider>(out var collider))
            return false;

        float height = collider.size.y * (stackIndex ?? destination.Count);
        if (!destination.TryAdd(item))
            return false;

        if (item.TryGetComponent<Rigidbody>(out var body))
            Object.Destroy(body);
        if (animate)
            MoveToStack(item, parent, height);
        else
        {
            item.transform.DOKill();
            item.transform.SetParent(parent);
            item.transform.localPosition = new Vector3(0, height, 0);
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
        }
        return true;
    }

    private static void MoveToStack(Item item, Transform parent, float height)
    {
        var target = item.transform;
        target.DOKill();
        target.SetParent(parent);
        target.DOLocalMove(new Vector3(0, height, 0), 0.2f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                target.localRotation = Quaternion.identity;
                target.localScale = Vector3.one;
            });
    }
}
