using UnityEngine;

[DisallowMultipleComponent]
public sealed class UpgradeInteraction : WorkAction
{
    private UIManager ui;

    public override void Stay(GameObject actor)
    {
        if (actor.TryGetComponent<Player>(out _))
        {
            ui = UIManager.Instance;
            ui.ShowUpgradeUI();
        }
    }

    public override void Exit(GameObject actor)
    {
        if (actor.TryGetComponent<Player>(out _) && ui != null)
            ui.CloseUpgradeUI();
    }
}
