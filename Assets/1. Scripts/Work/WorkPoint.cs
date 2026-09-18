using System.Collections.Generic;
using UnityEngine;

public class WorkPoint : MonoBehaviour
{
    [SerializeField] private WorkAction action;
    private readonly Dictionary<Collider, GameObject> occupants = new Dictionary<Collider, GameObject>();

    private GameObject Track(Collider other)
    {
        if (!isActiveAndEnabled || action == null || !action.isActiveAndEnabled)
            return null;
        if (occupants.TryGetValue(other, out var actor))
            return actor;
        if (!other.TryGetComponent<Player>(out _) && !other.TryGetComponent<Employee>(out _))
            return null;

        actor = other.gameObject;
        bool alreadyInside = occupants.ContainsValue(actor);
        occupants.Add(other, actor);
        if (!alreadyInside)
            action.Enter(actor);
        return actor;
    }

    private void OnTriggerEnter(Collider other)
    {
        Track(other);
    }

    private void OnTriggerStay(Collider other)
    {
        var actor = Track(other);
        if (actor != null)
            action.Stay(actor);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!occupants.TryGetValue(other, out var actor))
            return;
        occupants.Remove(other);
        if (action != null && actor != null && !occupants.ContainsValue(actor))
            action.Exit(actor);
    }

    private void OnDisable()
    {
        var actors = new HashSet<GameObject>(occupants.Values);
        occupants.Clear();
        if (action == null) return;
        foreach (var actor in actors)
            if (actor != null) action.Exit(actor);
    }
}
