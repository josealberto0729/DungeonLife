using System.Collections.Generic;
using UnityEngine;

public class DungeonSpawner : MonoBehaviour
{
    public static DungeonSpawner Instance { get; private set; }
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
    public GameObject playerPrefab;

    public GameObject player;
    public bool useRandomGeneration = false;

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {

    }

    public void CreateDungeon()
    {
        if (loader == null)
        {
            Debug.LogError("DungeonLoader reference missing!");
            return;
        }

        // Set tile size from corridor prefab
        if (corridorTilePrefab != null)
        {
            tileSize = GetFullPrefabSize(corridorTilePrefab);
            Debug.Log("Tile size set from prefab: " + tileSize);
        }

        int seed = customSeed ?? System.DateTime.Now.Millisecond;
        Random.InitState(seed);
        Debug.Log("Seed used: " + seed);

        if (useRandomGeneration)
        {
            GenerateRandomDungeonFromScratch();
        }
        else
        {
            loader.GetDungeonData(); // no file extension
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

            if (room.type == "boss")
                room.enemies.Add(new Enemy { type = "boss" });
            else
            {
                int enemyCount = Random.Range(0, 3);
                for (int j = 0; j < enemyCount; j++)
                {
                    room.enemies.Add(new Enemy
                    {
                        type = (Random.value > 0.5f) ? "melee" : "ranged"
                    });
                }
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
            if (occupiedPositions.Contains(roomGridPos)) continue;

            occupiedPositions.Add(roomGridPos);

            Vector3 roomPos = new Vector3(room.x * tileSize.x, room.y * tileSize.y, 0);
            GameObject roomPrefab = GetRoomPrefab(room.type);

            if (roomPrefab != null)
            {
                GameObject roomObj = Instantiate(roomPrefab, roomPos, Quaternion.identity, transform);

                foreach (Enemy enemy in room.enemies)
                {
                    float offsetX = Random.Range(-tileSize.x * 0.4f, tileSize.x * 0.4f);
                    float offsetY = Random.Range(-tileSize.y * 0.4f, tileSize.y * 0.4f);
                    Vector3 enemyPos = roomPos + new Vector3(offsetX, offsetY, 0);
                    GameObject enemyPrefab = GetEnemyPrefab(enemy.type);
                    if (enemyPrefab != null)
                        Instantiate(enemyPrefab, enemyPos, Quaternion.identity, roomObj.transform);
                }

                foreach (Powerup powerup in room.powerups)
                {
                    Vector3 powerupPos = roomPos + new Vector3(powerup.x * tileSize.x, powerup.y * tileSize.y, 0);
                    GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                    if (powerupPrefab != null)
                        Instantiate(powerupPrefab, powerupPos, Quaternion.identity, roomObj.transform);
                }

                foreach (Treasure treasure in room.treasures)
                {
                    Vector3 treasurePos = roomPos + new Vector3(treasure.x * tileSize.x, treasure.y * tileSize.y, 0);
                    if (treasurePrefab != null)
                        Instantiate(treasurePrefab, treasurePos, Quaternion.identity, roomObj.transform);
                }

                if (room.type == "spawn" && playerPrefab != null && player == null)
                {
                    player = Instantiate(playerPrefab, roomPos, Quaternion.identity);
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
            }
        }

        if (data.objectives != null)
        {
            foreach (string obj in data.objectives)
                Debug.Log("Objective: " + obj);
        }
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
                TryPlaceCorridorTile(current);
            }

            while (current.y != end.y)
            {
                current.y += (end.y > current.y) ? 1 : -1;
                TryPlaceCorridorTile(current);
            }
        }
    }

    void TryPlaceCorridorTile(Vector2Int position)
    {
        if (occupiedPositions.Add(position))
        {
            Vector3 worldPos = new Vector3(position.x * tileSize.x, position.y * tileSize.y, 0);
            GameObject corridorTile = Instantiate(corridorTilePrefab, worldPos, Quaternion.identity, transform);
            TrySpawnCorridorEnemy(worldPos, corridorTile.transform);
        }
    }

    void TrySpawnCorridorEnemy(Vector3 position, Transform parent)
    {
        if (Random.value < 0.15f)
        {
            GameObject enemyPrefab = (Random.value > 0.5f) ? meleeEnemyPrefab : rangedEnemyPrefab;
            if (enemyPrefab != null)
            {
                Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0);
                Instantiate(enemyPrefab, position + offset, Quaternion.identity, parent);
            }
        }
    }

    Vector2 GetFullPrefabSize(GameObject prefab)
    {
        GameObject temp = Instantiate(prefab);
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Destroy(temp);
            return Vector2.one;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Destroy(temp);
        return new Vector2(bounds.size.x, bounds.size.y);
    }

    GameObject GetRoomPrefab(string type) => type.ToLower() switch
    {
        "spawn" => spawnRoomPrefab,
        "normal" => normalRoomPrefab,
        "treasure" => treasureRoomPrefab,
        "boss" => bossRoomPrefab,
        "shop" => shopRoomPrefab,
        _ => null
    };

    GameObject GetEnemyPrefab(string type) => type.ToLower() switch
    {
        "melee" => meleeEnemyPrefab,
        "ranged" => rangedEnemyPrefab,
        "boss" => bossEnemyPrefab,
        _ => null
    };

    GameObject GetPowerupPrefab(string type) => type.ToLower() switch
    {
        "damage" => damagePowerupPrefab,
        "speed" => speedPowerupPrefab,
        "health" => healthPowerupPrefab,
        _ => null
    };

    public void GenerateNextLevel()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        occupiedPositions.Clear();
        Random.InitState(System.DateTime.Now.Millisecond);
        int nextRoomCount = Random.Range(8, 12);
        GenerateRandomDungeonFromScratch(nextRoomCount);
        GenerateDungeon();
        if (player != null)
        {
            // Find spawn room's position
            Room spawnRoom = loader.GetDungeonData().rooms.Find(r => r.type == "spawn");
            if (spawnRoom != null)
            {
                Vector3 newPosition = new Vector3(spawnRoom.x * tileSize.x, spawnRoom.y * tileSize.y, 0);
                player.transform.position = newPosition;
            }
            else
            {
                Debug.LogWarning("Spawn room not found in next level!");
            }
        }
    }
}
