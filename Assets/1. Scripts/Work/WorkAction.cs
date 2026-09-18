using UnityEngine;

public abstract class WorkAction : MonoBehaviour
{
    public virtual void Enter(GameObject actor) { }
    public abstract void Stay(GameObject actor);
    public virtual void Exit(GameObject actor) { }
}
