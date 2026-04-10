using Photon.Pun;
using UnityEngine;

public abstract class GunManager : MonoBehaviourPun {

    [Header("References")]
    [SerializeField] protected Transform gunSlot;
    [SerializeField] protected LayerMask shootableMask;

    public abstract void RequestReload();

}
