using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticSingleImpulse : MonoBehaviour
{
    [SerializeField] float amplitude = 0.8f;
    [SerializeField] float duration = 0.1f;

    void Start()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.AddListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        var interactor = args.interactorObject;
        if (interactor.transform.TryGetComponent(out XRBaseController ctrl))
        {
            ctrl.SendHapticImpulse(amplitude, duration);
        }
    }
}
