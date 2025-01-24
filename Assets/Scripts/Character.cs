using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Scriptable Objects/Character")]
public class Character : ScriptableObject
{
    [SerializeField] float speed;
    [SerializeField] float maxHealth;
    [SerializeField] float damage;
    [SerializeField] Ability ability;
    [SerializeField] Sprite sprite;
    [SerializeField] int maxAmmo;
    [SerializeField] float fireRate;

    public float Speed{
        get{return speed;}
    }
    public float MaxHealth{
        get{return maxHealth;}
    }
    public float Damage{
        get{return damage;}
    }
    public Ability Ability{
        get{return ability;}
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

}
