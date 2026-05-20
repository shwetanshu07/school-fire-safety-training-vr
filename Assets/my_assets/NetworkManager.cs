using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Room Settings")]
    public string roomName = "FireSafetyRoom";
    public byte maxPlayers = 2;

    [Header("Spawn Points")]
    public Transform spawnPoint1;   // spawn position for first player
    public Transform spawnPoint2;   // spawn position for second player

    [Header("Player Prefab")]
    public GameObject playerPrefab; // the avatar prefab to spawn

    void Start()
    {
        // connect to photon master server on scene load
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("Connecting to Photon...");
        }
    }

    // called automatically when connected to photon master server
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");
        JoinOrCreateRoom();
    }

    void JoinOrCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    // called when successfully joined a room
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room. Player count: " + PhotonNetwork.CurrentRoom.PlayerCount);
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        // choose spawn point based on player order in room
        Transform spawnPoint = PhotonNetwork.CurrentRoom.PlayerCount == 1 
            ? spawnPoint1 
            : spawnPoint2;

        // instantiate player avatar over the network
        PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );
    }

    // called if joining room fails
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Join Room Failed: " + message);
        // try again
        JoinOrCreateRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected: " + cause);
    }
}