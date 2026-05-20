using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HapticPattern : MonoBehaviour
{
    private ActionBasedController controller;

    void Start()
    {
        var i = GetComponent<XRBaseInteractable>();
        i.selectEntered.AddListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        controller = args.interactorObject.transform
            .GetComponentInParent<ActionBasedController>();
        Debug.Log("Pattern grabbed! Controller: " + controller);

        if (controller != null)
            StartCoroutine(PlayPattern());
        else
            Debug.LogWarning("No controller found!");
    }

    IEnumerator PlayPattern()
    {
        float[] timings = {
            0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f,
            0.25f, 0.1f,  0.25f, 0.1f,  0.25f, 0.2f,
            0.08f, 0.08f, 0.08f, 0.08f, 0.08f
        };

        bool vibrate = true;
        foreach (float t in timings)
        {
            if (vibrate && controller != null)
                controller.SendHapticImpulse(1f, t);
            yield return new WaitForSeconds(t);
            vibrate = !vibrate;
        }
    }
}