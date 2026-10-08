using UnityEngine;

public class SandwichTrashCan : MonoBehaviour
{
    [SerializeField] private AssemblyManager assemblyManager;

    private void OnTriggerEnter(Collider other)
    {
        if (assemblyManager == null)
            return;

        if (assemblyManager.IsCurrentSandwich(other))
        {
            assemblyManager.DiscardCurrentSandwich();
        }
    }
}
