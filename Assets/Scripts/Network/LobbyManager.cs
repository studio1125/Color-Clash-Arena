using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks {

    [Header("References")]
    private MenuController menuController;

    [Header("Custom Rooms")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button quickMatchButton;

    [Header("Room List")]
    [SerializeField] private RoomButton roomButtonPrefab;
    [SerializeField] private Transform roomListContent; // parent for room buttons
    private RoomButton[] roomButtons;

    [Header("Room Information")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomStatusText;
    [SerializeField] private PlayerInformation playerInformationPrefab;
    [SerializeField] private Transform playerListContent; // parent for player name texts
    [SerializeField] private Button roomLeaveButton;
    private Coroutine gameStartCoroutine;

    private void Start() {

        menuController = GetComponent<MenuController>();

        PhotonNetwork.ConnectUsingSettings();

        roomNameInput.onValueChanged.AddListener((value) => createButton.interactable = !string.IsNullOrEmpty(value)); // enable create button only when there's text in the input field
        createButton.onClick.AddListener(CreateRoom);
        quickMatchButton.onClick.AddListener(QuickMatch);
        roomLeaveButton.onClick.AddListener(LeaveRoom);

        createButton.interactable = false; // disable create button by default

        roomButtons = new RoomButton[0]; // initialize room buttons array (to avoid null reference errors when updating the room list for the first time)

    }

    public override void OnConnectedToMaster() {

        Debug.Log("Connected to Master Server");
        PhotonNetwork.JoinLobby();

    }

    public override void OnJoinedLobby() {

        Debug.Log("Joined Lobby");

    }

    // TODO:
    //public override void OnLeftRoom() {

    //    PhotonNetwork.Destroy(tempPlayer);
    //    base.OnLeftRoom();
    // update with RPC to make sure the game doesn't start

    //}

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {

        // destroy existing room buttons
        foreach (RoomButton button in roomButtons)
            Destroy(button.gameObject);

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

    public void QuickMatch() => PhotonNetwork.JoinRandomOrCreateRoom(null, 2, MatchmakingMode.FillRoom, null, null, "Quick" + Random.Range(1000, 9999), new RoomOptions { MaxPlayers = 1 }); // join a random room or create one if none are available, with a unique name to avoid conflicts

    public override void OnJoinedRoom() {

        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        menuController.ShowRoomScreen();

        RefreshRoomMenu();

        // if the room is full, send an RPC to load the game level for all players in the room after a delay (to give the UI time to update and show the "Full! Starting game..." message before loading the level)
        if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            gameStartCoroutine = StartCoroutine(SendDelayedGameStartRPC());

    }

    private IEnumerator SendDelayedGameStartRPC() {

        yield return new WaitForSeconds(3f); // add a short delay before sending the RPC
        photonView.RPC(nameof(RPC_LoadLevel), RpcTarget.All);
        gameStartCoroutine = null; // reset the coroutine reference after sending the RPC

    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshRoomMenu();

    public override void OnPlayerLeftRoom(Player otherPlayer) {

        RefreshRoomMenu();

        if (gameStartCoroutine != null) {

            StopCoroutine(gameStartCoroutine); // stop the game start coroutine if a player leaves the room before the game starts
            gameStartCoroutine = null;

        }
    }

    private void RefreshRoomMenu() {

        if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers) {

            roomStatusText.text = "Full! Starting game...";
            roomLeaveButton.interactable = false; // disable the leave button to prevent players from leaving the room before the game starts

        } else {

            roomStatusText.text = "Waiting for players... (" + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers + ")";

        }

        foreach (Transform child in playerListContent)
            Destroy(child.gameObject);

        int playerNumber = 1;

        foreach (KeyValuePair<int, Player> kvp in PhotonNetwork.CurrentRoom.Players) {

            PlayerInformation playerInfo = Instantiate(playerInformationPrefab, playerListContent);
            playerInfo.Initialize(playerNumber, kvp.Value.NickName);
            playerNumber++;

        }
    }

    [PunRPC]
    public void RPC_LoadLevel() {

        print("START");
        PhotonNetwork.LoadLevel("Arena");

    }

    private void LeaveRoom() {

        PhotonNetwork.LeaveRoom(); // leave the current room
        menuController.ConnectToLobby(ConnectionCompleteAction.ShowLobbyScreen); // connect to the lobby again and show the lobby screen once connected

    }
}
