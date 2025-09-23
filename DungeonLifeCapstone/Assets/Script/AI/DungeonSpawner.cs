using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    public GameObject doorPrefab;

    public GameObject meleeEnemyPrefab;
    public GameObject rangedEnemyPrefab;
    public GameObject bossEnemyPrefab;

    public GameObject damagePowerupPrefab;
    public GameObject speedPowerupPrefab;
    public GameObject healthPowerupPrefab;

    public GameObject treasurePrefab;
    public GameObject playerPrefab;

    public GameObject victoryPortalPrefab;

    public GameObject player;
    public bool useRandomGeneration = false;

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
    public Dictionary<Vector2Int, GameObject> roomGameObjects = new Dictionary<Vector2Int, GameObject>();

    public UnityEvent allEnemySpawned;

    public GameObject retryPanel;

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
    public void SpawnVictoryPortal()
    {
        if (loader == null)
        {
            Debug.LogWarning("DungeonLoader is missing!");
            return;
        }

        DungeonData data = loader.GetDungeonData();
        if (data == null)
        {
            Debug.LogWarning("DungeonData is null!");
            return;
        }

        // Find the boss room in the data
        Room bossRoomData = data.rooms.Find(r => r.type.ToLower() == "boss");
        if (bossRoomData == null)
        {
            Debug.LogWarning("No boss room found in dungeon data!");
            return;
        }

        Vector2Int bossRoomPos = new Vector2Int(bossRoomData.x, bossRoomData.y);

        if (roomGameObjects.TryGetValue(bossRoomPos, out GameObject bossRoomGO))
        {
            Vector3 spawnPos = bossRoomGO.transform.position + Vector3.up * 1f; // slightly above the center
            Instantiate(victoryPortalPrefab, spawnPos, Quaternion.identity);
            Debug.Log("Victory portal spawned in boss room!");
        }
        else
        {
            Debug.LogWarning("Boss room GameObject not found! Spawning portal at origin.");
            Instantiate(victoryPortalPrefab, Vector3.zero, Quaternion.identity);
        }
    }

    public void Start()
    {
        CreateDungeon();
        //player.GetComponent<PlayerStatsHandler>().retryPanel = retryPanel;
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

        // Spawn rooms
        Debug.Log("rooms are : " + data.rooms +" : " + data.rooms.Count);
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
                roomGameObjects.Add(roomGridPos, roomObj);

                // Spawn enemies
                foreach (Enemy enemy in room.enemies ?? new List<Enemy>())
                {
                    GameObject enemyPrefab = GetEnemyPrefab(enemy.type);
                    if (enemyPrefab != null)
                    {
                        Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
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
                roomObj.GetComponent<RoomManager>().FillEnemyList();

                // Spawn powerups
                foreach (Powerup powerup in room.powerups ?? new List<Powerup>())
                {
                    GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                    if (powerupPrefab != null)
                    {
                        Vector3 offset = new Vector3(powerup.x, powerup.y, 0);
                        Instantiate(powerupPrefab, roomPos + offset, Quaternion.identity, roomObj.transform);
                    }
                }

                // Spawn treasures
                foreach (Treasure treasure in room.treasures ?? new List<Treasure>())
                {
                    if (treasurePrefab != null)
                    {
                        Vector3 offset = new Vector3(treasure.x, treasure.y, 0);
                        Instantiate(treasurePrefab, roomPos + offset, Quaternion.identity, roomObj.transform);
                    }
                }

                // Spawn player
                if (room.type == "spawn" && playerPrefab != null && player == null)
                {
                    Debug.Log("Spawning Player");
                    player = Instantiate(playerPrefab, roomPos, Quaternion.identity);
                }
            }
        }

        // Spawn corridors & doors
        foreach (Connection connection in data.connections)
        {
            SpawnCorridor(connection.fromX, connection.fromY, connection.toX, connection.toY);
        }

        // Spawn global powerups
        foreach (Powerup powerup in data.powerups ?? new List<Powerup>())
        {
            Vector3 powerupPos = new Vector3(powerup.x * tileSize.x, 0, powerup.y * tileSize.y);
            GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
            if (powerupPrefab != null)
                Instantiate(powerupPrefab, powerupPos, Quaternion.identity);
        }

        // Log objectives
        foreach (string obj in data.objectives ?? new List<string>())
            Debug.Log("Objective: " + obj);
    }

    void SpawnCorridor(int fromX, int fromY, int toX, int toY)
    {
        Vector2Int from = new Vector2Int(fromX, fromY);
        Vector2Int to = new Vector2Int(toX, toY);
        Vector2Int current = from;

        // Horizontal path first
        while (current.x != to.x)
        {
            Vector2Int next = new Vector2Int(current.x + (to.x > current.x ? 1 : -1), current.y);
            SpawnCorridorTile(next);
            current = next;
        }

        // Vertical path second
        while (current.y != to.y)
        {
            Vector2Int next = new Vector2Int(current.x, current.y + (to.y > current.y ? 1 : -1));
            SpawnCorridorTile(next);
            current = next;
        }

        // ✅ Always try to spawn doors between the two rooms at endpoints
        TrySpawnDoorBetweenRooms(from, to);
        TrySpawnDoorBetweenRooms(to, from);
    }

    void SpawnCorridorTile(Vector2Int pos)
    {
        if (corridorTilePrefab != null)
        {
            Vector3 worldPos = new Vector3(pos.x * tileSize.x, pos.y * tileSize.y, 0);
            Instantiate(corridorTilePrefab, worldPos, Quaternion.identity, transform);
        }
    }

    void TrySpawnDoorBetweenRooms(Vector2Int from, Vector2Int to)
    {
        if (roomGameObjects.TryGetValue(from, out GameObject fromRoom) &&
            roomGameObjects.TryGetValue(to, out GameObject toRoom))
        {
            Vector2Int dir = to - from;

            if (dir == Vector2Int.right)
            {
                SpawnDoor(fromRoom, "DoorRight", to);
                SpawnDoor(toRoom, "DoorLeft", from);
            }
            else if (dir == Vector2Int.left)
            {
                SpawnDoor(fromRoom, "DoorLeft", to);
                SpawnDoor(toRoom, "DoorRight", from);
            }
            else if (dir == Vector2Int.up)
            {
                SpawnDoor(fromRoom, "DoorTop", to);
                SpawnDoor(toRoom, "DoorBottom", from);
            }
            else if (dir == Vector2Int.down)
            {
                SpawnDoor(fromRoom, "DoorBottom", to);
                SpawnDoor(toRoom, "DoorTop", from);
            }
        }
    }

    void SpawnDoor(GameObject roomObj, string doorPointName, Vector2Int targetRoomGridPos)
    {
        Transform doorPoint = roomObj.transform.Find(doorPointName);
        if (doorPoint != null && doorPrefab != null)
        {
            GameObject door = Instantiate(doorPrefab, doorPoint.position, doorPoint.rotation, roomObj.transform);
            DoorPortal portal = door.AddComponent<DoorPortal>();
            portal.roomGridPosition = GetRoomGridPosition(roomObj);
            portal.targetRoomGridPosition = targetRoomGridPos;

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

    Vector2Int GetRoomGridPosition(GameObject roomObj)
    {
        foreach (var kvp in roomGameObjects)
        {
            if (kvp.Value == roomObj)
                return kvp.Key;
        }
        return Vector2Int.zero;
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

    //public void LoadNewDungeonFromJson(DungeonData newData)
    //{
    //    foreach (Transform child in transform) Destroy(child.gameObject); occupiedPositions.Clear(); roomGameObjects.Clear(); Random.InitState(System.DateTime.Now.Millisecond); int nextRoomCount = Random.Range(8, 12);
    //}
    public void GenerateNextLevel()
    {

        foreach (Transform child in transform) Destroy(child.gameObject);
        occupiedPositions.Clear(); 
        roomGameObjects.Clear(); 
        Random.InitState(System.DateTime.Now.Millisecond); 
        int nextRoomCount = Random.Range(8, 12);
        CreateDungeon();
        if (player != null) 
        { 
            Room spawnRoom = loader.GetDungeonData().rooms.Find(r => r.type == "spawn"); 
            if (spawnRoom != null) 
            { Vector3 newPosition = new Vector3(spawnRoom.x * tileSize.x, spawnRoom.y * tileSize.y, 0); 
                player.transform.position = newPosition; 
            } 
        }
    }
}
