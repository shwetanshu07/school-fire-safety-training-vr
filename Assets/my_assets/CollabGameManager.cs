using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine.Rendering.PostProcessing;

public class CollabGameManager : MonoBehaviourPun
{
    // ===== ROLE SELECTION =====
    [Header("Role Selection Canvas")]
    public GameObject blackboardCanvas;
    public Button responderSelectButton;
    public Button wardenSelectButton;
    public Outline responderCardOutline;
    public Outline wardenCardOutline;
    public GameObject waitingText;
    public GameObject roleSelectionContainer; // wraps all role selection UI elements

    [Header("Role Colors")]
    public Color availableColor = Color.green;
    public Color takenColor = Color.gray;

    // ===== PLANNING PHASE =====
    [Header("Planning Canvas")]
    public GameObject planningCanvas;
    public TextMeshProUGUI planNumberText;
    public TextMeshProUGUI planBodyText;
    public Button prevButton;
    public Button nextButton;
    public Button selectPlanButton;

    // ===== STATUS BOARD =====
    [Header("Status Board")]
    public GameObject statusBoardContainer;
    public TextMeshProUGUI statusResultText;
    public TextMeshProUGUI statusBodyText;
    public TextMeshProUGUI countdownText;

    // countdown timer - easily changeable
    public float countdownDuration = 300f;  // 5 minutes in seconds
    private float remainingTime;
    public bool gameActive = false;

    // task completion states
    private bool fireOut = false;
    private bool alarmPulled = false;
    private bool ventilationOn = false;
    private bool shelfLifted = false;
    private bool personEvacuated = false;

    // task status strings for board display
    private string fireStatus = "NOT DONE";
    private string alarmStatus = "NOT DONE";
    private string ventilationStatus = "NOT DONE";
    private string shelfStatus = "NOT DONE";
    private string evacuationStatus = "NOT DONE";

    // role state
    public static bool isResponder = false;
    private bool hasSelectedRole = false;

    // track which roles are taken across network
    private bool responderTaken = false;
    private bool wardenTaken = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip welcomeAudio;
    public AudioClip mismatchAudio;
    public AudioClip successAudio;      // plays on game success
    public AudioClip failureAudio;      // plays on game failure
    public AudioClip taskCompleteAudio; // plays when individual task is completed

    // plan data
    private int currentPlanIndex = 0;
    private int mySelectedPlan = -1;    // -1 means not confirmed yet
    private int otherPlayerSelectedPlan = -1;

    private string[] planNumbers = {
        "Plan 1 of 3",
        "Plan 2 of 3",
        "Plan 3 of 3"
    };

    private string[] planBodies = {
        // Plan 1 - Fire First
        "<b>Plan 1 — Fire First</b>\n" +
        "1. Extinguish fire\n" +
        "2. Lift shelf together\n" +
        "3. Pull fire alarm\n" +
        "4. Activate ventilation\n" +
        "5. Evacuate person",

        // Plan 2 - People First (Correct)
        "<b>Plan 2 — People First</b>\n" +
        "1. Lift shelf together\n" +
        "2. Pull fire alarm\n" +
        "3. Activate ventilation\n" +
        "4. Evacuate person\n" +
        "5. Extinguish fire",

        // Plan 3 - Systems First
        "<b>Plan 3 — Systems First</b>\n" +
        "1. Pull fire alarm\n" +
        "2. Activate ventilation\n" +
        "3. Lift shelf together\n" +
        "4. Extinguish fire\n" +
        "5. Evacuate person"
    };

    // ===== FIRE AND SMOKE =====
    [Header("Fire and Smoke")]
    public GameObject fireContainer;            // parent of all fire particles and smoke
    public ParticleSystem fireParticle;         // main fire - FireParticle
    public ParticleSystem fireParticle01;       // FireParticle_01
    public ParticleSystem fireParticle02;       // FireParticle_02
    public ParticleSystem fireParticle03;       // FireParticle_03
    public ParticleSystem smokeParticle;        // smoke particle system
    public GameObject postProcessVolumeObject;
    private PostProcessVolume postProcessVolume; // for vision obscuring via vignette

    // smoke settings - independent of fire
    public float maxSmokeEmission = 500f;        // max particles per second
    public float smokeIncreaseRate = 2f;        // how fast smoke increases per second
    public float maxVignetteIntensity = 0.6f;   // max vision obscuring value
    public float smokeClearRate = 10f;           // how fast smoke clears when ventilation on
    private float currentSmokeEmission = 5f;
    private float currentVignetteIntensity = 0f;
    private bool ventilationActive = false;


    void Start()
    {
        // ensure waiting text hidden at start
        waitingText.SetActive(false);

        // status board hidden at start
        statusBoardContainer.SetActive(false);

        // status result text hidden at start
        statusResultText.gameObject.SetActive(false);

        // fire and smoke hidden at start - activated when welcome audio ends
        fireContainer.SetActive(false);

        // both cards available at start - green borders
        responderCardOutline.effectColor = availableColor;
        wardenCardOutline.effectColor = availableColor;

        // get post process volume component from assigned object
        if (postProcessVolumeObject != null)
            postProcessVolume = postProcessVolumeObject.GetComponent<PostProcessVolume>();
    }

    void Update()
    {
        // only master client drives the timer and fire logic to keep them in sync
        if (gameActive && PhotonNetwork.IsMasterClient)
        {
            remainingTime -= Time.deltaTime;

            // sync timer to all clients
            photonView.RPC("RPC_SyncTimer", RpcTarget.All, remainingTime);

            // check timer failure
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                gameActive = false;
                photonView.RPC("RPC_GameFailed", RpcTarget.All, "Time ran out");
            }

        }

        // smoke and vision logic runs on all clients independently
        if (gameActive)
            UpdateSmokeAndVision();
    }

    void UpdateSmokeAndVision()
    {
        if (ventilationActive)
        {
            // ventilation on - decrease smoke regardless of fire state
            currentSmokeEmission = Mathf.Max(0f, currentSmokeEmission - smokeClearRate * Time.deltaTime);
            currentVignetteIntensity = Mathf.Max(0f, currentVignetteIntensity - smokeClearRate * 0.1f * Time.deltaTime);
        }
        else
        {
            // ventilation off - smoke increases over time
            currentSmokeEmission = Mathf.Min(maxSmokeEmission, currentSmokeEmission + smokeIncreaseRate * Time.deltaTime);
            currentVignetteIntensity = Mathf.Min(maxVignetteIntensity, currentVignetteIntensity + smokeIncreaseRate * 0.02f * Time.deltaTime);
        }

        // apply smoke emission rate
        if (smokeParticle != null)
        {
            var emission = smokeParticle.emission;
            emission.rateOverTime = currentSmokeEmission;
        }

        // apply vignette intensity for vision obscuring
        if (postProcessVolume != null)
        {
            Vignette vignette;
            if (postProcessVolume.profile.TryGetSettings(out vignette))
            {
                vignette.intensity.value = currentVignetteIntensity;
            }
        }

        // check smoke failure - only master client triggers - not failure on smoke increase right now. may change later
        // if (currentSmokeEmission >= maxSmokeEmission && PhotonNetwork.IsMasterClient && !fireOut)
        // {
        //     gameActive = false;
        //     photonView.RPC("RPC_GameFailed", RpcTarget.All, "Smoke filled the room");
        // }
    }

    // ===== ROLE SELECTION =====

    public void OnSelectResponder()
    {
        if (hasSelectedRole) return;
        if (responderTaken) return;

        hasSelectedRole = true;
        isResponder = true;

        // update local UI
        responderCardOutline.effectColor = takenColor;
        responderSelectButton.interactable = false;

        // show waiting text
        waitingText.SetActive(true);

        // tell other player responder is taken
        photonView.RPC("RPC_RoleTaken", RpcTarget.Others, true);

        // update local avatar appearance
        UpdateLocalAvatar(true);

        // check if both roles are now selected
        CheckBothRolesSelected();

        Debug.Log("RESPONDER SELECTED");
    }

    public void OnSelectWarden()
    {
        if (hasSelectedRole) return;
        if (wardenTaken) return;

        hasSelectedRole = true;
        isResponder = false;

        // update local UI
        wardenCardOutline.effectColor = takenColor;
        wardenSelectButton.interactable = false;

        // show waiting text
        waitingText.SetActive(true);

        // tell other player warden is taken
        photonView.RPC("RPC_RoleTaken", RpcTarget.Others, false);

        // update local avatar appearance
        UpdateLocalAvatar(false);

        // check if both roles are now selected
        CheckBothRolesSelected();

        Debug.Log("WARDEN SELECTED");
    }

    [PunRPC]
    void RPC_RoleTaken(bool responderWasTaken)
    {
        if (responderWasTaken)
        {
            responderTaken = true;
            responderCardOutline.effectColor = takenColor;
            responderSelectButton.interactable = false;
        }
        else
        {
            wardenTaken = true;
            wardenCardOutline.effectColor = takenColor;
            wardenSelectButton.interactable = false;
        }

        // check if both roles selected from this client's perspective
        CheckBothRolesSelected();
    }

    void CheckBothRolesSelected()
    {
        bool bothSelected = (responderTaken || (hasSelectedRole && isResponder)) &&
                           (wardenTaken || (hasSelectedRole && !isResponder));

        if (bothSelected && hasSelectedRole)
        {
            // only master client triggers start to avoid double firing
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_BeginGame", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void RPC_BeginGame()
    {
        StartCoroutine(BeginGameAfterDelay());
    }

    IEnumerator BeginGameAfterDelay()
    {
        // hide waiting text
        waitingText.SetActive(false);

        // small delay before hiding canvas
        yield return new WaitForSeconds(0.5f);

        // hide role selection elements but keep canvas active for status board later
        roleSelectionContainer.SetActive(false);

        // 2 second delay then play welcome audio
        yield return new WaitForSeconds(1f);
        if (welcomeAudio != null)
            audioSource.PlayOneShot(welcomeAudio);

        // activate fire and smoke after welcome audio starts
        fireContainer.SetActive(true);

        // activate planning canvas after welcome audio ends
        yield return new WaitForSeconds(welcomeAudio != null ? welcomeAudio.length : 0f);
        ActivatePlanningPhase();
    }

    void UpdateLocalAvatar(bool isResponderRole)
    {
        // hat colour was not changing... needed to add a delay
        StartCoroutine(UpdateAvatarWithDelay(isResponderRole));
    }

    IEnumerator UpdateAvatarWithDelay(bool isResponderRole)
    {
        // wait for avatar to be fully spawned before updating appearance
        yield return new WaitForSeconds(0.5f);

        PlayerAvatar[] avatars = FindObjectsOfType<PlayerAvatar>();
        foreach (PlayerAvatar avatar in avatars)
        {
            if (avatar.photonView.IsMine)
            {
                avatar.SetRole(isResponderRole);
                break;
            }
        }
    }

    // ===== PLANNING PHASE =====

    void ActivatePlanningPhase()
    {
        planningCanvas.SetActive(true);
        currentPlanIndex = 0;
        UpdatePlanDisplay();
    }

    void UpdatePlanDisplay()
    {
        planNumberText.text = planNumbers[currentPlanIndex];
        planBodyText.text = planBodies[currentPlanIndex];
    }

    public void OnPrevPlan()
    {
        // cycle backward through plans
        currentPlanIndex = (currentPlanIndex - 1 + planBodies.Length) % planBodies.Length;
        UpdatePlanDisplay();

        // sync plan change to other player
        photonView.RPC("RPC_SyncPlanIndex", RpcTarget.Others, currentPlanIndex);
    }

    public void OnNextPlan()
    {
        // cycle forward through plans
        currentPlanIndex = (currentPlanIndex + 1) % planBodies.Length;
        UpdatePlanDisplay();

        // sync plan change to other player
        photonView.RPC("RPC_SyncPlanIndex", RpcTarget.Others, currentPlanIndex);
    }

    public void OnConfirmPlan()
    {
        mySelectedPlan = currentPlanIndex;

        // disable confirm button after pressing
        selectPlanButton.interactable = false;

        // tell other player which plan we confirmed
        photonView.RPC("RPC_PlanConfirmed", RpcTarget.Others, mySelectedPlan);

        Debug.Log("PLAN CONFIRMED: " + mySelectedPlan);

        // check if both players confirmed same plan
        CheckBothPlansConfirmed();
    }

    [PunRPC]
    void RPC_SyncPlanIndex(int planIndex)
    {
        // update plan display when other player changes plan
        currentPlanIndex = planIndex;
        UpdatePlanDisplay();
    }

    [PunRPC]
    void RPC_PlanConfirmed(int planIndex)
    {
        otherPlayerSelectedPlan = planIndex;
        CheckBothPlansConfirmed();
    }

    void CheckBothPlansConfirmed()
    {
        // wait until both players have confirmed
        if (mySelectedPlan == -1 || otherPlayerSelectedPlan == -1) return;

        if (mySelectedPlan == otherPlayerSelectedPlan)
        {
            // same plan - only master client fires RPC to avoid double firing
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_BeginGameFromPlan", RpcTarget.All);
            }
        }
        else
        {
            // different plans - only master client fires RPC to avoid double firing
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_PlanMismatch", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void RPC_BeginGameFromPlan()
    {
        planningCanvas.SetActive(false);
        StartCoroutine(BeginGame());
    }

    [PunRPC]
    void RPC_PlanMismatch()
    {
        // reset and allow both players to try again
        mySelectedPlan = -1;
        otherPlayerSelectedPlan = -1;
        selectPlanButton.interactable = true;

        if (mismatchAudio != null)
            audioSource.PlayOneShot(mismatchAudio);

        Debug.Log("PLAN MISMATCH - select same plan");
    }

    IEnumerator BeginGame()
    {
        // small delay then start game
        yield return new WaitForSeconds(1f);

        // show status board on blackboard
        // blackboardCanvas.SetActive(true);
        statusBoardContainer.SetActive(true);

        // initialize board with default status
        RefreshStatusBoard();

        // start countdown timer
        remainingTime = countdownDuration;
        gameActive = true;

        // only master client runs the fire spread sequence
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(FireSpreadSequence());

        Debug.Log("GAME STARTED");
    }

    // ===== FIRE SPREAD SEQUENCE =====

    IEnumerator FireSpreadSequence()
    {
        // wait 30 seconds then activate FireParticle_01
        yield return new WaitForSeconds(600f);
        if (!fireOut)
            photonView.RPC("RPC_ActivateFireSpread", RpcTarget.All, 1);

        // wait 10 more seconds then activate FireParticle_02
        yield return new WaitForSeconds(10f);
        if (!fireOut)
            photonView.RPC("RPC_ActivateFireSpread", RpcTarget.All, 2);

        // wait 10 more seconds then activate FireParticle_03
        yield return new WaitForSeconds(10f);
        if (!fireOut)
            photonView.RPC("RPC_ActivateFireSpread", RpcTarget.All, 3);
    }

    [PunRPC]
    void RPC_ActivateFireSpread(int spreadIndex)
    {
        switch (spreadIndex)
        {
            case 1:
                if (fireParticle01 != null) fireParticle01.gameObject.SetActive(true);
                break;
            case 2:
                if (fireParticle02 != null) fireParticle02.gameObject.SetActive(true);
                break;
            case 3:
                if (fireParticle03 != null) fireParticle03.gameObject.SetActive(true);
                // small delay before triggering failure to ensure fire is visible first
                if (PhotonNetwork.IsMasterClient)
                    StartCoroutine(TriggerFailureAfterDelay("Fire reached trapped person"));
                break;
        }
        Debug.Log("FIRE SPREAD " + spreadIndex + " ACTIVATED");
    }

    IEnumerator TriggerFailureAfterDelay(string reason)
    {
        yield return new WaitForSeconds(0.5f);
        if (gameActive)
        {
            gameActive = false;
            photonView.RPC("RPC_GameFailed", RpcTarget.All, reason);
        }
    }

    // ===== FIRE SUPPRESSION =====

    // called from extinguisher script when spraying correctly at fire
    public void OnFireSuppressed(float amount)
    {
        if (!gameActive || fireOut) return;
        photonView.RPC("RPC_SuppressFire", RpcTarget.All, amount);
    }

    [PunRPC]
    void RPC_SuppressFire(float amount)
    {
        // deactivate fire particles from furthest to closest
        if (fireParticle03 != null && fireParticle03.gameObject.activeSelf)
        {
            fireParticle03.gameObject.SetActive(false);
            return;
        }
        if (fireParticle02 != null && fireParticle02.gameObject.activeSelf)
        {
            fireParticle02.gameObject.SetActive(false);
            return;
        }
        if (fireParticle01 != null && fireParticle01.gameObject.activeSelf)
        {
            fireParticle01.gameObject.SetActive(false);
            return;
        }

        // all spread fires out - now extinguish main fire
        if (fireParticle != null && fireParticle.gameObject.activeSelf)
        {
            fireParticle.gameObject.SetActive(false);

            // all fire out - stop smoke and mark task done
            if (smokeParticle != null) smokeParticle.Stop();

            // play task complete locally - only the responder extinguishes fire
            PlayTaskCompleteLocal();

            UpdateTaskStatus("fire", "DONE");
        }
    }

    // called when ventilation is activated by Player 2
    public void OnVentilationActivated()
    {
        ventilationActive = true;
    }

    // called locally by the completing player only - not synced across network
    public void PlayTaskCompleteLocal()
    {
        if (taskCompleteAudio != null)
            audioSource.PlayOneShot(taskCompleteAudio);
    }

    // ===== STATUS BOARD =====

    // rebuilds the entire status board text from current state strings
    void RefreshStatusBoard()
    {
        statusBodyText.text =
            FormatRow("Fire (Responder)", fireStatus) +
            FormatRow("Fire Alarm (Warden)", alarmStatus) +
            FormatRow("Ventilation (Warden)", ventilationStatus) +
            FormatRow("Trapped Person (Both)", shelfStatus) +
            FormatRow("Evacuation (Warden)", evacuationStatus);
    }

    // formats a single task row with color based on status
    string FormatRow(string taskName, string status)
    {
        string colorTag = status == "DONE" ? "<color=green>" :
                          status == "IN PROGRESS" ? "<color=yellow>" :
                          "<color=white>";
        return taskName + " : " + colorTag + status + "</color>\n";
    }

    // called from action scripts to update a task status on both clients
    public void UpdateTaskStatus(string task, string status)
    {
        photonView.RPC("RPC_UpdateTaskStatus", RpcTarget.All, task, status);
    }

    [PunRPC]
    void RPC_UpdateTaskStatus(string task, string status)
    {
        // update correct status string and completion flag
        switch (task)
        {
            case "fire":
                fireStatus = status;
                if (status == "DONE") fireOut = true;
                break;
            case "alarm":
                alarmStatus = status;
                if (status == "DONE") alarmPulled = true;
                break;
            case "ventilation":
                ventilationStatus = status;
                if (status == "DONE") ventilationOn = true;
                break;
            case "shelf":
                shelfStatus = status;
                if (status == "DONE") shelfLifted = true;
                break;
            case "evacuation":
                evacuationStatus = status;
                if (status == "DONE") personEvacuated = true;
                break;
        }

        // task complete audio is played locally by the completing player before calling UpdateTaskStatus
        // not played here to keep it local to the completing player only

        // rebuild board with updated statuses
        RefreshStatusBoard();

        // check if all tasks are now complete
        CheckAllTasksComplete();
    }

    void CheckAllTasksComplete()
    {
        if (fireOut && alarmPulled && ventilationOn && shelfLifted && personEvacuated)
        {
            // only master client fires success to avoid double firing
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_GameSuccess", RpcTarget.All);
            }
        }
    }

    // ===== TIMER =====

    [PunRPC]
    void RPC_SyncTimer(float time)
    {
        remainingTime = time;
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        countdownText.text = string.Format("Time Remaining: {0:00}:{1:00}", minutes, seconds);
    }

    // ===== GAME END STATES =====

    [PunRPC]
    void RPC_GameSuccess()
    {
        gameActive = false;

        // stop fire alarm and ventilation on game end
        WardenActions wardenActions = FindObjectOfType<WardenActions>();
        if (wardenActions != null) wardenActions.StopAlarm();

        // show success message in green
        statusResultText.text = "SUCCESS";
        statusResultText.color = Color.green;
        statusResultText.gameObject.SetActive(true);

        // play success audio
        if (successAudio != null) audioSource.PlayOneShot(successAudio);

        Debug.Log("GAME SUCCESS");
    }

    [PunRPC]
    void RPC_GameFailed(string reason)
    {
        gameActive = false;

        // stop fire alarm and ventilation on game end
        WardenActions wardenActions = FindObjectOfType<WardenActions>();
        if (wardenActions != null) wardenActions.StopAlarm();

        // show failed message
        statusResultText.text = "FAILED " + reason;
        statusResultText.color = Color.red;
        statusResultText.gameObject.SetActive(true);

        // play failure audio
        if (failureAudio != null) audioSource.PlayOneShot(failureAudio);

        Debug.Log("GAME FAILED: " + reason);
    }

    public void TriggerFailure(string reason)
    {
        if (!gameActive) return;
        gameActive = false;
        photonView.RPC("RPC_GameFailed", RpcTarget.All, reason);
    }
}