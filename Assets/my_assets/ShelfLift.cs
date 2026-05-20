using UnityEngine;
using Photon.Pun;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class ShelfLift : MonoBehaviourPun
{
    [Header("References")]
    public CollabGameManager gameManager;       // main game manager
    public WardenActions wardenActions;         // to call OnNPCFreed when shelf lifted

    [Header("Grab Points")]
    public XRGrabInteractable grabPointA;       // Player 1 grab point on pCube144
    public XRGrabInteractable grabPointB;       // Player 2 grab point on pCube172

    [Header("Lift Settings")]
    public float liftHeight = 1f;            // how high shelf lifts in meters
    public float liftSpeed = 0.6f;               // speed of lift
    private Vector3 originalPosition;          // shelf position at start
    private Vector3 targetPosition;            // raised position
    private bool isLifted = false;             // true once fully raised and locked

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip liftCompleteClip;         // plays when shelf fully raised

    // grab state - tracked across network
    private bool playerAGrabbing = false;
    private bool playerBGrabbing = false;
    private bool isLifting = false;

    void Start()
    {
        // save original position for reference
        originalPosition = transform.position;
        targetPosition = originalPosition + Vector3.up * liftHeight;

        // make shelf rigidbody kinematic - we move it via code not physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    // ===== called when Player 1 grabs GrabPointA =====
    public void OnGrabPointAGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;
        if (isLifted) return;

        // request ownership so this client can move shelf
        if (!photonView.IsMine)
            photonView.RequestOwnership();

        // broadcast to all clients that Player A is grabbing
        photonView.RPC("RPC_SetGrabState", RpcTarget.All, true, true);

        Debug.Log("GRAB POINT A GRABBED");
    }

    // ===== called when Player 1 releases GrabPointA =====
    public void OnGrabPointAReleased(SelectExitEventArgs args)
    {
        if (isLifted) return;
        photonView.RPC("RPC_SetGrabState", RpcTarget.All, true, false);
        Debug.Log("GRAB POINT A RELEASED");
    }

    // ===== called when Player 2 grabs GrabPointB =====
    public void OnGrabPointBGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;
        if (isLifted) return;

        if (!photonView.IsMine)
            photonView.RequestOwnership();

        // broadcast to all clients that Player B is grabbing
        photonView.RPC("RPC_SetGrabState", RpcTarget.All, false, true);

        Debug.Log("GRAB POINT B GRABBED");
    }

    // ===== called when Player 2 releases GrabPointB =====
    public void OnGrabPointBReleased(SelectExitEventArgs args)
    {
        if (isLifted) return;
        photonView.RPC("RPC_SetGrabState", RpcTarget.All, false, false);
        Debug.Log("GRAB POINT B RELEASED");
    }

    [PunRPC]
    void RPC_SetGrabState(bool isPointA, bool isGrabbing)
    {
        // update grab state on all clients
        if (isPointA)
            playerAGrabbing = isGrabbing;
        else
            playerBGrabbing = isGrabbing;

        // check if both are grabbing - start lift
        if (playerAGrabbing && playerBGrabbing && !isLifting && !isLifted)
        {
            isLifting = true;
            StartCoroutine(LiftShelf());
        }

        // if either releases stop lifting
        if (!playerAGrabbing || !playerBGrabbing)
        {
            isLifting = false;
        }
    }

    IEnumerator LiftShelf()
    {
        // smoothly lift shelf to target height
        while (isLifting && transform.position.y < targetPosition.y)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                liftSpeed * Time.deltaTime
            );
            yield return null;
        }

        // check if fully raised
        if (transform.position.y >= targetPosition.y - 0.05f)
        {
            // lock shelf in raised position
            isLifted = true;
            isLifting = false;
            transform.position = targetPosition;

            // play completion audio
            // if (audioSource != null && liftCompleteClip != null)
            //     audioSource.PlayOneShot(liftCompleteClip);
            // completion audio should be for both players - joint action
            photonView.RPC("RPC_PlayLiftComplete", RpcTarget.All);

            // notify warden actions that NPC is freed
            if (wardenActions != null)
                wardenActions.OnNPCFreed();

            // update status board
            if (gameManager != null)
                gameManager.UpdateTaskStatus("shelf", "DONE");

            Debug.Log("SHELF FULLY LIFTED - NPC FREED");
        }
    }

    [PunRPC]
    void RPC_PlayLiftComplete()
    {
        if (audioSource != null && liftCompleteClip != null)
            audioSource.PlayOneShot(liftCompleteClip);
    }
}