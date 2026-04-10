using UnityEngine;

public class PhantomPatrolRoute : MonoBehaviour {

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;

    public Transform[] GetPatrolPoints() => patrolPoints;

}
