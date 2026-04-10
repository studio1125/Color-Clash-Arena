using Photon.Pun;
using System.Collections;
using UnityEngine;

public class PhantomSpawn : MonoBehaviour {

    [Header("Spawn")]
    [SerializeField] private PhantomController phantomPrefab; // must be in Resources/ folder
    [SerializeField] private Gun gun; // must be in Resources/Guns/ folder
    private PhantomController currPhantom;
    private bool isFlipped;

    [Header("Patrol")]
    private PhantomPatrolRoute patrolRoute;

    [Header("Respawn")]
    [SerializeField] private bool respawnEnabled;
    [SerializeField] private float respawnWaitDuration;

    [Header("Claimable")]
    [SerializeField][Tooltip("Can be left null if no claimable platform is available nearby")] private Claimable claimablePlatform;

    private void Awake() {

        isFlipped = transform.right.x < 0f;
        patrolRoute = GetComponentInChildren<PhantomPatrolRoute>();

    }

    public void SpawnEnemy() {

        currPhantom = PhotonNetwork.Instantiate(phantomPrefab.name, transform.position + new Vector3(0f, phantomPrefab.transform.localScale.y / 2f, 0f), isFlipped ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity).GetComponent<PhantomController>();
        currPhantom.Initialize(this, gun, isFlipped, patrolRoute.GetPatrolPoints());

    }

    public void OnEnemyDeath() {

        if (respawnEnabled)
            StartCoroutine(RespawnEnemy());

    }

    private IEnumerator RespawnEnemy() {

        yield return new WaitForSeconds(respawnWaitDuration);

        if (claimablePlatform) // some spawns might not have a claimable platform
            while (claimablePlatform.GetClaimer() == EntityType.Player) // don't respawn enemy if claimed by player
                yield return null;

        if (respawnEnabled && PhotonNetwork.IsMasterClient) // check again in case respawn was disabled while waiting; only MasterClient should spawn to avoid duplicates
            SpawnEnemy();

    }

    public bool IsPhantomAlive() => currPhantom != null;

    public bool IsFlipped() => isFlipped;

    public bool IsRespawnEnabled() => respawnEnabled;

    public void SetRespawnEnabled(bool respawnEnabled) => this.respawnEnabled = respawnEnabled;

    public float GetRespawnWaitDuration() => respawnWaitDuration;

}
