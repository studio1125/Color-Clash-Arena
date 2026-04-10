using TMPro;
using UnityEngine;

public class PlayerInformation : MonoBehaviour {

    [Header("References")]
    [SerializeField] private TMP_Text playerNumberText;
    [SerializeField] private TMP_Text playerNameText;

    public void Initialize(int playerNumber, string playerName) {

        playerNumberText.text = $"Player {playerNumber}";
        playerNameText.text = playerName;

    }
}
