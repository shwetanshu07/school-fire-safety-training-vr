using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExtinguisherLogic : MonoBehaviour
{
    [Header("Settings")]
    public bool isGoodExtinguisher = true;
    public float inspectionDistance = 0.5f;

    [Header("References")]
    public Transform playerCamera;
    public XRGrabInteractable pinInteractable; // The Safety Pin
    public XRSocketInteractor pinSocket;      // The Socket
    public ParticleSystem waterSpray;
    public AudioSource warningAudioSource;    // The buzzer error sound
    public GameObject fireObject;

    [Header("Spray Settings")]
    public float extinguishTime = 10f;
    private float currentExtinguishTimer = 0f;

    private XRBaseController currentController;
    private bool isSpraying = false;
    private int currentStep = 1; // 1 idle, 2 grabbed, 3 gauge ok, 4 pin pulled

    void Start()
    {
        // Disable pin grab and ensure socket is active
        if (pinInteractable != null) pinInteractable.enabled = false;
        if (playerCamera == null) playerCamera = Camera.main.transform;
    }

    void Update()
    {
        // STEP 3 GAUGE CHECK (for Good FE)
        if (currentStep == 2 && isGoodExtinguisher)
        {
            float dist = Vector3.Distance(transform.position, playerCamera.position);
            if (dist < inspectionDistance)
            {
                Debug.Log("Gauge OK! Pin is now pullable.");
                currentStep = 3;
                pinInteractable.enabled = true; // Hand can now grab it
            }
        }

        // Haptics and Fire Logic
        if (isSpraying && currentController != null)
        {
            currentController.SendHapticImpulse(0.5f, 0.1f);
            HandleFire();
        }
    }

    public void OnExtinguisherGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        if (isGoodExtinguisher)
        {
            if (currentStep == 1) currentStep = 2;
        }
        else
        {
            // --- FIX FOR BUG 1 ---
            // 1 Force pin to stay stuck to the extinguisher
            if (pinInteractable != null)
            {
                pinInteractable.transform.SetParent(this.transform);
                pinInteractable.enabled = false; // Disable grab so it can't be touched
            }

            // 2 Disable socket so it doesnt try to hold the pin in world space
            if (pinSocket != null) pinSocket.gameObject.SetActive(false);

            Debug.Log("BAD FE GRABBED - Pin Locked & Warning Played");
            PlayWarning();
            currentStep = -1;
        }
    }

    // LINK TO Pin Socket - XR Socket Interactor - Select Exited
    public void OnPinPulled(SelectExitEventArgs args)
    {
        // i only care if the pin left the socket after the gauge check
        if (currentStep == 3)
        {
            Debug.Log("Pin Pulled from Socket");
            currentStep = 4;

            // allow the pin to fall
            Rigidbody rb = pinInteractable.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }
    }

    // LINK TO Extinguisher Parent - XR Grab - Activate
    public void StartSpray(ActivateEventArgs args)
    {
        if (currentStep < 4)
        {
            PlayWarning();
            return;
        }
        isSpraying = true;
        waterSpray.Play();
        currentController = args.interactorObject.transform.GetComponentInParent<XRBaseController>();
    }

    public void StopSpray(DeactivateEventArgs args)
    {
        isSpraying = false;
        waterSpray.Stop();
    }

    void HandleFire()
    {
        RaycastHit hit;
        // Raycast from the water spray nozzle
        if (Physics.Raycast(waterSpray.transform.position, waterSpray.transform.forward, out hit, 10f))
        {
            if (hit.collider.CompareTag("Fire"))
            {
                currentExtinguishTimer += Time.deltaTime;

                if (currentExtinguishTimer >= extinguishTime)
                {
                    // Kill fire object
                    fireObject.SetActive(false);

                    // Stop background sounds
                    if (SceneAudioManager.instance != null)
                    {
                        SceneAudioManager.instance.StopAllEnvironmentSounds();
                    }

                    // 3. Set step to finished so it stops spraying
                    currentStep = 5;
                    isSpraying = false;
                    waterSpray.Stop();
                }
            }
        }
    }

    void PlayWarning()
    {
        if (warningAudioSource != null && !warningAudioSource.isPlaying)
            warningAudioSource.Play();
    }
}