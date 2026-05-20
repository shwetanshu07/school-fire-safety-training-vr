using UnityEngine;
using Photon.Pun;

public class PlayerAvatar : MonoBehaviourPun, IPunObservable
{
    [Header("Avatar Parts")]
    public GameObject bodyCapsule;
    public GameObject headSphere;
    public GameObject hat;

    [Header("Role Materials")]
    public Material responderMaterial;  // red
    public Material wardenMaterial;     // blue matrial

    [Header("VR Tracking")]
    public Transform vrCamera;          // assigned at runtime

    // smoothing for remote player movement
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    void Start()
    {
        if (photonView.IsMine)
        {
            // hide our own body from ourselves - we dont need to see our own capsule
            bodyCapsule.SetActive(false);
            headSphere.SetActive(false);
            hat.SetActive(false);

            // find main camera automatically at runtime
            vrCamera = Camera.main.transform;
        }
        else
        {
            // initialize network position to current position
            networkPosition = transform.position;
            networkRotation = transform.rotation;
        }
    }

    void Update()
    {
        if (photonView.IsMine && vrCamera != null)
        {
            // move avatar root to match VR camera position
            transform.position = vrCamera.position;
            transform.rotation = vrCamera.rotation;
        }
        else
        {
            // smoothly interpolate remote player position
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
        }
    }

    // called after role selection to update avatar appearance
    public void SetRole(bool isResponder)
    {
        // make hat visible first
        hat.SetActive(true);
        
        ApplyRoleMaterial(isResponder);

        // broadcast role appearance to all other clients
        photonView.RPC("RPC_SetRoleAppearance", RpcTarget.Others, isResponder);
    }

    void ApplyRoleMaterial(bool isResponder)
    {
        Material roleMaterial = isResponder ? responderMaterial : wardenMaterial;
        // hat.GetComponent<Renderer>().material = roleMaterial;
        // get all renderers including children since hat is FBX with child meshes
        Renderer[] renderers = hat.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = roleMaterial;
        }
    }

    [PunRPC]
    void RPC_SetRoleAppearance(bool isResponder)
    {
        hat.SetActive(true);
        ApplyRoleMaterial(isResponder);
    }

    // IPunObservable - manually sync position and rotation for smooth movement
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // we are the owner - send our position and rotation
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // we are receiving - read position and rotation
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}