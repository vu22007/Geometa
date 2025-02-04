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
            player.OnCreated(playerRef, characterPath, spawnPosition, team);
            runner.SetPlayerObject(playerRef, networkObject);
        });

        return networkPlayerObject;
    }

    public static NetworkObject SpawnBullet(NetworkRunner runner, GameObject prefab, Vector3 spawnPosition, Vector2 moveDirection, float speed, float damage, int team){
        // Get rotation
        Vector3 direction = new Vector3(moveDirection.x, moveDirection.y);
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);

        // Spawn the bullet network object
        NetworkObject networkBulletObject = runner.Spawn(prefab, spawnPosition, rotation, null, (runner, networkObject) =>
        {
            // Initialise the bullet (this is called before the bullet is spawned)
            Bullet bullet = networkObject.GetComponent<Bullet>();
            bullet.OnCreated(moveDirection, speed, damage, team);
        });

        return networkBulletObject;
    }

    public static NetworkObject SpawnShape(NetworkRunner runner, GameObject prefab, Vector3 spawnPosition, Quaternion rotation)
    {
        // Spawn the shape network object
        NetworkObject networkShapeObject = runner.Spawn(prefab, spawnPosition, rotation, null, (runner, networkObject) =>
        {
            // Initialise the shape (this is called before the shape is spawned)
            Shape shape = networkObject.GetComponent<Shape>();
            //shape.OnCreated(moveDirection, speed, damage, team);
        });

        return networkShapeObject;
    }
}
