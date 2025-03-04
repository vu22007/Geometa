using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Scriptable Objects/Character")]
public class Character : ScriptableObject
{
    [SerializeField] float speed;
    [SerializeField] float maxHealth;
    [SerializeField] float damage;
    [SerializeField] Sprite sprite;
    [SerializeField] int maxAmmo;
    [SerializeField] float fireRate;
    [SerializeField] float dashSpeed;
    [SerializeField] float dashDuration;
    [SerializeField] float dashCooldown;
    public float Speed{
        get{return speed;}
    }
    public float MaxHealth{
        get{return maxHealth;}
    }
    public float Damage{
        get{return damage;}
    }
    public Sprite Sprite{
        get{return sprite;}
    }
    public int MaxAmmo{
        get{return maxAmmo;}
    }
    public float FireRate{
        get{return fireRate;}
    }

    public float DashSpeed{
        get{return dashSpeed;}
    }
    public float DashDuration{
        get{return dashDuration;}
    }
    public float DashCooldown{
        get{return dashCooldown;}
    }
}
