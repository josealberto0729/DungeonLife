using System.Collections.Generic;
using UnityEngine;

public class DungeonSpawner : MonoBehaviour
{
    public DungeonLoader loader;
    public Vector2 tileSize = new Vector2(5f, 5f);
    public int? customSeed = null;

    // Prefabs
    public GameObject spawnRoomPrefab;
    public GameObject normalRoomPrefab;
    public GameObject treasureRoomPrefab;
    public GameObject bossRoomPrefab;
    public GameObject corridorTilePrefab;
    public GameObject shopRoomPrefab;

    public GameObject meleeEnemyPrefab;
    public GameObject rangedEnemyPrefab;
    public GameObject bossEnemyPrefab;

    public GameObject damagePowerupPrefab;
    public GameObject speedPowerupPrefab;
    public GameObject healthPowerupPrefab;

    public GameObject treasurePrefab;
    public bool useRandomGeneration = false;
    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();


    void Start()
    {
        if (loader == null)
        {
            Debug.LogError("DungeonLoader reference missing!");
            return;
        }

        // Set tile size based on prefab size
        if (corridorTilePrefab != null)
        {
            Vector3 size = GetFullPrefabSize(corridorTilePrefab);
            tileSize = new Vector2(size.x, size.y);
            Debug.Log("Tile size set from prefab: " + tileSize);
        }

        // Random seed setup
        int seed = customSeed ?? System.DateTime.Now.Millisecond;
        Random.InitState(seed);
        Debug.Log("Seed used: " + seed);
        if (useRandomGeneration)
        {
            GenerateRandomDungeonFromScratch();
        }
            GenerateDungeon();
    }

    public void GenerateRandomDungeonFromScratch(int roomCount = 8)
    {
        DungeonData dungeon = new DungeonData
        {
            rooms = new List<Room>(),
            connections = new List<Connection>(),
            powerups = new List<Powerup>(),
            objectives = new List<string> { "Defeat the boss", "Find the treasure" }
        };

        HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

        for (int i = 0; i < roomCount; i++)
        {
            Vector2Int pos;
            do
            {
                pos = new Vector2Int(Random.Range(-5, 6), Random.Range(-5, 6));
            } while (usedPositions.Contains(pos));
            usedPositions.Add(pos);

            string roomType = (i == 0) ? "spawn" :
                              (i == roomCount - 1) ? "boss" :
                              (Random.value < 0.2f) ? "treasure" :
                              (Random.value < 0.1f) ? "shop" :
                              "normal";

            Room room = new Room
            {
                x = pos.x,
                y = pos.y,
                width = 1,
                height = 1,
                type = roomType,
                enemies = new List<Enemy>(),
                powerups = new List<Powerup>(),
                treasures = new List<Treasure>()
            };

            int enemyCount = Random.Range(0, 3);
            for (int j = 0; j < enemyCount; j++)
            {
                room.enemies.Add(new Enemy
                {
                    type = (Random.value > 0.5f) ? "melee" : "ranged"
                });
            }

            if (Random.value < 0.3f)
            {
                room.powerups.Add(new Powerup
                {
                    x = Random.Range(0, 2),
                    y = Random.Range(0, 2),
                    type = (Random.value > 0.5f) ? "health" : "damage"
                });
            }

            if (room.type == "treasure")
            {
                room.treasures.Add(new Treasure
                {
                    x = Random.Range(0, 2),
                    y = Random.Range(0, 2)
                });
            }

            dungeon.rooms.Add(room);
        }

        // Create simple connections between sequential rooms
        for (int i = 0; i < dungeon.rooms.Count - 1; i++)
        {
            dungeon.connections.Add(new Connection
            {
                fromX = dungeon.rooms[i].x,
                fromY = dungeon.rooms[i].y,
                toX = dungeon.rooms[i + 1].x,
                toY = dungeon.rooms[i + 1].y
            });
        }

        loader.SetDungeonData(dungeon);
        Debug.Log("Random JSON-style dungeon generated.");
    }


    void GenerateCorridors(Connection[] connections)
    {
        foreach (var conn in connections)
        {
            Vector2Int start = new Vector2Int(conn.fromX, conn.fromY);
            Vector2Int end = new Vector2Int(conn.toX, conn.toY);
            Vector2Int current = start;

            while (current.x != end.x)
            {
                current.x += (end.x > current.x) ? 1 : -1;

                if (!occupiedPositions.Contains(current))
                {
                    Vector3 pos = new Vector3(current.x * tileSize.x, current.y * tileSize.y, 0);
                    GameObject corridorTile = Instantiate(corridorTilePrefab, pos, Quaternion.identity);

                    TrySpawnCorridorEnemy(pos, corridorTile.transform);
                }
            }

            while (current.y != end.y)
            {
                current.y += (end.y > current.y) ? 1 : -1;

                if (!occupiedPositions.Contains(current))
                {
                    Vector3 pos = new Vector3(current.x * tileSize.x, current.y * tileSize.y, 0);
                    GameObject corridorTile = Instantiate(corridorTilePrefab, pos, Quaternion.identity);

                    TrySpawnCorridorEnemy(pos, corridorTile.transform);
                }
            }
        }
    }


    void TrySpawnCorridorEnemy(Vector3 position, Transform parent)
    {
        float spawnChance = 0.15f; // 15% chance to spawn enemy in corridor tile

        if (Random.value < spawnChance)
        {
            GameObject enemyPrefab = (Random.value > 0.5f) ? meleeEnemyPrefab : rangedEnemyPrefab;
            if (enemyPrefab != null)
            {
                Vector3 spawnPos = position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity, parent);  // parent assigned here
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

        foreach (Room room in data.rooms)
        {
            Vector2Int roomGridPos = new Vector2Int(room.x, room.y);
            if (occupiedPositions.Contains(roomGridPos))
            {
                Debug.LogWarning($"Room at {roomGridPos} already instantiated. Skipping.");
                continue;
            }
            occupiedPositions.Add(roomGridPos);

            Vector3 roomPos = new Vector3(room.x * tileSize.x, room.y * tileSize.y, 0);
            GameObject roomPrefab = GetRoomPrefab(room.type);

            if (roomPrefab != null)
            {
                GameObject roomObj = Instantiate(roomPrefab, roomPos, Quaternion.identity);

                foreach (Enemy enemy in room.enemies)
                {
                    float enemyX = roomPos.x + Random.Range(0f, room.width) * tileSize.x;
                    float enemyY = roomPos.y + Random.Range(0f, room.height) * tileSize.y;
                    Vector3 enemyPos = new Vector3(enemyX, enemyY, 0);

                    GameObject enemyPrefab = GetEnemyPrefab(enemy.type);
                    if (enemyPrefab != null)
                        Instantiate(enemyPrefab, enemyPos, Quaternion.identity, roomObj.transform);
                }

                foreach (Powerup powerup in room.powerups)
                {
                    Vector3 powerupPos = new Vector3(powerup.x * tileSize.x, powerup.y * tileSize.y, 0);
                    GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                    if (powerupPrefab != null)
                        Instantiate(powerupPrefab, powerupPos, Quaternion.identity, roomObj.transform);
                }

                foreach (Treasure treasure in room.treasures)
                {
                    Vector3 treasurePos = new Vector3(treasure.x * tileSize.x, treasure.y * tileSize.y, 0);
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
            GenerateCorridors(data.connections.ToArray());

        if (data.powerups != null)
        {
            foreach (Powerup powerup in data.powerups)
            {
                Vector3 powerupPos = new Vector3(powerup.x * tileSize.x, 0, powerup.y * tileSize.y);
                GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                if (powerupPrefab != null)
                    Instantiate(powerupPrefab, powerupPos, Quaternion.identity);
                else
                    Debug.LogWarning($"Powerup prefab not found for type: {powerup.type}");
            }
        }

        if (data.objectives != null)
        {
            foreach (string obj in data.objectives)
                Debug.Log("Objective: " + obj);
        }
    }

    Vector3 GetFullPrefabSize(GameObject prefab)
    {
        GameObject temp = Instantiate(prefab);
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Destroy(temp);
            return Vector3.one; // fallback
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 size = combinedBounds.size;
        Destroy(temp);
        return size;
    }

    GameObject GetRoomPrefab(string type)
    {
        switch (type.ToLower())
        {
            case "spawn": return spawnRoomPrefab;
            case "normal": return normalRoomPrefab;
            case "treasure": return treasureRoomPrefab;
            case "boss": return bossRoomPrefab;
            case "shop": return shopRoomPrefab;
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
