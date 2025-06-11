using System.Collections.Generic;
using UnityEngine;

public class DungeonSpawner : MonoBehaviour
{
    public DungeonLoader loader;
    public float tileSize = 5f;

    // Prefabs
    public GameObject spawnRoomPrefab;
    public GameObject normalRoomPrefab;
    public GameObject treasureRoomPrefab;
    public GameObject bossRoomPrefab;
    public GameObject corridorTilePrefab; 

    public GameObject meleeEnemyPrefab;
    public GameObject rangedEnemyPrefab;
    public GameObject bossEnemyPrefab;

    public GameObject damagePowerupPrefab;
    public GameObject speedPowerupPrefab;
    public GameObject healthPowerupPrefab;

    public GameObject treasurePrefab; 



    void Start()
    {
        if (loader == null)
        {
            Debug.LogError("DungeonLoader reference missing!");
            return;
        }
        GenerateDungeon();
    }

    void GenerateCorridors(Connection[] connections)
    {
        foreach (var conn in connections)
        {
            Vector2Int start = new Vector2Int(conn.fromX, conn.fromY);
            Vector2Int end = new Vector2Int(conn.toX, conn.toY);
            Vector2Int current = start;

            // Move horizontally
            while (current.x != end.x)
            {
                current.x += (end.x > current.x) ? 1 : -1;
                Instantiate(corridorTilePrefab, new Vector3(current.x * tileSize, current.y * tileSize, 0), Quaternion.identity);
            }

            // Move vertically
            while (current.y != end.y)
            {
                current.y += (end.y > current.y) ? 1 : -1;
                Instantiate(corridorTilePrefab, new Vector3(current.x * tileSize, current.y * tileSize, 0), Quaternion.identity);
            }
        }
    }

    void GenerateDungeon()
    {
        DungeonData data = loader.GetDungeonData();
        if (data == null)
        {
            Debug.LogError("Dungeon data is null!");
            return;
        }

        // Spawn Rooms and Enemies
        foreach (Room room in data.rooms)
        {
            //Vector3 roomPos = new Vector3(room.x * tileSize, 0, room.y * tileSize);
            Vector3 roomPos = new Vector3(room.x * tileSize, room.y * tileSize, 0);

            GameObject roomPrefab = GetRoomPrefab(room.type);
            if (roomPrefab != null)
            {
                GameObject roomObj = Instantiate(roomPrefab, roomPos, Quaternion.identity);

                // Enemies
                foreach (Enemy enemy in room.enemies)
                {
                    float enemyX = roomPos.x + Random.Range(0f, room.width) * tileSize;
                    float enemyY = roomPos.y + Random.Range(0f, room.height) * tileSize;
                    Vector3 enemyPos = new Vector3(enemyX, enemyY, 0);


                    GameObject enemyPrefab = GetEnemyPrefab(enemy.type);
                    if (enemyPrefab != null)
                        Instantiate(enemyPrefab, enemyPos, Quaternion.identity, roomObj.transform);
                }

                // Powerups in room
                foreach (Powerup powerup in room.powerups)
                {
                    Vector3 powerupPos = new Vector3(powerup.x * tileSize, powerup.y * tileSize, 0);
                    GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                    if (powerupPrefab != null)
                        Instantiate(powerupPrefab, powerupPos, Quaternion.identity, roomObj.transform);
                }

                // Treasures in room
                foreach (Treasure treasure in room.treasures)
                {
                    Vector3 treasurePos = new Vector3(treasure.x * tileSize, treasure.y * tileSize, 0);
                    if (treasurePrefab != null)
                        Instantiate(treasurePrefab, treasurePos, Quaternion.identity, roomObj.transform);
                }
            }
            else
            {
                Debug.LogWarning($"Room prefab not found for type: {room.type}");
            }
        }
        if (data.connections != null)
        {
            GenerateCorridors(data.connections);
        }


        if (data.powerups != null)
        {
            // Powerups
            foreach (Powerup powerup in data.powerups)
            {
                Vector3 powerupPos = new Vector3(powerup.x * tileSize, 0, powerup.y * tileSize);
                GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                if (powerupPrefab != null)
                    Instantiate(powerupPrefab, powerupPos, Quaternion.identity);
                else
                    Debug.LogWarning($"Powerup prefab not found for type: {powerup.type}");
            }
        }

        // Objectives
        if (data.objectives != null)
        {
            foreach (string obj in data.objectives)
            {
                Debug.Log("Objective: " + obj);
            }
        }
    }
    Vector3 GetPrefabSize(GameObject prefab)
    {
        Renderer renderer = prefab.GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.bounds.size : Vector3.one;
    }
    GameObject GetRoomPrefab(string type)
    {
        switch (type.ToLower())
        {
            case "spawn": return spawnRoomPrefab;
            case "normal": return normalRoomPrefab;
            case "treasure": return treasureRoomPrefab;
            case "boss": return bossRoomPrefab;
            default: return null;
        }
    }

    GameObject GetEnemyPrefab(string type)
    {
        switch (type.ToLower())
        {
            case "melee": return meleeEnemyPrefab;
            case "ranged": return rangedEnemyPrefab;
            case "boss": return bossEnemyPrefab;
            default: return null;
        }
    }

    GameObject GetPowerupPrefab(string type)
    {
        switch (type.ToLower())
        {
            case "damage": return damagePowerupPrefab;
            case "speed": return speedPowerupPrefab;
            case "health": return healthPowerupPrefab;
            default: return null;
        }
    }
}
