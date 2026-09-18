using UnityEngine;

[DisallowMultipleComponent]
public sealed class PackagingInteraction : WorkAction
{
    [SerializeField] private BoxPackaging packaging;

    public override void Stay(GameObject actor)
    {
        if (packaging == null || !packaging.isActiveAndEnabled) return;
        actor.TryGetComponent<Player>(out var player);
        actor.TryGetComponent<Employee>(out var employee);
        if ((player != null && player.Inventory.IsEmpty)
            || (employee != null && employee.Inventory.IsEmpty))
            packaging.Packaging(player, employee);
    }

    public override void Exit(GameObject actor)
    {
        if (actor.TryGetComponent<Player>(out var player))
            player.StopBoxPackagingAnimationPlayer();
        if (actor.TryGetComponent<Employee>(out var employee))
            employee.StopBoxPackagingAnimationEmployee();
    }
}
