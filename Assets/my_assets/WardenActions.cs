using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using System.Collections;

public class WardenActions : MonoBehaviourPun
{
    [Header("References")]
    public CollabGameManager gameManager;       // main game manager

    [Header("Role Lock")]
    public bool roleLockEnabled = true;         // toggle to disable role locking for testing

    // ===== FIRE ALARM =====
    [Header("Fire Alarm")]
    public AudioSource fireAlarmAudioSource;    // 3D spatial audio on alarm object - heard by both players

    // ===== VENTILATION =====
    [Header("Ventilation")]
    public Transform ventilationSwitch;
    public AudioSource ventilationAudioSource;  // 3D spatial audio on fan object - heard by both players
    public float switchOnRotationX = -105.629f;
    public float switchOffRotationX = -78.558f;
    private bool ventilationDone = false;

    // ===== LOCAL ONE SHOT AUDIO =====
    [Header("Local Audio")]
    public AudioSource localAudioSource;        // 2D local audio - heard only by this client
    public AudioClip ventilationHintClip;       // hint for Player 1 only
    public AudioClip correctExitClip;           // correct exit confirmation - local only
    public AudioClip wrongExitClip;             // wrong exit feedback - local only

    // ===== HINT =====
    [Header("Hint")]
    public float hintDelay = 20f;               // seconds before hint plays for Player 1
    private bool hintPlayed = false;
    private float hintTimer = 0f;

    // ===== NPC =====
    [Header("NPC")]
    public GameObject npcObject;
    public GameObject npcLabelCanvas;
    public TextMeshProUGUI npcLabelText;
    public Vector3 npcStandingPosition = new Vector3(7.868f, -0.009f, -15.50f);
    public Vector3 npcStandingRotation = new Vector3(-90f, -90f, 0f);
    private bool npcFreed = false;
    private bool npcEvacuated = false;

    // ===== ALARM STATE =====
    private bool alarmDone = false;

    void Start()
    {
        // start NPC pulsing label coroutine
        StartCoroutine(PulseNPCLabel());
    }

    void Update()
    {
        // hint timer - only runs on responder client, if ventilation not done
        if (!CollabGameManager.isResponder) return;  // hint plays on Player 1 (responder) only
        if (ventilationDone || hintPlayed) return;
        if (!gameManager.gameActive) return;

        hintTimer += Time.deltaTime;
        if (hintTimer >= hintDelay)
        {
            hintPlayed = true;
            PlayHintAudio();
        }
    }

    // ===== FIRE ALARM =====

    public void OnFireAlarmPulled()
    {
        // role lock - only Evacuation Warden can pull alarm
        if (roleLockEnabled && CollabGameManager.isResponder)
        {
            Debug.Log("ROLE LOCK - only Evacuation Warden can pull alarm");
            return;
        }

        if (alarmDone) return;
        alarmDone = true;

        // broadcast alarm to all clients - both hear it
        photonView.RPC("RPC_ActivateAlarm", RpcTarget.All);

        // play task complete locally - only the completing player hears it
        gameManager.PlayTaskCompleteLocal();

        // update status board
        gameManager.UpdateTaskStatus("alarm", "DONE");

        Debug.Log("FIRE ALARM PULLED");
    }

    [PunRPC]
    void RPC_ActivateAlarm()
    {
        // 3D spatial audio - plays on both clients from alarm object position
        if (fireAlarmAudioSource != null)
        {
            fireAlarmAudioSource.loop = true;
            fireAlarmAudioSource.Play();
        }
    }

    // ===== VENTILATION =====

    public void OnVentilationSwitchPressed()
    {
        // both players can activate ventilation

        if (ventilationDone) return;
        ventilationDone = true;

        // broadcast ventilation to all clients - both see switch move and hear fan
        photonView.RPC("RPC_ActivateVentilation", RpcTarget.All);

        // tell game manager to start clearing smoke
        gameManager.OnVentilationActivated();

        // play task complete locally - only the completing player hears it
        gameManager.PlayTaskCompleteLocal();

        // update status board
        gameManager.UpdateTaskStatus("ventilation", "DONE");

        Debug.Log("VENTILATION ACTIVATED");
    }

    [PunRPC]
    void RPC_ActivateVentilation()
    {
        // rotate switch visually on both clients
        if (ventilationSwitch != null)
        {
            Vector3 currentRotation = ventilationSwitch.localEulerAngles;
            ventilationSwitch.localEulerAngles = new Vector3(
                switchOnRotationX,
                currentRotation.y,
                currentRotation.z
            );
        }

        // 3D spatial audio - plays on both clients from fan object position
        if (ventilationAudioSource != null)
        {
            ventilationAudioSource.loop = true;
            ventilationAudioSource.Play();
        }
    }

    // ===== HINT AUDIO =====

    void PlayHintAudio()
    {
        // local 2D audio - plays only on Player 1's client
        // tells Player 1 that Player 2 has not activated ventilation yet
        if (localAudioSource != null && ventilationHintClip != null)
            localAudioSource.PlayOneShot(ventilationHintClip);
    }

    // ===== NPC LABEL PULSE =====

    IEnumerator PulseNPCLabel()
    {
        // pulse HELP!!! label until NPC is freed
        while (!npcFreed)
        {
            if (npcLabelCanvas != null) npcLabelCanvas.SetActive(true);
            yield return new WaitForSeconds(0.8f);
            if (npcLabelCanvas != null) npcLabelCanvas.SetActive(false);
            yield return new WaitForSeconds(0.8f);
        }
    }

    // NPC FREED 

    // public void OnNPCFreed()
    // {
    //     if (npcFreed) return;
    //     npcFreed = true;

    //     // stop pulsing label
    //     if (npcLabelCanvas != null) npcLabelCanvas.SetActive(false);

    //     // teleport NPC to standing position
    //     if (npcObject != null)
    //     {
    //         npcObject.transform.position = npcStandingPosition;
    //         npcObject.transform.eulerAngles = npcStandingRotation;
    //     }

    //     // show guide label
    //     if (npcLabelText != null)
    //         npcLabelText.text = "Guide me to the correct exit";
    //     if (npcLabelCanvas != null)
    //         npcLabelCanvas.SetActive(true);

    //     Debug.Log("NPC FREED AND STANDING");
    // }

    public void OnNPCFreed()
    {
        if (npcFreed) return;
        photonView.RPC("RPC_OnNPCFreed", RpcTarget.All);
    }

    [PunRPC]
    void RPC_OnNPCFreed()
    {
        npcFreed = true;

        if (npcLabelCanvas != null) npcLabelCanvas.SetActive(false);

        if (npcObject != null)
        {
            npcObject.transform.position = npcStandingPosition;
            npcObject.transform.eulerAngles = npcStandingRotation;
        }

        // if (npcLabelText != null)
        //     npcLabelText.text = "Guide to correct exit";
        // if (npcLabelCanvas != null)
        //     npcLabelCanvas.SetActive(true);

        Debug.Log("NPC FREED");
    }

    // ===== EXIT DOOR INTERACTIONS =====

    public void OnStairsDoorClicked()
    {
        // role lock - only Evacuation Warden can evacuate
        if (roleLockEnabled && CollabGameManager.isResponder)
        {
            Debug.Log("ROLE LOCK - only Evacuation Warden can evacuate");
            return;
        }

        if (!npcFreed || npcEvacuated) return;
        npcEvacuated = true;

        // hide guide label
        if (npcLabelCanvas != null) npcLabelCanvas.SetActive(false);

        // local 2D audio - correct exit confirmation heard only by Player 2
        if (localAudioSource != null && correctExitClip != null)
            localAudioSource.PlayOneShot(correctExitClip);

        // play task complete locally - only the completing player hears it
        gameManager.PlayTaskCompleteLocal();

        // update status board
        gameManager.UpdateTaskStatus("evacuation", "DONE");

        Debug.Log("CORRECT EXIT - EVACUATION COMPLETE");
    }

    public void OnElevatorDoorClicked()
    {
        // role lock
        if (roleLockEnabled && CollabGameManager.isResponder) return;

        if (!npcFreed || npcEvacuated) return;

        gameManager.TriggerFailure("Wrong exit chosen");

        Debug.Log("WRONG EXIT - use stairs not elevator");
    }

    // ===== STOP ALARM ON GAME END =====

    public void StopAlarm()
    {
        // stop both looping spatial audios
        if (fireAlarmAudioSource != null) fireAlarmAudioSource.Stop();
        if (ventilationAudioSource != null) ventilationAudioSource.Stop();
    }
}