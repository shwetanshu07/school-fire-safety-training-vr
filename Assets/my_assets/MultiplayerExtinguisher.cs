using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

public class MultiplayerExtinguisher : MonoBehaviourPun
{
    [Header("References")]
    public CollabGameManager gameManager;       // reference to main game manager
    public ParticleSystem waterSpray;           // spray particle system

    [Header("Spray Settings")]
    public float suppressionAmount = 0.05f;     // amount to suppress fire per frame
    public float suppressionThreshold = 0.5f;   // send RPC every this many suppression units
    public string fireTag = "Fire";             // tag on fire collider objects

    [Header("Role Lock")]
    public bool roleLockEnabled = true;         // toggle to disable role locking for testing

    [Header("Pin")]
    public XRGrabInteractable pinInteractable;
    public Rigidbody pinRigidbody;

    [Header("Audio")]
    public AudioSource errorAudioSource;

    // state
    private bool isSpraying = false;
    private bool pinPulled = false;
    private XRBaseController currentController;
    private float suppressionAccumulator = 0f;

    void Start()
    {
        // disable pin at start - enabled after extinguisher is grabbed
        if (pinInteractable != null)
        {
            pinInteractable.enabled = false;
            if (pinRigidbody != null) pinRigidbody.useGravity = false;
        }
    }

    // ===== called when extinguisher is grabbed =====
    public void OnExtinguisherGrabbed(SelectEnterEventArgs args)
    {
        // ignore socket interactor grabs
        if (args.interactorObject is XRSocketInteractor) return;

        // role lock - only Fire Responder (Player 1) can grab
        if (roleLockEnabled && !CollabGameManager.isResponder)
        {
            Debug.Log("ROLE LOCK - only Fire Responder can grab extinguisher");
            return;
        }

        // request photon ownership so this client can move the extinguisher
        if (!photonView.IsMine)
            photonView.RequestOwnership();

        // enable pin now that extinguisher is grabbed
        if (pinInteractable != null)
        {
            pinInteractable.enabled = true;
            if (pinRigidbody != null) pinRigidbody.useGravity = true;
        }

        Debug.Log("EXTINGUISHER GRABBED");
    }

    // ===== called when squeeze trigger pressed =====
    public void OnStartSpraying(ActivateEventArgs args)
    {
        // must pull pin first - play warning audio if not
        // WARNING HAPTIC: single long buzz via warning audio trigger
        if (!pinPulled)
        {
            PlayErrorAudio();     // audio feedback for wrong action
            return;
        }

        isSpraying = true;
        waterSpray.Play();

        // get controller for haptics
        currentController = args.interactorObject.transform
            .GetComponentInParent<XRBaseController>();

        // update fire status to in progress on first spray
        if (gameManager != null)
            gameManager.UpdateTaskStatus("fire", "IN PROGRESS");

        Debug.Log("SPRAYING STARTED");
    }

    // ===== called when squeeze trigger released =====
    public void OnStopSpraying(DeactivateEventArgs args)
    {
        isSpraying = false;
        waterSpray.Stop();

        // reset accumulator when stopped spraying
        suppressionAccumulator = 0f;
    }

    // ===== called when pin is grabbed and pulled =====
    public void OnPinPulled(SelectEnterEventArgs args)
    {
        // ignore socket interactor
        if (args.interactorObject is XRSocketInteractor) return;

        pinPulled = true;
        Debug.Log("PIN PULLED");
    }

    void Update()
    {
        if (!isSpraying || currentController == null) return;

        // HAPTIC FEEDBACK: continuous vibration while spraying - 0.5 intensity, 0.1 duration per frame
        // this gives the feel of the extinguisher operating in the user's hand
        currentController.SendHapticImpulse(0.5f, 0.1f);

        // handle spray direction and fire suppression
        HandleSprayLogic();
    }

    void HandleSprayLogic()
    {
        if (gameManager == null) return;

        RaycastHit hit;
        // shoot ray from water spray position and direction - same as Scene 1
        if (Physics.Raycast(waterSpray.transform.position, waterSpray.transform.forward, out hit, 10f))
        {
            if (hit.collider.CompareTag(fireTag))
            {
                if (errorAudioSource != null && errorAudioSource.isPlaying)
                    errorAudioSource.Stop();

                suppressionAccumulator += suppressionAmount * Time.deltaTime;

                if (suppressionAccumulator >= suppressionThreshold)
                {
                    gameManager.OnFireSuppressed(suppressionAccumulator);
                    suppressionAccumulator = 0f;
                }
            }
            else
            {
                PlayErrorAudio();
            }
        }
        else
        {
            PlayErrorAudio();
        }
    }

    void PlayErrorAudio()
    {
        // only play if not already playing - prevents audio stacking
        if (errorAudioSource != null && !errorAudioSource.isPlaying)
            errorAudioSource.Play();
    }
}