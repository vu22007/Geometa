using UnityEngine;

public static class PrefabFactory
{
    static Vector3 up = new Vector3(0.0f,0.0f,-1.0f);
    public static Player SpawnPlayer(GameObject prefab, Vector3 spawnPosition, Character character){
        GameObject instantiatedPlayer = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
        Player player = instantiatedPlayer.GetComponent<Player>();
        player.OnCreated(character, spawnPosition);
        return player;
    }

    public static Bullet SpawnBullet(GameObject prefab, Vector3 spawnPosition, Vector3 moveDirection, float speed, float damage){
        Quaternion wantedRotation = Quaternion.LookRotation(moveDirection, up);
        GameObject instantiatedBullet = Object.Instantiate(prefab, spawnPosition, wantedRotation);
        Bullet newBullet = instantiatedBullet.GetComponent<Bullet>();
        moveDirection.z = 0;
        newBullet.OnCreated(moveDirection.normalized, speed, damage);
        return newBullet;
    }
}
