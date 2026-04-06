using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks {

    [Header("Custom Rooms")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button quickMatchButton;

    [Header("Room List")]
    [SerializeField] private RoomButton roomButtonPrefab;
    [SerializeField] private Transform roomListContent; // parent for room buttons
    private RoomButton[] roomButtons;

    private void Start() {

        PhotonNetwork.ConnectUsingSettings();

        roomNameInput.onValueChanged.AddListener((value) => createButton.interactable = !string.IsNullOrEmpty(value)); // enable create button only when there's text in the input field
        createButton.onClick.AddListener(CreateRoom);
        quickMatchButton.onClick.AddListener(QuickMatch);

        createButton.interactable = false; // disable create button by default

    }

    public override void OnConnectedToMaster() {

        Debug.Log("Connected to Master Server");
        PhotonNetwork.JoinLobby();

    }

    public override void OnJoinedLobby() {

        Debug.Log("Joined Lobby");

    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {

        for (int i = 0; i < roomList.Count; i++)
            if (roomList[i].RemovedFromList)
                Destroy(roomButtons[i].gameObject);

        roomButtons = new RoomButton[roomList.Count];

        for (int i = 0; i < roomList.Count; i++) {

            if (roomList[i].IsOpen && roomList[i].IsVisible && roomList[i].PlayerCount >= 0) {

                RoomButton roomButton = Instantiate(roomButtonPrefab, roomListContent);
                roomButton.Initialize(roomList[i].Name, roomList[i].PlayerCount, roomList[i].MaxPlayers);
                roomButtons[i] = roomButton;

            }
        }
    }

    public void CreateRoom() {

        string roomName = roomNameInput.text;

        if (!string.IsNullOrEmpty(roomName))
            PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 2 });

    }

    public void JoinRoomInList(string roomName) => PhotonNetwork.JoinRoom(roomName);

    public void QuickMatch() => PhotonNetwork.JoinRandomOrCreateRoom(null, 2, MatchmakingMode.FillRoom, null, null, "Quick" + Random.Range(1000, 9999)); // join a random room or create one if none are available, with a unique name to avoid conflicts

    public override void OnJoinedRoom() {

        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("Arena");

    }
}
