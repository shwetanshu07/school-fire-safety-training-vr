using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;   // needed for croutine

public class FireTrainingManager : MonoBehaviour
{
    // ===== for the avatars::STARTS =====
    [Header("Avatar Selection")]
    public GameObject selectionCanvas;
    public static bool isNovice = true;
    private bool hasSelected = false;
    // ===== for the avatars::ENDS =====

    [Header("Timer")]
    public GameObject timerCanvas;
    public TextMeshProUGUI timerText;
    private float elapsedTime = 0f;
    private bool timerRunning = false;
    
    [Header("UI & Progress Board")]
    // public Button startButton;
    // public TextMeshProUGUI startButtonText;
    public Image[] boardCheckmarks; // array for the checkmark images
    public Sprite completedSprite;  // green tick svg

    [Header("Audio")]
    public AudioSource stepAudioSource;
    public AudioClip[] stepAudioClips; // array of audio for each step

    [Header("Extinguisher Controls")]
    public XRGrabInteractable extinguisherGrab;
    public MeshRenderer topHandleRenderer;
    public Material glowingMaterial;
    private Material normalHandleMaterial;

    [Header("Step 3: Gauge")]
    public Transform playerCamera; // drag main camera here
    public float inspectionDistance = 0.8f; // distance to trigger gaze

    [Header("Step 4: Safety Pin")]
    public XRGrabInteractable pinInteractable;
    public MeshRenderer pinRenderer;
    private Material normalPinMaterial;

    [Header("Step 5: Water Spray & Haptics")]
    public ParticleSystem waterSpray;
    private XRBaseController currentController;
    private bool isSpraying = false;

    [Header("Step 6: Fire Logic")]
    public GameObject fireObject; // fireee
    public float extinguishTime = 5f; // Seconds required to kill fire
    private float currentExtinguishTimer = 0f;
    public AudioSource errorAudioSource; // spraying wrong place sound
    public AudioSource warningAudioSource;

    [Header("Proficient Spatial Audio")]
    public AudioSource fireAlarmAudioSource;
    public AudioSource fireCrackleAudioSource;
    public float baseCrackleVolume = 1f;
    public float crackleVolumeIncreasePerStage = 2f;

    [Header("Warning Spatial Text")]
    public GameObject fireWarningCanvas;
    public TextMeshProUGUI fireWarningText;
    public GameObject aimWarningCanvas;
    public TextMeshProUGUI aimWarningText;

    // fire growth
    private float fireGrowthTimer = 0f;
    private bool fireGrowthActive = false;
    private float fireGrowthInterval = 7f;
    private Vector3 originalFireScale;

    private int currentStep = 0;

    private bool fireHasGrown = false;  // check flag for preventing excess fire growth as it was messing with colliders

    void Start()
    {
        // lock everything at start
        extinguisherGrab.enabled = false;
        pinInteractable.enabled = false;

        // hide timer at first
        if (timerCanvas != null) timerCanvas.SetActive(false);

        // save the normal material of the handle and pin as they glow
        normalHandleMaterial = topHandleRenderer.material;
        if (pinRenderer != null) normalPinMaterial = pinRenderer.material;

        // save the original fire scale at start
        if (fireObject != null) originalFireScale = fireObject.transform.localScale;
    }

    public void SelectNovice()
    {
        if (hasSelected) return;
        hasSelected = true;
        isNovice = true;
        BeginTraining();
        Debug.Log("NOVICE SELECTED");
    }

    public void SelectProficient()
    {
        if (hasSelected) return;
        hasSelected = true;
        isNovice = false;
        BeginTraining();
        Debug.Log("PROFICIENT SELECTED");
    }

    void BeginTraining()
    {
        selectionCanvas.SetActive(false);
        Step1_StartClicked();
    }


    // --- STEP 1 START BUTTON ---
    public void Step1_StartClicked()
    {
        if (currentStep != 0) return;

        // Play Audio based on the avatar selected
        if (isNovice)
        {
            PlayAudio(0);
        }
        else
        {
            PlayAudio(6);
            timerCanvas.SetActive(true);    // show timer. initally frozen at 00:00
            StartCoroutine(StartTimerAfterAudio()); // start counting after audio ends
        }
        
        // Unlock Extinguisher
        extinguisherGrab.enabled = true;

        if (isNovice) topHandleRenderer.material = glowingMaterial;   // add glow

        currentStep = 1;
    }

    // NOTE this is being called only for proficient ... but it is tightly bound to timer so may need to refactor it later
    IEnumerator StartTimerAfterAudio()
    {
        yield return new WaitForSeconds(stepAudioClips[6].length);
        timerRunning = true;
        fireGrowthActive = true;    // start watching for inaction for fire

        // start spatial audios for proficient
        if (fireAlarmAudioSource != null) fireAlarmAudioSource.Play();
        if (fireCrackleAudioSource != null) fireCrackleAudioSource.Play();
    }

    // --- STEP 2 GRAB EXTINGUISHER ---
    public void Step2_ExtinguisherGrabbed(SelectEnterEventArgs args)
    {
        // Check if the thing grabbing the extinguisher is a Socket
        // If it is a socket IGNORE IT and exit the function
        if (args.interactorObject is XRSocketInteractor)
        {
            Debug.Log("IGNORING GRAB OF EXTINGUISHER BY SOCKET");
            return;
        }

        if (currentStep != 1) return;

        Debug.Log("GRABBED EXTINGUISHER BY HAND");
        
        CompleteBoardObjective(0); // Update Progress Board ... grab is step 0 on board
        
        if (isNovice)
        {
            topHandleRenderer.material = normalHandleMaterial; // stopoing the glow now
            PlayAudio(1); // play audio for guage check
        }

        currentStep = 2;
    }

    // --- STEP 3 GUAGE CHECK ---
    public void Step3_GaugeChecked()
    {
        Debug.Log("GAUGE CLICKED!");

        if (currentStep != 2) return;

        Debug.Log("GAUGE INSPECTED BY LOOKING");
        CompleteBoardObjective(1);

        if (isNovice) PlayAudio(2);

        pinInteractable.enabled = true;
        if (isNovice) pinRenderer.material = glowingMaterial;
        // re-enable gravity so pin drops naturally after user pulls it out
        Rigidbody pinRb = pinInteractable.GetComponent<Rigidbody>();
        if (pinRb != null) pinRb.useGravity = true;


        currentStep = 3;
    }

    // --- STEP 4 PIN PULL ----
    public void Step4_PinPulled(SelectEnterEventArgs args)
    {
        // if socket is grabbing pin ignore it
        if (args.interactorObject is XRSocketInteractor) return;

        if (currentStep != 3) return;

        if (isNovice) pinRenderer.material = normalPinMaterial; // stop pin from glowing

        CompleteBoardObjective(2);
        if (isNovice) PlayAudio(3);

        currentStep = 4;
    }

    // --- STEP 5 AIM and SPRAY ----
    public void StartSpraying(ActivateEventArgs args)
    {
        // ensure the pin is pulled (Step 4) before allowing spray
        if (currentStep < 4) {
            PlayWarningAudio();
            return;
        }

        isSpraying = true;
        waterSpray.Play();

        // this finds the controller currently holding the extinguisher
        currentController = args.interactorObject.transform.GetComponentInParent<XRBaseController>();

        // Step 5 logic first time spraying
        if (currentStep == 4)
        {
            CompleteBoardObjective(3); // aim and squeeze tick
            if (isNovice) PlayAudio(4);
            currentStep = 5;
            Debug.Log("Squeeze Successful: Spraying started.");
        }
    }

    public void StopSpraying(DeactivateEventArgs args)
    {
        isSpraying = false;
        waterSpray.Stop();
    }

    // always being called as long as the game is running
    void Update()
    {
        // timer counting logic
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = string.Format("Elapsed Time: {0:00}:{1:00}", minutes, seconds);
        }

        // fire growth for proficient user
        if (!isNovice && fireGrowthActive && fireObject.activeSelf)
        {
            fireGrowthTimer += Time.deltaTime;

            if (fireGrowthTimer >= fireGrowthInterval)
            {
                fireGrowthTimer = 0f;
                GrowFire();
            }
        }

        // for haptic when squeezing
        if (isSpraying && currentController != null)
        {
            // 0.5f is intensity (0 to 1), 0.1f is duration
            currentController.SendHapticImpulse(0.5f, 0.1f);
            HandleFireLogic();
        }

        // STEP 3 PROXIMITY CHECK GUAGE
        if (currentStep == 2)
        {
            float distance = Vector3.Distance(extinguisherGrab.transform.position, playerCamera.position);
            if (distance < inspectionDistance)
            {
                Step3_GaugeChecked();
            }
        }
    }

    void GrowFire()
    {
        if (fireHasGrown) return;       // only grow once
        fireHasGrown = true;
        fireGrowthActive = false;       // stop the timer from triggering this again

        // grow the fire object scale by 25% each interval
        fireObject.transform.localScale *= 1.5f;

        // also increase particle system start size to match
        var main = fireObject.GetComponent<ParticleSystem>().main;
        main.startSize = main.startSize.constant * 1.25f;

        // shift colour to more reddish
        main.startColor = new Color(1f, 0.165f, 0.016f, 1f);

        // increase crackle volume as fire grows
        if (fireCrackleAudioSource != null)
            fireCrackleAudioSource.volume = Mathf.Min(fireCrackleAudioSource.volume + crackleVolumeIncreasePerStage, 4f);

        // show floating warning for proficient user
        if (!isNovice && fireWarningCanvas != null)
        {
            fireWarningText.text = "Fire spreading!";
            fireWarningCanvas.SetActive(true);
            StartCoroutine(HideFireWarning());
        }

        Debug.Log("FIRE GREW: " + fireObject.transform.localScale.ToString());
    }

    IEnumerator HideFireWarning()
    {
        yield return new WaitForSeconds(5f);  // sign persists for x sec and then disappears
        if (fireWarningCanvas != null)
            fireWarningCanvas.SetActive(false);
    }

    void HandleFireLogic()
    {
        RaycastHit hit;
        // shoot the ray from the nozzle
        if (Physics.Raycast(waterSpray.transform.position, waterSpray.transform.forward, out hit, 10f))
        {
            // check if the object we hit has fire tag
            if (hit.collider.CompareTag("Fire"))
            {
                fireGrowthActive = false;    // correct action stops the fire growing
                currentExtinguishTimer += Time.deltaTime;
                Debug.Log("EXTINGUISHING: " + currentExtinguishTimer.ToString("F1") + "s");

                // stop error audio if it was playing

                if (errorAudioSource.isPlaying) errorAudioSource.Stop();
                if (currentExtinguishTimer >= extinguishTime && currentStep == 5)
                {
                    FinishTraining();
                }
            }
            else
            {
                PlayErrorAudio();
                ShowAimWarning();
            }
        }
        else
        {
            PlayErrorAudio();
            ShowAimWarning();
        }
    }

    void ShowAimWarning()
    {
        if (!isNovice || aimWarningCanvas == null) return;
        aimWarningText.text = "Aim at the base of fire-->";
        aimWarningCanvas.SetActive(true);
        StopCoroutine("HideAimWarning");        // prevent overlapping coroutines
        StartCoroutine("HideAimWarning");
    }

    IEnumerator HideAimWarning()
    {
        yield return new WaitForSeconds(3f);
        if (aimWarningCanvas != null)
            aimWarningCanvas.SetActive(false);
    }

    void PlayErrorAudio()
    {
        // only play if its NOT already playing
        if (!errorAudioSource.isPlaying)
        {
            errorAudioSource.Play();
        }
    }

    void PlayWarningAudio()
    {
        if (warningAudioSource != null && !warningAudioSource.isPlaying)
        {
            warningAudioSource.Play();
        }
    }


    void FinishTraining()
    {
        isSpraying = false;
        waterSpray.Stop();
        fireObject.SetActive(false); // Kill fire

        timerRunning = false; // stop timer
        fireGrowthActive = false;    // stop fire growth on completion

        // stop spatial audios
        if (fireAlarmAudioSource != null) fireAlarmAudioSource.Stop();
        if (fireCrackleAudioSource != null) fireCrackleAudioSource.Stop();

        CompleteBoardObjective(4); // Final checkmark
        PlayAudio(5); // Victory audio

        currentStep = 6;
        Debug.Log("TRAINING COMPLETE!");
    }

    // Utility Methods
    private void CompleteBoardObjective(int index)
    {
        if (index < boardCheckmarks.Length)
            boardCheckmarks[index].sprite = completedSprite;
    }

    private void PlayAudio(int clipIndex)
    {
        if (clipIndex < stepAudioClips.Length)
        {
            stepAudioSource.clip = stepAudioClips[clipIndex];
            stepAudioSource.Play();
        }
    }
}