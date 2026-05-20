using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticContinuous : MonoBehaviour
{
    [SerializeField] float amplitude = 0.4f;

    private ActionBasedController heldBy;

    void Start()
    {
        var i = GetComponent<XRBaseInteractable>();
        i.selectEntered.AddListener(OnGrab);
        i.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        heldBy = args.interactorObject.transform
            .GetComponentInParent<ActionBasedController>();
        Debug.Log("Grabbed! Controller: " + heldBy);
    }

    void Update()
    {
        if (heldBy != null)
            heldBy.SendHapticImpulse(amplitude, Time.deltaTime + 0.01f);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        heldBy = null;
    }
}