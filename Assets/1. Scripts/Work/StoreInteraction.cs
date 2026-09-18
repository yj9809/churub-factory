using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoreInteraction : WorkAction
{
    private UIManager ui;
    private AudioManager audio;

    public override void Enter(GameObject actor)
    {
        if (actor.TryGetComponent<Player>(out _))
        {
            audio = AudioManager.Instance;
            audio.PlayEffect(EffectType.Store);
        }
    }

    public override void Stay(GameObject actor)
    {
        if (actor.TryGetComponent<Player>(out _))
        {
            ui = UIManager.Instance;
            ui.ShowStoreUI();
        }
    }

    public override void Exit(GameObject actor)
    {
        if (!actor.TryGetComponent<Player>(out _)) return;
        // During scene shutdown, do not create replacement singleton objects.
        if (ui != null) ui.CloseStoreUI();
        if (audio != null) audio.PlayEffect(EffectType.Store);
    }
}
