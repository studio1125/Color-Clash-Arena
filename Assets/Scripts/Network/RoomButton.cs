using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomButton : MonoBehaviour {

    [Header("References")]
    [SerializeField] private TMP_Text text;
    private LobbyManager lobbyManager;
    private Button button;
    private string roomName;

    public void Initialize(string roomName, int playerCount, int maxPlayers) {

        this.roomName = roomName;

        lobbyManager = FindFirstObjectByType<LobbyManager>();
        button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);

        text.text = $"{roomName} ({playerCount}/{maxPlayers})";

    }

    private void OnClick() => lobbyManager.JoinRoomInList(roomName); // pass the room name to the lobby manager so it can join the correct room

}
