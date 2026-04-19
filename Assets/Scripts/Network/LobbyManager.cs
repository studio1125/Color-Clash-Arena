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

    [Header("Settings")]
    [SerializeField] private float gameStartDelay; // delay before loading the game level after the room is full

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

    public override void OnJoinedLobby() => Debug.Log("Joined Lobby");

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
            PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = Constants.MAX_PLAYERS_PER_ROOM });

    }

    public void JoinRoomInList(string roomName) => PhotonNetwork.JoinRoom(roomName);

    // TODO: change MaxPlayers to 2
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

        yield return new WaitForSeconds(gameStartDelay); // add a short delay before sending the RPC
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
    public void RPC_LoadLevel() => PhotonNetwork.LoadLevel("Arena");

    private void LeaveRoom() {

        PhotonNetwork.LeaveRoom(); // leave the current room
        menuController.ConnectToLobby(ConnectionCompleteAction.ShowLobbyScreen); // connect to the lobby again and show the lobby screen once connected

    }

    public override void OnCreateRoomFailed(short returnCode, string message) {

        switch (returnCode) {

            case ErrorCode.GameIdAlreadyExists:
                menuController.DisplayError("Room creation failed: A room with that name already exists. Please choose a different name.");
                break;

            case ErrorCode.InvalidOperation:
                menuController.DisplayError("Room creation failed: You are already in a room. Please leave your current room before creating a new one.");
                break;

            case ErrorCode.InvalidAuthentication:
                menuController.DisplayError("Room creation failed: Authentication failed. Please check your connection and try again.");
                break;

            case ErrorCode.ServerFull:
                menuController.DisplayError("Room creation failed: The server is currently full. Please try again later.");
                break;

            case ErrorCode.InternalServerError:
                menuController.DisplayError("Room creation failed: An internal server error occurred. Please try again later.");
                break;

            case ErrorCode.InvalidRegion:
                menuController.DisplayError("Room creation failed: The selected region is currently unavailable. Please try again later.");
                break;

            case ErrorCode.CustomAuthenticationFailed:
                menuController.DisplayError("Room creation failed: Custom authentication failed. Please check your connection and try again.");
                break;

            case ErrorCode.AuthenticationTicketExpired:
                menuController.DisplayError("Room creation failed: Authentication ticket expired. Please check your connection and try again.");
                break;

            case ErrorCode.PluginReportedError:
                menuController.DisplayError("Room creation failed: A plugin reported an error. Please try again later.");
                break;

            case ErrorCode.PluginMismatch:
                menuController.DisplayError("Room creation failed: A plugin mismatch occurred. Please try again later.");
                break;

            case ErrorCode.JoinFailedPeerAlreadyJoined:
                menuController.DisplayError("Room creation failed: You have already joined a room. Please leave your current room before creating a new one.");
                break;

            case ErrorCode.JoinFailedFoundInactiveJoiner:
                menuController.DisplayError("Room creation failed: An inactive joiner was found. Please try again later.");
                break;

            case ErrorCode.JoinFailedWithRejoinerNotFound:
                menuController.DisplayError("Room creation failed: A rejoiner was not found. Please try again later.");
                break;

            case ErrorCode.JoinFailedFoundExcludedUserId:
                menuController.DisplayError("Room creation failed: An excluded user ID was found. Please try again later.");
                break;

            case ErrorCode.JoinFailedFoundActiveJoiner:
                menuController.DisplayError("Room creation failed: An active joiner was found. Please try again later.");
                break;

            case ErrorCode.HttpLimitReached:
                menuController.DisplayError("Room creation failed: HTTP limit reached. Please try again later.");
                break;

            case ErrorCode.ExternalHttpCallFailed:
                menuController.DisplayError("Room creation failed: An external HTTP call failed. Please try again later.");
                break;

            case ErrorCode.OperationLimitReached:
                menuController.DisplayError("Room creation failed: Operation limit reached. Please try again later.");
                break;

            case ErrorCode.SlotError:
                menuController.DisplayError("Room creation failed: A slot error occurred. Please try again later.");
                break;

            case ErrorCode.InvalidEncryptionParameters:
                menuController.DisplayError("Room creation failed: Invalid encryption parameters. Please try again later.");
                break;

            case ErrorCode.GameFull:
                menuController.DisplayError("Room creation failed: The room is already full. Please try joining a different room or creating a new one.");
                break;

            case ErrorCode.GameClosed:
                menuController.DisplayError("Room creation failed: The room is closed. Please try joining a different room or creating a new one.");
                break;

            case ErrorCode.NoRandomMatchFound:
                menuController.DisplayError("Room creation failed: No random match found. Please try joining a different room or creating a new one.");
                break;

            case ErrorCode.GameDoesNotExist:
                menuController.DisplayError("Room creation failed: The specified room does not exist. Please try joining a different room or creating a new one.");
                break;

            case ErrorCode.MaxCcuReached:
                menuController.DisplayError("Room creation failed: The maximum number of concurrent users has been reached. Please try again later.");
                break;

            case ErrorCode.OperationNotAllowedInCurrentState:
                menuController.DisplayError("Room creation failed: This operation is not allowed in the current state. Please try again later.");
                break;

            default:
                menuController.DisplayError("Room creation failed: An unknown error occurred (Error Code: " + returnCode + "). Please try again later.");
                break;

        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message) {

        switch (returnCode) {

            case ErrorCode.GameFull:
                menuController.DisplayError("Room join failed: The room is already full. Please try joining a different room.");
                break;

            case ErrorCode.GameClosed:
                menuController.DisplayError("Room join failed: The room is closed. Please try joining a different room.");
                break;

            case ErrorCode.NoRandomMatchFound:
                menuController.DisplayError("Room join failed: No random match found. Please try joining a different room.");
                break;

            case ErrorCode.GameDoesNotExist:
                menuController.DisplayError("Room join failed: The specified room does not exist. Please try joining a different room.");
                break;

            case ErrorCode.MaxCcuReached:
                menuController.DisplayError("Room join failed: The maximum number of concurrent users has been reached. Please try again later.");
                break;

            case ErrorCode.OperationNotAllowedInCurrentState:
                menuController.DisplayError("Room join failed: This operation is not allowed in the current state. Please try again later.");
                break;

            default:
                menuController.DisplayError("Room join failed: An unknown error occurred (Error Code: " + returnCode + "). Please try again later.");
                break;

        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {

        switch (returnCode) {

            case ErrorCode.NoRandomMatchFound:
                menuController.DisplayError("Quick match failed: No available rooms found. Please try creating a new room or joining a different room.");
                break;

            case ErrorCode.MaxCcuReached:
                menuController.DisplayError("Quick match failed: The maximum number of concurrent users has been reached. Please try again later.");
                break;

            case ErrorCode.OperationNotAllowedInCurrentState:
                menuController.DisplayError("Quick match failed: This operation is not allowed in the current state. Please try again later.");
                break;

            default:
                menuController.DisplayError("Quick match failed: An unknown error occurred (Error Code: " + returnCode + "). Please try again later.");
                break;

        }
    }
}
