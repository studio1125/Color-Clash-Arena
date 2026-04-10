using UnityEngine;

[CreateAssetMenu]
public class GunData : ScriptableObject {

    [Header("Data")]
    [SerializeField] private new string name;
    [SerializeField] private Sprite icon;
    [SerializeField] private float damage;
    [SerializeField] private bool isAutomatic;
    [SerializeField] private int magazineSize;
    [SerializeField] private float fireRate;
    [SerializeField] private float maxRange;
    [SerializeField] private float reloadTime;
    [SerializeField] private bool useRaycastShooting;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;

    public string GetName() => name;

    public Sprite GetIcon() => icon;

    public float GetDamage() => damage;

    public bool IsAutomatic() => isAutomatic;

    public int GetMagazineSize() => magazineSize;

    public float GetFireRate() => fireRate;

    public float GetMaxRange() => maxRange;

    public float GetReloadTime() => reloadTime;

    public bool UsesRaycastShooting() => useRaycastShooting;

    public AudioClip GetShootSound() => shootSound;

    public AudioClip GetReloadSound() => reloadSound;

}
