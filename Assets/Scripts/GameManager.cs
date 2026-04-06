using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class GameManager : MonoBehaviour {

    [Header("References")]
    [SerializeField] private PlayerController playerPrefab;
    private GameCore gameCore;
    protected PlayerController playerController;
    protected PlayerClaimManager claimManager;
    protected UIController uiController;
    private CameraController cameraController;

    [Header("Level")]
    [SerializeField] protected Level level;

    [Header("Checkpoints")]
    [SerializeField] private Transform playerSpawn;
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

        uiController = FindFirstObjectByType<UIController>();

        // destroy any existing player controllers in scene
        foreach (PlayerController obj in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            Destroy(obj.gameObject);

        playerController = PhotonNetwork.Instantiate(playerPrefab.name, playerSpawn.position + new Vector3(0f, playerPrefab.transform.localScale.y / 2f, 0f), Quaternion.identity).GetComponent<PlayerController>(); // instantiate player prefab for multiplayer
        playerController.Initialize(uiController, level.GetSpeedModifier(), level.GetJumpModifier(), level.IsUnderwater()); // initialize player controller

        cameraController.SetTarget(playerController.transform);

        // claims
        claimManager = FindFirstObjectByType<PlayerClaimManager>();
        claimManager.transform.position = playerSpawn.position;

        playerClaims = new List<PlayerClaim>();
        enemyClaims = new List<PhantomClaim>();

    }

    protected void Start() {

        SpawnEnemies(); // to allow enemy spawn class to run awake method first

        gameCore.ResetGravity(); // reset gravity to modify original gravity, not current one
        gameCore.ModifyGravity(level.GetGravityModifier());

        uiController.SetSubtitleText(firstSubtitle); // update subtitle text

    }

    public abstract void Initialize();

    protected void SpawnEnemies() {

        // destroy existing enemies in scene
        foreach (PhantomController obj in FindObjectsByType<PhantomController>(FindObjectsSortMode.None))
            Destroy(obj.gameObject);

        foreach (PhantomSpawn enemySpawn in FindObjectsByType<PhantomSpawn>(FindObjectsSortMode.None))
            enemySpawn.SpawnEnemy(); // spawn enemy

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

    public abstract void AddClaim(EntityClaim claim);

    public abstract void RemoveClaim(EntityClaim claim);

    public abstract bool IsLevelObjectiveCompleted(); // doesn't mean level is completed, just that the objective has been reached

    public bool IsLevelCompleted() => levelCompleted;

    public void SetLevelCompleted(bool levelCompleted) { this.levelCompleted = levelCompleted; }

    public int GetLevelIndex() => level.GetLevelNumber();

    public Vector3 GetPlayerSpawn() => playerSpawn.position;

    public void SetPlayerSpawn(Vector3 playerSpawn) { this.playerSpawn.position = playerSpawn; }

    public AudioClip GetBackgroundMusic() => level.GetBackgroundMusic();

    public int GetLevelTotalCheckpoints() => checkpoints.Length;

    public int GetLevelCurrentCheckpoints() => currCheckpointIndex;

    public List<PlayerClaim> GetPlayerClaims() => playerClaims;

    public List<PhantomClaim> GetEnemyClaims() => enemyClaims;

    public int GetLevelTotalClaimables() => levelClaimables.Count;

    public int GetLevelCurrentClaimables() => levelCurrClaimables;

    public bool LevelHasCode() => level.HasCode();

}
