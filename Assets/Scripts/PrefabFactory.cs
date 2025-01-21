using UnityEngine;

public static class PrefabFactory
{
    public static Player SpawnPlayer(GameObject prefab, Vector3 spawnPosition, Character character){
        GameObject instantiatedPlayer = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
        Player player = instantiatedPlayer.GetComponent<Player>();
        player.OnCreated(character);
        return player;
    }

    public static Bullet SpawnBullet(GameObject prefab, Vector3 spawnPosition, Vector3 moveDirection, float speed, float damage){
        Quaternion wantedRotation = Quaternion.LookRotation(moveDirection);
        GameObject instantiatedBullet = Object.Instantiate(prefab, spawnPosition, wantedRotation);
        Bullet newBullet = instantiatedBullet.GetComponent<Bullet>();
        newBullet.OnCreated(moveDirection, speed, damage);
        return newBullet;
    }
}
