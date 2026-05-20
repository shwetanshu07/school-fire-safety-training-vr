using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class HazardManager : MonoBehaviour
{
    // ===== avatar selection
    [Header("Avatar Selection")]
    public GameObject selectionCanvas;
    public static bool isNovice = true;
    private bool hasSelected = false;

    // ===== board canvas
    [Header("Hazard Board")]
    public GameObject hazardBoardCanvas;
    public TextMeshProUGUI boardHeadingText;
    public TextMeshProUGUI boardBodyText;

    // ===== end button for proficient users only
    [Header("End Button")]
    public GameObject endButtonCanvas;

    // ===== audio
    [Header("Audio")]
    public AudioSource audioSource; // main audio source for instructions
    public AudioClip noviceStartClip;   // audio for novice on start
    public AudioClip proficientStartClip;   // audio for proficient on start
    public AudioClip hazardFixedClip;   // sound when hazard is fixed
    public AudioClip completionClip;  // plays on scene completion

    // ===== proximity labels
    [Header("Proximity Labels")]
    public GameObject extinguisherLabelCanvas;
    public TextMeshProUGUI extinguisherLabelText;
    public GameObject burnerLabelCanvas;
    public TextMeshProUGUI burnerLabelText;
    public GameObject boxLabelCanvas;
    public TextMeshProUGUI boxLabelText;

    // ===== hazard objects
    [Header("Hazard Objects")]
    public Transform extinguisherObject;    // the lying extinguisher
    public Transform burnerObject;          // the bunsen burner knob
    public ParticleSystem burnerFireParticle;   // fire particle on burner to stop when fixed
    public Transform boxObject1;

    [Header("Novice Proximity Feedback")]
    public XRBaseController leftController;
    public XRBaseController rightController;
    private float proximityHapticTimer = 0f;
    public float proximityHapticInterval = 0.8f; // pulse every x seconds

    public float proximityDistance = 2f;  // distance to trigger label

    // distance based check for box
    private Vector3 boxStartPosition;

    public float boxMoveDistance = 1f; // how far box must be moved to count as fixed

    private bool completionAudioPlayed = false;

    // ===== states
    private bool extinguisherFixed = false;
    private bool burnerFixed = false;
    private bool boxFixed = false;

    // ===== timer
    private float elapsedTime = 0f;
    private bool timerRunning = false;

    // ===== player
    [Header("Player")]
    public Transform playerCamera;          // main camera / headset

    void Start()
    {
        // hide all canvases at start except avatar selection
        hazardBoardCanvas.SetActive(false);
        endButtonCanvas.SetActive(false);

        // hide all proximity labels at start
        extinguisherLabelCanvas.SetActive(false);
        burnerLabelCanvas.SetActive(false);
        boxLabelCanvas.SetActive(false);

        if (boxObject1 != null) boxStartPosition = boxObject1.position;
    }

    // ===== avatar selection =====

    public void SelectNovice()
    {
        if (hasSelected) return;
        hasSelected = true;
        isNovice = true;
        BeginScene();
        Debug.Log("NOVICE SELECTED");
    }

    public void SelectProficient()
    {
        if (hasSelected) return;
        hasSelected = true;
        isNovice = false;
        BeginScene();
        Debug.Log("PROFICIENT SELECTED");
    }

    void BeginScene()
    {
        selectionCanvas.SetActive(false);
        hazardBoardCanvas.SetActive(true);

        if (isNovice)
        {
            // show novice checklist immediately
            boardHeadingText.text = "Find and fix the 3 issues in this lab.";
            boardBodyText.text = "? Issue 1\n? Issue 2\n? Issue 3";
            audioSource.clip = noviceStartClip;
            audioSource.Play();
        }
        else
        {
            // show proficient in-progress board and end button
            boardHeadingText.text = "Inspection In Progress";
            boardBodyText.text = "Time Elapsed: 00:00";
            endButtonCanvas.SetActive(true);
            audioSource.clip = proficientStartClip;
            audioSource.Play();

            // start timer after audio finishes
            StartCoroutine(StartTimerAfterAudio());
        }

        // for the proximity labels
        if (isNovice)
        {
            extinguisherLabelText.text = "Extinguisher is on its side!\nPick it up and place it in the wall mount.";
            burnerLabelText.text = "Bunsen burner is unattended!\nTurn the knob to shut off the gas and prevent fire.";
            boxLabelText.text = "Fire exit is blocked!\nMove the boxes away from the door.";
        }
        else
        {
            extinguisherLabelText.text = "Extinguisher improperly stored.";
            burnerLabelText.text = "Unattended heat source detected.";
            boxLabelText.text = "Emergency exit obstructed.";
        }
    }

    // wait for proficient start audio to finish then start timer
    IEnumerator StartTimerAfterAudio()
    {
        yield return new WaitForSeconds(proficientStartClip.length);
        timerRunning = true;
    }

    // ===== timer

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        boardBodyText.text = string.Format("Time Elapsed: {0:00}:{1:00}", minutes, seconds);
    }

    // ===== proximity label logic
    // void HandleProximityLabels()
    // {
    //     if (!extinguisherFixed)
    //         HandleSingleLabel(extinguisherObject, extinguisherLabelCanvas);

    //     if (!burnerFixed)
    //         HandleSingleLabel(burnerObject, burnerLabelCanvas);

    //     if (!boxFixed)
    //     {
    //         Vector3 playerFlat = new Vector3(playerCamera.position.x, 0, playerCamera.position.z);
    //         Vector3 boxFlat = new Vector3(boxObject1.position.x, 0, boxObject1.position.z);
    //         float distance = Vector3.Distance(playerFlat, boxFlat);
    //         if (distance < proximityDistance)
    //             boxLabelCanvas.SetActive(true);
    //         else
    //             boxLabelCanvas.SetActive(false);
    //     }
    // }
    void HandleProximityLabels()
    {
        bool nearAnyHazard = false;

        if (!extinguisherFixed)
        {
            bool near = HandleSingleLabel(extinguisherObject, extinguisherLabelCanvas);
            if (near) nearAnyHazard = true;
        }

        if (!burnerFixed)
        {
            bool near = HandleSingleLabel(burnerObject, burnerLabelCanvas);
            if (near) nearAnyHazard = true;
        }

        if (!boxFixed)
        {
            Vector3 playerFlat = new Vector3(playerCamera.position.x, 0, playerCamera.position.z);
            Vector3 boxFlat = new Vector3(boxObject1.position.x, 0, boxObject1.position.z);
            float distance = Vector3.Distance(playerFlat, boxFlat);
            bool near = distance < proximityDistance;
            boxLabelCanvas.SetActive(near);
            if (near) nearAnyHazard = true;
        }

        // trigger proximity feedback if near any unfixed hazard
        TriggerProximityFeedback(nearAnyHazard);
    }

    // void HandleSingleLabel(Transform hazard, GameObject labelCanvas)
    // {
    //     if (hazard == null || labelCanvas == null) return;

    //     // ignore y axis only check horizontal distance
    //     Vector3 playerFlat = new Vector3(playerCamera.position.x, 0, playerCamera.position.z);
    //     Vector3 hazardFlat = new Vector3(hazard.position.x, 0, hazard.position.z);
        
    //     float distance = Vector3.Distance(playerFlat, hazardFlat);

    //     if (distance < proximityDistance)
    //         labelCanvas.SetActive(true);
    //     else
    //         labelCanvas.SetActive(false);
    // }
    bool HandleSingleLabel(Transform hazard, GameObject labelCanvas)
    {
        if (hazard == null || labelCanvas == null) return false;

        Vector3 playerFlat = new Vector3(playerCamera.position.x, 0, playerCamera.position.z);
        Vector3 hazardFlat = new Vector3(hazard.position.x, 0, hazard.position.z);
        float distance = Vector3.Distance(playerFlat, hazardFlat);

        bool isNear = distance < proximityDistance;
        labelCanvas.SetActive(isNear);
        return isNear;
    }

    // ===== callbacks when a hazard is fixed
    // these will be called from the inspector or trigger scripts
    public void FixExtinguisher()
    {
        if (extinguisherFixed) return;
        extinguisherFixed = true;
        extinguisherLabelCanvas.SetActive(false);
        PlayFixedSound();
        UpdateBoard();
        Debug.Log("EXTINGUISHER FIXED");
    }

    public void FixBurner()
    {
        if (burnerFixed) return;
        burnerFixed = true;
        burnerLabelCanvas.SetActive(false);

        // turn off burner fire particle system
        if (burnerFireParticle != null) burnerFireParticle.Stop();

        PlayFixedSound();
        UpdateBoard();
        Debug.Log("BURNER FIXED");
    }

    public void FixBoxes()
    {
        if (boxFixed) return;
        boxFixed = true;
        boxLabelCanvas.SetActive(false);
        PlayFixedSound();
        UpdateBoard();
        Debug.Log("BOXES FIXED");
    }

    // ===== board updates
    void UpdateBoard()
    {
        if (isNovice)
        {
            // update novice checklist with strikethrough for fixed items
            string extText = extinguisherFixed ? "<s>Extinguisher on its side</s>" : "? Issue 1";
            string burnText = burnerFixed ? "<s>Burner left on unattended</s>" : "? Issue 2";
            string boxText = boxFixed ? "<s>Fire exit blocked by boxes</s>" : "? Issue 3";
            boardBodyText.text = extText + "\n" + burnText + "\n" + boxText;

            // play completion audio once when all 3 fixed
            if (extinguisherFixed && burnerFixed && boxFixed && !completionAudioPlayed)
            {
                completionAudioPlayed = true;
                audioSource.PlayOneShot(completionClip);
            }
        }
        // proficient board only updates on end button press
    }

    // end button
    public void OnInspectionComplete()
    {
        timerRunning = false;

        // format final time
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

        // build resolved and missed lists
        string resolved = "";
        string missed = "";

        if (extinguisherFixed) resolved += "Extinguisher on its side\n";
        else missed += "Extinguisher on its side\n";

        if (burnerFixed) resolved += "Burner left on unattended\n";
        else missed += "Burner left on unattended\n";

        if (boxFixed) resolved += "Fire exit blocked\n";
        else missed += "Fire exit blocked\n";

        int fixedCount = (extinguisherFixed ? 1 : 0) + (burnerFixed ? 1 : 0) + (boxFixed ? 1 : 0);

        // build final report text
        boardHeadingText.text = "Inspection Report";
        boardBodyText.text =
            "Total Time Taken: " + timeString + "\n\n" +
            "Issues Resolved:\n" + (resolved == "" ? "None\n" : resolved) + "\n" +
            "Issues Missed:\n" + (missed == "" ? "None\n" : missed) + "\n" +
            "Result: " + fixedCount + " of 3 hazards fixed";

        // hide end button after pressed
        endButtonCanvas.SetActive(false);

        audioSource.PlayOneShot(completionClip);

        Debug.Log("INSPECTION COMPLETE");
    }

    void PlayFixedSound()
    {
        if (hazardFixedClip != null)
            audioSource.PlayOneShot(hazardFixedClip);
    }

    void Update()
    {
        // update timer display every frame for proficient
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        // check proximity labels every frame
        if (hasSelected)
            HandleProximityLabels();

        // check if box has been moved far enough from its start position
        if (hasSelected && !boxFixed && boxObject1 != null)
        {
            float distanceMoved = Vector3.Distance(boxObject1.position, boxStartPosition);
            if (distanceMoved >= boxMoveDistance)
            {
                FixBoxes();
            }
        }
        
    }

    void TriggerProximityFeedback(bool isInProximity)
    {
        if (!isNovice) return;

        if (isInProximity)
        {
            // pulse haptics at interval so it doesnt buzz constantly
            proximityHapticTimer += Time.deltaTime;
            if (proximityHapticTimer >= proximityHapticInterval)
            {
                proximityHapticTimer = 0f;
                if (leftController != null) leftController.SendHapticImpulse(0.4f, 0.1f);
                if (rightController != null) rightController.SendHapticImpulse(0.4f, 0.1f);
            }
        }
        else
        {
            // reset timer when out of proximity
            proximityHapticTimer = 0f;
        }
    }
}