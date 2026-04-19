using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class GameManager : MonoBehaviourPun {

    [Header("References")]
    [SerializeField] private PlayerController playerPrefab; // must be inside the Resources/ folder
    private GameCore gameCore;
    protected PlayerController playerController;
    protected PlayerClaimManager claimManager;
    protected UIController uiController;
    private CameraController cameraController;

    [Header("Level")]
    [SerializeField] protected Level level;
    private Vector3 currentRespawnPoint;

    [Header("Checkpoints")]
    [SerializeField] private Transform[] playerSpawns;
    [SerializeField] private Transform checkpointsParent;
    protected Checkpoint[] checkpoints;
    protected int currCheckpointIndex;

    [Header("Claims")]
    protected List<Claimable> levelClaimables;
    protected List<PlayerClaim> playerClaims;
    protected List<PhantomClaim> enemyClaims;
    protected int levelCurrClaimables; // for teleporter

    [Header("Level Completion")]
    protected bool levelCompleted;

    [Header("Subtitles")]
    [SerializeField] private string firstSubtitle;

    [Header("Teleporter")]
    [SerializeField] protected Teleporter teleporter;

    protected void Awake() {

        PhotonNetwork.SendRate = 30; // frequency of outgoing packets
        PhotonNetwork.SerializationRate = 30; // frequency of OnPhotonSerializeView calls

        if (playerSpawns.Length < Constants.MAX_PLAYERS_PER_ROOM)
            Debug.LogError("Not enough player spawn points for max players per room! Please add more spawn points to the level or increase the max players per room in Constants.cs");

        // add all claimables to list
        levelClaimables = new List<Claimable>();

        foreach (Claimable claimable in FindObjectsByType<Claimable>(FindObjectsSortMode.None))
            levelClaimables.Add(claimable);

        gameCore = FindFirstObjectByType<GameCore>();
        cameraController = FindFirstObjectByType<CameraController>();

        // checkpoints
        if (checkpointsParent != null) { // having checkpoints is optional, so make sure level has them

            checkpoints = checkpointsParent.GetComponentsInChildren<Checkpoint>();

            for (int i = 1; i < checkpoints.Length; i++) // disable all checkpoints except first
                checkpoints[i].gameObject.SetActive(false);

        }

        uiController = FindFirstObjectByType<UIController>(); // set early for player controller initialization

        // destroy any existing player controllers in scene
        foreach (PlayerController obj in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            Destroy(obj.gameObject); // don't use PhotonNetwork.Destroy because these player controllers aren't networked objects until they are instantiated by PhotonNetwork.Instantiate

        currentRespawnPoint = GetUniqueRandomSpawn(); // get a unique random spawn point for this player

        playerController = PhotonNetwork.Instantiate(playerPrefab.name, currentRespawnPoint + new Vector3(0f, playerPrefab.transform.localScale.y / 2f, 0f), Quaternion.identity).GetComponent<PlayerController>(); // instantiate player prefab for multiplayer
        playerController.Initialize(uiController, level.GetSpeedModifier(), level.GetJumpModifier(), level.IsUnderwater()); // initialize player controller

        uiController.Initialize(); // initialize UI controller after player controller so that the correct player controller is found by the UI controller

        cameraController.SetTarget(playerController.transform);

        // claims
        claimManager = playerController.GetComponent<PlayerClaimManager>(); // get claim manager from local player instead of FindFirstObjectByType to avoid getting remote player's component

        playerClaims = new List<PlayerClaim>();
        enemyClaims = new List<PhantomClaim>();

    }

    protected void Start() {

        // destroy existing enemies in scene (done for all clients, not just MasterClient)
        foreach (PhantomController obj in FindObjectsByType<PhantomController>(FindObjectsSortMode.None))
            Destroy(obj.gameObject); // don't use PhotonNetwork.Destroy because these enemies aren't networked objects until they are spawned by PhantomSpawn.SpawnEnemy

        gameCore.ResetGravity(); // reset gravity to modify original gravity, not current one
        gameCore.ModifyGravity(level.GetGravityModifier());

        uiController.SetSubtitleText(firstSubtitle); // update subtitle text

    }

    public abstract void Initialize();

    protected void SpawnEnemies() {

        if (!PhotonNetwork.IsMasterClient) return; // only MasterClient should spawn enemies; they are networked objects so spawning on all clients would create duplicates

        foreach (PhantomSpawn enemySpawn in FindObjectsByType<PhantomSpawn>(FindObjectsSortMode.None))
            enemySpawn.SpawnEnemy(); // spawn enemy (PhantomSpawn.SpawnEnemy uses PhotonNetwork.Instantiate)

    }

    public void UpdateCheckpoints() {

        // enable next checkpoint
        for (int i = 0; i < checkpoints.Length; i++)
            if (i == currCheckpointIndex + 1)
                checkpoints[i].gameObject.SetActive(true); // enable checkpoint

        currCheckpointIndex++; // increment checkpoint index

        // update teleporter because some track checkpoints
        if (level.HasTeleporter())
            teleporter.UpdateTeleporter();

    }

    private Vector3 GetUniqueRandomSpawn() {

        // use the room name as a seed so every client shuffles the list in the EXACT same order
        int seed = PhotonNetwork.CurrentRoom.Name.GetHashCode();
        System.Random rng = new System.Random(seed);

        // create a list of indices and shuffle them
        List<int> spawnIndices = Enumerable.Range(0, playerSpawns.Length).ToList();

        // Fisher-Yates Shuffle algorithm
        for (int i = spawnIndices.Count - 1; i > 0; i--) {

            int k = rng.Next(i + 1);
            //int value = spawnIndices[k];
            //spawnIndices[k] = spawnIndices[i];
            //spawnIndices[i] = value;

            (spawnIndices[i], spawnIndices[k]) = (spawnIndices[k], spawnIndices[i]); // using a tuple swap to avoid needing a temporary variable

        }

        // use ActorNumber (1, 2, 3...) to pick an index from the shuffled list
        // use modulo so if there are more players than spawns, they wrap around
        int myIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % playerSpawns.Length;
        int shuffledSpawnIndex = spawnIndices[myIndex];

        return playerSpawns[shuffledSpawnIndex].position;

    }

    public abstract void AddClaim(EntityClaim claim);

    public abstract void RemoveClaim(EntityClaim claim);

    public abstract bool IsLevelObjectiveCompleted(); // doesn't mean level is completed, just that the objective has been reached

    public bool IsLevelCompleted() => levelCompleted;

    public void SetLevelCompleted(bool levelCompleted) => this.levelCompleted = levelCompleted;

    public int GetLevelIndex() => level.GetLevelNumber();

    public Vector3 GetPlayerSpawn() => currentRespawnPoint;

    public void SetPlayerSpawn(Vector3 playerSpawn) => currentRespawnPoint = playerSpawn;

    public AudioClip GetBackgroundMusic() => level.GetBackgroundMusic();

    public int GetLevelTotalCheckpoints() => checkpoints.Length;

    public int GetLevelCurrentCheckpoints() => currCheckpointIndex;

    public List<PlayerClaim> GetPlayerClaims() => playerClaims;

    public List<PhantomClaim> GetEnemyClaims() => enemyClaims;

    public int GetLevelTotalClaimables() => levelClaimables.Count;

    public int GetLevelCurrentClaimables() => levelCurrClaimables;

    public bool LevelHasCode() => level.HasCode();

}
