using Fusion;
using UnityEngine;

public static class PrefabFactory
{
    public static NetworkObject SpawnPlayer(NetworkRunner runner, PlayerRef playerRef, GameObject prefab, Vector3 spawnPosition, string characterPath, int team){
        // Spawn the player network object
        NetworkObject networkPlayerObject = runner.Spawn(prefab, spawnPosition, Quaternion.identity, playerRef, (runner, networkObject) =>
        {
            // Initialise the player (this is called before the player is spawned)
            Player player = networkObject.GetComponent<Player>();
            player.OnCreated(characterPath, spawnPosition, team);
            runner.SetPlayerObject(playerRef, networkObject);
        });

        return networkPlayerObject;
    }

    public static NetworkObject SpawnWorldCollider(NetworkRunner runner, GameObject prefab)
    {
        NetworkObject worldCollider = runner.Spawn(prefab, Vector3.zero);
        return worldCollider;
    }
    public static NetworkObject SpawnBullet(NetworkRunner runner, PlayerRef playerRef, GameObject prefab, Vector3 spawnPosition, Vector2 moveDirection, float speed, float damage, int team, Player playerFiring){
        // Get rotation
        Vector3 direction = new Vector3(moveDirection.x, moveDirection.y);
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);

        // Spawn the bullet network object
        NetworkObject networkBulletObject = runner.Spawn(prefab, spawnPosition, rotation, playerRef, (runner, networkObject) =>
        {
            // Initialise the bullet (this is called before the bullet is spawned)
            Bullet bullet = networkObject.GetComponent<Bullet>();
            bullet.OnCreated(spawnPosition, moveDirection, rotation, speed, damage, team, playerFiring);
        });

        return networkBulletObject;
    }

    //For type: 0 is health, 1 is points
    public static NetworkObject SpawnPickup(NetworkRunner runner, GameObject prefab, Vector3 spawnPosition, int type, int amount){

        NetworkObject networkPickupObject = runner.Spawn(prefab, spawnPosition, Quaternion.identity, null, (runner, networkObject) =>
        {
            // Initialise the pickup (this is called before the pickup is spawned)
            Pickup pickup = networkObject.GetComponent<Pickup>();
            pickup.OnCreated(type, amount);
        });

        return networkPickupObject;
    }

    public static NetworkObject SpawnFlag(NetworkRunner runner, GameObject prefab, Vector3 spawnPosition, int team)
    {
        NetworkObject networkFlagObject = runner.Spawn(prefab, spawnPosition, Quaternion.identity, null, (runner, networkObject) =>
        {
            // Initialise the pickup (this is called before the pickup is spawned)
            PickupFlag flag = networkObject.GetComponent<PickupFlag>();
            flag.OnCreated(team);
        });

        return networkFlagObject;
    }

}
