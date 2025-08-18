using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

    public GameObject doorPrefab; // <- NEW: Assign this in the Inspector

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
    public Dictionary<Vector2Int, GameObject> roomGameObjects = new Dictionary<Vector2Int, GameObject>(); // <- NEW

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

    public void CreateDungeon()
    {
        if (loader == null)
        {
            Debug.LogError("DungeonLoader reference missing!");
            return;
        }

        if (corridorTilePrefab != null)
        {
            tileSize = GetFullPrefabSize(corridorTilePrefab);
            Debug.Log("Tile size set from prefab: " + tileSize);
        }

        int seed = customSeed ?? System.DateTime.Now.Millisecond;
        Random.InitState(seed);
        Debug.Log("Seed used: " + seed);

        //if (useRandomGeneration)
        //    GenerateRandomDungeonFromScratch();
        //else
        loader.GetDungeonData();

        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        DungeonData data = loader.GetDungeonData();
        if (data == null)
        {
            Debug.LogError("Dungeon data is null!");
            return;
        }

        roomGameObjects.Clear();

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
                roomGameObjects.Add(roomGridPos, roomObj); // <- Track room object

                // Enemies
                foreach (Enemy enemy in room.enemies ?? new List<Enemy>())
                {
                    GameObject enemyPrefab = GetEnemyPrefab(enemy.type);
                    if (enemyPrefab != null)
                    {
                        Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
                        //GameObject enemyObj = Instantiate(enemyPrefab, roomPos + offset, Quaternion.identity, transform);
                        GameObject enemyObj = Instantiate(enemyPrefab, roomPos + offset, Quaternion.identity, roomObj.transform);


                        EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
                        if (enemyAI != null)
                        {
                            Vector2[] patrolPoints = new Vector2[3];
                            float patrolRangeX = tileSize.x * room.width / 2f;
                            float patrolRangeY = tileSize.y * room.height / 2f;
                            for (int i = 0; i < patrolPoints.Length; i++)
                            {
                                float x = roomPos.x + Random.Range(-patrolRangeX, patrolRangeX);
                                float y = roomPos.y + Random.Range(-patrolRangeY, patrolRangeY);
                                patrolPoints[i] = new Vector2(x, y);
                            }
                            enemyAI.SetPatrolPoints(patrolPoints);
                        }
                    }
                }

                // Powerups
                foreach (Powerup powerup in room.powerups ?? new List<Powerup>())
                {
                    GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                    if (powerupPrefab != null)
                    {
                        Vector3 offset = new Vector3(powerup.x, powerup.y, 0);
                        Instantiate(powerupPrefab, roomPos + offset, Quaternion.identity, roomObj.transform);
                    }
                }

                // Treasures
                foreach (Treasure treasure in room.treasures ?? new List<Treasure>())
                {
                    if (treasurePrefab != null)
                    {
                        Vector3 offset = new Vector3(treasure.x, treasure.y, 0);
                        Instantiate(treasurePrefab, roomPos + offset, Quaternion.identity, roomObj.transform);
                    }
                }

                // Spawn Player
                if (room.type == "spawn" && playerPrefab != null && player == null)
                {
                    Debug.Log("Spawning Player");
                    player = Instantiate(playerPrefab, roomPos, Quaternion.identity);
                }
                    
            }
        }

        // 🔑 DOORS between connected rooms
        foreach (Connection connection in data.connections)
        {
            Vector2Int from = new Vector2Int(connection.fromX, connection.fromY);
            Vector2Int to = new Vector2Int(connection.toX, connection.toY);

            if (roomGameObjects.TryGetValue(from, out GameObject fromRoom) &&
                roomGameObjects.TryGetValue(to, out GameObject toRoom))
            {
                Vector2Int dir = to - from;

                if (dir == Vector2Int.right)
                {
                    SpawnDoor(fromRoom, "DoorRight");
                    SpawnDoor(toRoom, "DoorLeft");
                }
                else if (dir == Vector2Int.left)
                {
                    SpawnDoor(fromRoom, "DoorLeft");
                    SpawnDoor(toRoom, "DoorRight");
                }
                else if (dir == Vector2Int.up)
                {
                    SpawnDoor(fromRoom, "DoorTop");
                    SpawnDoor(toRoom, "DoorBottom");
                }
                else if (dir == Vector2Int.down)
                {
                    SpawnDoor(fromRoom, "DoorBottom");
                    SpawnDoor(toRoom, "DoorTop");
                }
            }
        }

        // Global powerups (if any)
        foreach (Powerup powerup in data.powerups ?? new List<Powerup>())
        {
            Vector3 powerupPos = new Vector3(powerup.x * tileSize.x, 0, powerup.y * tileSize.y);
            GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
            if (powerupPrefab != null)
                Instantiate(powerupPrefab, powerupPos, Quaternion.identity);
        }

        foreach (string obj in data.objectives ?? new List<string>())
            Debug.Log("Objective: " + obj);
    }

    void SpawnDoor(GameObject roomObj, string doorPointName)
    {
        Transform doorPoint = roomObj.transform.Find(doorPointName);
        if (doorPoint != null && doorPrefab != null)
        {
            GameObject door = Instantiate(doorPrefab, doorPoint.position, doorPoint.rotation, roomObj.transform);

            DoorPortal portal = door.AddComponent<DoorPortal>();

            // Parse room grid position from dictionary
            foreach (var kvp in roomGameObjects)
            {
                if (kvp.Value == roomObj)
                {
                    portal.roomGridPosition = kvp.Key;
                    break;
                }
            }

            // Assign direction based on doorPointName
            if (doorPointName.Contains("Right")) portal.direction = "Right";
            else if (doorPointName.Contains("Left")) portal.direction = "Left";
            else if (doorPointName.Contains("Top")) portal.direction = "Top";
            else if (doorPointName.Contains("Bottom")) portal.direction = "Bottom";
        }
        else
        {
            Debug.LogWarning($"Missing door point {doorPointName} or doorPrefab not assigned.");
        }
    }

    //public void GenerateRandomDungeonFromScratch(int roomCount = 8)
    //{
    //    DungeonData dungeon = new DungeonData
    //    {
    //        rooms = new List<Room>(),
    //        connections = new List<Connection>(),
    //        powerups = new List<Powerup>(),
    //        objectives = new List<string> { "Defeat the boss", "Find the treasure" }
    //    };

    //    HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

    //    for (int i = 0; i < roomCount; i++)
    //    {
    //        //Vector2Int pos;
    //        //do
    //        //{
    //        //    pos = new Vector2Int(Random.Range(-5, 6), Random.Range(-5, 6));
    //        //} while (usedPositions.Contains(pos));
    //        Vector2Int pos = (i == 0) ? Vector2Int.zero : GetAdjacentFreePosition(usedPositions);
    //        usedPositions.Add(pos);

    //        string roomType = (i == 0) ? "spawn" :
    //                          (i == roomCount - 1) ? "boss" :
    //                          (Random.value < 0.2f) ? "treasure" :
    //                          (Random.value < 0.1f) ? "shop" :
    //                          "normal";

    //        Room room = new Room
    //        {
    //            x = pos.x,
    //            y = pos.y,
    //            width = 1,
    //            height = 1,
    //            type = roomType,
    //            enemies = new List<Enemy>(),
    //            powerups = new List<Powerup>(),
    //            treasures = new List<Treasure>()
    //        };

    //        if (room.type == "boss")
    //            room.enemies.Add(new Enemy { type = "boss" });
    //        else
    //        {
    //            int enemyCount = Random.Range(0, 3);
    //            for (int j = 0; j < enemyCount; j++)
    //            {
    //                room.enemies.Add(new Enemy
    //                {
    //                    type = (Random.value > 0.5f) ? "melee" : "ranged"
    //                });
    //            }
    //        }

    //        if (Random.value < 0.3f)
    //        {
    //            room.powerups.Add(new Powerup
    //            {
    //                x = Random.Range(0, 2),
    //                y = Random.Range(0, 2),
    //                type = (Random.value > 0.5f) ? "health" : "damage"
    //            });
    //        }

    //        if (room.type == "treasure")
    //        {
    //            room.treasures.Add(new Treasure
    //            {
    //                x = Random.Range(0, 2),
    //                y = Random.Range(0, 2)
    //            });
    //        }

    //        dungeon.rooms.Add(room);
    //    }

    //    for (int i = 0; i < dungeon.rooms.Count - 1; i++)
    //    {
    //        dungeon.connections.Add(new Connection
    //        {
    //            fromX = dungeon.rooms[i].x,
    //            fromY = dungeon.rooms[i].y,
    //            toX = dungeon.rooms[i + 1].x,
    //            toY = dungeon.rooms[i + 1].y
    //        });
    //    }

    //    loader.SetDungeonData(dungeon);
    //    Debug.Log("Random JSON-style dungeon generated.");
    //}

    Vector2Int GetAdjacentFreePosition(HashSet<Vector2Int> used)
    {
        // Directions: up, down, left, right
        Vector2Int[] directions = {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

        foreach (Vector2Int existing in used)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = existing + dir;
                if (!used.Contains(neighbor))
                    return neighbor;
            }
        }

        // Fallback if all adjacent positions are taken (shouldn't happen with few rooms)
        return new Vector2Int(Random.Range(-10, 10), Random.Range(-10, 10));
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
        roomGameObjects.Clear();

        Random.InitState(System.DateTime.Now.Millisecond);
        int nextRoomCount = Random.Range(8, 12);
        //GenerateRandomDungeonFromScratch(nextRoomCount);
        GenerateDungeon();

        if (player != null)
        {
            Room spawnRoom = loader.GetDungeonData().rooms.Find(r => r.type == "spawn");
            if (spawnRoom != null)
            {
                Vector3 newPosition = new Vector3(spawnRoom.x * tileSize.x, spawnRoom.y * tileSize.y, 0);
                player.transform.position = newPosition;
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
}
