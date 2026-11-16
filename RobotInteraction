
using UnityEngine;
using LastSeed.Domain;

public class RobotInteraction : MonoBehaviour
{
    [SerializeField] private float interactDistance = 2.0f;
    [SerializeField] private Transform rayOrigin;   

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) Interact();
    }

    private void Interact()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Transform o = rayOrigin != null ? rayOrigin : transform;
        if (!Physics.Raycast(o.position, o.forward, out RaycastHit hit, interactDistance)) return;

        var target = hit.collider.GetComponentInParent<Interactable>();
        if (target == null) return;

        switch (target.type)
        {
            case InteractableType.WaterWell:
                gm.Rules.CollectWater();
                break;
            case InteractableType.PlantSpotPS1:
                
                if (gm.State.PS1.Stage == PlantStage.None) gm.Rules.PlantPioneer();
                else gm.Rules.WaterPlant();
                break;
        }
        gm.NotifyChanged();
    }
}
