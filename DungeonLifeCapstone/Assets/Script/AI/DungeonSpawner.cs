using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DungeonSpawner : MonoBehaviour
{
    public static DungeonSpawner Instance { get; private set; }
    public DungeonLoader loader;
    public Vector2 tileSize = new Vector2(5f,5f);
    public int? customSeed = null;

    // Prefabs
    public GameObject spawnRoomPrefab;
    public GameObject normalRoomPrefab;
    public GameObject treasureRoomPrefab;
    public GameObject bossRoomPrefab;
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

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
    public Dictionary<Vector2Int, GameObject> roomGameObjects = new Dictionary<Vector2Int, GameObject>();
    // map final grid anchor -> Room data so SpawnDoor can compute fallback positions
    private Dictionary<Vector2Int, Room> roomDataByGrid = new Dictionary<Vector2Int, Room>();

    // dedicated parent for all generated dungeon objects
    private Transform roomsParent;

    public UnityEvent allEnemySpawned;

    public GameObject retryPanel;
    public GameObject playerSpawnPoint;

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
            Vector3 spawnPos = bossRoomGO.transform.position + Vector3.up *1f; // slightly above the center
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
        //CreateDungeon();
        //player.GetComponent<PlayerStatsHandler>().retryPanel = retryPanel;
    }


    public void CreateDungeon()
    {
        if (loader == null)
        {
            Debug.LogError("DungeonLoader reference missing!");
            return;
        }

        // Destroy previously created rooms container only to avoid interfering with other generators
        if (roomsParent != null)
        {
            Destroy(roomsParent.gameObject);
            roomsParent = null;
        }

        occupiedPositions.Clear();
        roomGameObjects.Clear();
        roomDataByGrid.Clear();

        // create new parent for this generation
        roomsParent = new GameObject("DungeonRooms").transform;
        roomsParent.parent = transform;

        if (normalRoomPrefab != null)
        {
            tileSize = GetFullPrefabSize(normalRoomPrefab);
            Debug.Log("Tile size set from prefab: " + tileSize);
        }

        // Force the loader to reload data from JSON so we start from original coordinates
        // each time CreateDungeon is called (prevents mutated coordinates from previous runs)
        try
        {
            loader.SetDungeonData(null);
        }
        catch { }

        loader.GetDungeonData();
        var disabledGenerators = new List<MonoBehaviour>();
        // Temporarily disable other generator components in the scene to avoid them spawning rooms
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            // include inactive to match behavior of FindObjectsOfType
            var components = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in components)
            {
                if (mb == null || mb == this) continue;
                var tname = mb.GetType().Name;
                if (tname.Contains("Generator") || tname.Contains("BSP") || tname.Contains("Procedural") || tname.Contains("OpenAI"))
                {
                    if (mb.enabled)
                    {
                        mb.enabled = false;
                        disabledGenerators.Add(mb);
                    }
                }
            }
        }

        GenerateDungeon();
        MenuController.Instance.ShowIngameView();

        // Re-enable previously disabled generator components
        foreach (var mb in disabledGenerators)
            mb.enabled = true;

    }
    public void RespawnPlayer()
    {
        player = Instantiate(playerPrefab, playerSpawnPoint.transform.position, Quaternion.identity);
    }

    void GenerateDungeon()
    {
        DungeonData data = loader.GetDungeonData();
        if (data == null)
        {
            Debug.LogError("Dungeon data is null!");
            return;
        }

        // Debug: how many rooms present in the JSON
        int jsonRoomCount = data.rooms != null ? data.rooms.Count :0;
        Debug.Log($"Rooms in JSON: {jsonRoomCount}");

        //1) Fix overlaps and get mapping original -> final anchor
        Dictionary<Vector2Int, Vector2Int> originalToFinal = FixOverlappingRooms(data);

        // Clear state
        occupiedPositions.Clear();
        roomGameObjects.Clear();
        roomDataByGrid.Clear();

        //2) Spawn rooms at final positions (anchor = top-left or anchor definition you use)
        Debug.Log("rooms are : " + data.rooms + " : " + data.rooms.Count);
        foreach (Room room in data.rooms)
        {
            // final anchor already stored in room.x / room.y by FixOverlappingRooms
            Vector2Int roomGridPos = new Vector2Int(room.x, room.y);

            // avoid duplicated instantiation if multiple tiles covered the same anchor
            if (occupiedPositions.Contains(roomGridPos))
            {
                Debug.Log($"Skipping instantiation for grid {roomGridPos} because occupied.");
                continue;
            }

            // mark occupied by anchor (we also mark footprint below)
            occupiedPositions.Add(roomGridPos);

            Vector3 roomPos = new Vector3(room.x * tileSize.x, room.y * tileSize.y,0);
            GameObject roomPrefab = GetRoomPrefab(room.type);

            if (roomPrefab != null)
            {
                GameObject roomObj = Instantiate(roomPrefab, roomPos, Quaternion.identity, roomsParent);
                Debug.Log($"Instantiated room prefab '{roomPrefab.name}' type='{room.type}' at grid={roomGridPos} world={roomPos}");
                roomGameObjects.Add(roomGridPos, roomObj);
                // store room data for door fallback calculations
                roomDataByGrid[roomGridPos] = room;

                // mark the full footprint as occupied for other placement logic (optional)
                for (int x = room.x; x < room.x + room.width; x++)
                for (int y = room.y; y < room.y + room.height; y++)
                occupiedPositions.Add(new Vector2Int(x, y));

                // Spawn enemies
                foreach (Enemy enemy in room.enemies ?? new List<Enemy>())
                {
                    GameObject enemyPrefab = GetEnemyPrefab(enemy.type);
                    if (enemyPrefab != null)
                    {
                        Vector3 offset = new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f),0);
                        GameObject enemyObj = Instantiate(enemyPrefab, roomPos + offset, Quaternion.identity, roomObj.transform);
                        EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
                        if (enemyAI != null)
                        {
                            Vector2[] patrolPoints = new Vector2[3];
                            float patrolRangeX = tileSize.x * room.width /2f;
                            float patrolRangeY = tileSize.y * room.height /2f;
                            for (int i =0; i < patrolPoints.Length; i++)
                            {
                                float x = roomPos.x + Random.Range(-patrolRangeX, patrolRangeX);
                                float y = roomPos.y + Random.Range(-patrolRangeY, patrolRangeY);
                                patrolPoints[i] = new Vector2(x, y);
                            }
                            enemyAI.SetPatrolPoints(patrolPoints);
                        }
                    }
                }
                RoomManager rm = roomObj.GetComponent<RoomManager>();
                if (rm != null) rm.FillEnemyList();

                // Spawn powerups
                foreach (Powerup powerup in room.powerups ?? new List<Powerup>())
                {
                    GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
                    if (powerupPrefab != null)
                    {
                        Vector3 offset = new Vector3(powerup.x, powerup.y,0);
                        Instantiate(powerupPrefab, roomPos + offset, Quaternion.identity, roomObj.transform);
                    }
                }

                // Spawn treasures
                foreach (Treasure treasure in room.treasures ?? new List<Treasure>())
                {
                    if (treasurePrefab != null)
                    {
                        Vector3 offset = new Vector3(treasure.x, treasure.y,0);
                        Instantiate(treasurePrefab, roomPos + offset, Quaternion.identity, roomObj.transform);
                    }
                }

                // Spawn player
                if (room.type == "spawn" && playerPrefab != null && player == null)
                {
                    Debug.Log("Spawning Player");
                    player = Instantiate(playerPrefab, roomPos, Quaternion.identity);
                    if (playerSpawnPoint != null) playerSpawnPoint.transform.position = player.transform.position;
                }
            }
        }

        // Debug: how many rooms were actually spawned in the scene
        Debug.Log($"Rooms spawned in scene: {roomGameObjects.Count} (JSON: {jsonRoomCount})");

        // Additional debug: total children under spawner and list them (helps identify unexpected objects)
        int totalChildren = roomsParent != null ? roomsParent.childCount :0;
        Debug.Log($"Total children under DungeonRooms: {totalChildren}");
        if (roomsParent != null)
        {
            for (int i =0; i < roomsParent.childCount; i++)
            {
                Debug.Log($"Child[{i}] = {roomsParent.GetChild(i).name}");
            }
        }

        //3) Spawn corridors & doors using original -> final mapping
        foreach (Connection connection in data.connections ?? new List<Connection>())
        {
            Vector2Int fromOriginal = new Vector2Int(connection.fromX, connection.fromY);
            Vector2Int toOriginal = new Vector2Int(connection.toX, connection.toY);

            if (!originalToFinal.ContainsKey(fromOriginal) || !originalToFinal.ContainsKey(toOriginal))
            {
                Debug.LogWarning($"Skipping connection from {fromOriginal} to {toOriginal} - mapping missing.");
                continue;
            }

            Vector2Int fromFinal = originalToFinal[fromOriginal];
            Vector2Int toFinal = originalToFinal[toOriginal];

            // Spawn corridor between final anchors (this will place corridor tiles along the grid)
            ArrangeRooms(fromFinal.x, fromFinal.y, toFinal.x, toFinal.y);

            // Spawn doors on appropriate room sides using the dominant direction between anchors
            TrySpawnDoorBetweenRooms(fromFinal, toFinal);
            TrySpawnDoorBetweenRooms(toFinal, fromFinal);
        }

        //4) Spawn global powerups
        foreach (Powerup powerup in data.powerups ?? new List<Powerup>())
        {
            Vector3 powerupPos = new Vector3(powerup.x * tileSize.x,0, powerup.y * tileSize.y);
            GameObject powerupPrefab = GetPowerupPrefab(powerup.type);
            if (powerupPrefab != null)
                Instantiate(powerupPrefab, powerupPos, Quaternion.identity);
        }

        //5) Log objectives
        foreach (string obj in data.objectives ?? new List<string>())
            Debug.Log("Objective: " + obj);
    }

    void ArrangeRooms(int fromX, int fromY, int toX, int toY)
    {
        Vector2Int from = new Vector2Int(fromX, fromY);
        Vector2Int to = new Vector2Int(toX, toY);
        Vector2Int current = from;

        // Horizontal path first
        while (current.x != to.x)
        {
            Vector2Int next = new Vector2Int(current.x + (to.x > current.x ? 1 : -1), current.y);
            current = next;
        }

        // Vertical path second
        while (current.y != to.y)
        {
            Vector2Int next = new Vector2Int(current.x, current.y + (to.y > current.y ? 1 : -1));
            current = next;
        }

        // DO NOT spawn doors here to avoid duplicates.
        // Door spawning is handled once in the outer loop where ArrangeRooms is called.
    }



    void TrySpawnDoorBetweenRooms(Vector2Int fromAnchor, Vector2Int toAnchor)
    {
        // If either room doesn't exist as a spawned GameObject, bail out
        if (!roomGameObjects.TryGetValue(fromAnchor, out GameObject fromRoom) ||
            !roomGameObjects.TryGetValue(toAnchor, out GameObject toRoom))
            return;

        Vector2Int delta = toAnchor - fromAnchor;
        // decide axis: use the axis with the larger absolute distance
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            // horizontal dominant
            if (delta.x >0)
            {
                SpawnDoor(fromRoom, "DoorRight", toAnchor);
                SpawnDoor(toRoom, "DoorLeft", fromAnchor);
            }
            else if (delta.x <0)
            {
                SpawnDoor(fromRoom, "DoorLeft", toAnchor);
                SpawnDoor(toRoom, "DoorRight", fromAnchor);
            }
        }
        else
        {
            // vertical dominant
            if (delta.y >0)
            {
                SpawnDoor(fromRoom, "DoorTop", toAnchor);
                SpawnDoor(toRoom, "DoorBottom", fromAnchor);
            }
            else if (delta.y <0)
            {
                SpawnDoor(fromRoom, "DoorBottom", toAnchor);
                SpawnDoor(toRoom, "DoorTop", fromAnchor);
            }
        }
    }

    // Replace the existing SpawnDoor method with this guarded version
    void SpawnDoor(GameObject roomObj, string doorPointName, Vector2Int targetRoomGridPos)
    {
        // compute direction early for duplicate checks
        string dir = doorPointName.Contains("Right") ? "Right"
                   : doorPointName.Contains("Left") ? "Left"
                   : doorPointName.Contains("Top") ? "Top"
                   : doorPointName.Contains("Bottom") ? "Bottom"
                   : null;

        // If a door already exists on this room targeting the same room or same direction, skip
        foreach (var existingPortal in roomObj.GetComponentsInChildren<DoorPortal>())
        {
            if (existingPortal == null) continue;
            if (existingPortal.targetRoomGridPosition == targetRoomGridPos)
            {
                Debug.Log($"SpawnDoor: skipping duplicate door in {roomObj.name} -> already targets {targetRoomGridPos}");
                return;
            }
            if (!string.IsNullOrEmpty(dir) && existingPortal.direction == dir)
            {
                Debug.Log($"SpawnDoor: skipping duplicate door in {roomObj.name} -> direction {dir} already has a door");
                return;
            }
        }

        Transform doorPoint = roomObj.transform.Find(doorPointName);
        if (doorPoint != null && doorPrefab != null)
        {
            GameObject door = Instantiate(doorPrefab, doorPoint.position, doorPoint.rotation, roomObj.transform);
            DoorPortal portal = door.AddComponent<DoorPortal>();
            portal.roomGridPosition = GetRoomGridPosition(roomObj);
            portal.targetRoomGridPosition = targetRoomGridPos;
            portal.direction = dir;
            Debug.Log($"SpawnDoor: placed door at named hook '{doorPointName}' for room {roomObj.name} at {doorPoint.position}");
            return;
        }

        // Fallback logic unchanged but with the duplicate guard already applied above
        if (doorPrefab == null)
        {
            Debug.LogWarning($"SpawnDoor: doorPrefab not assigned; cannot spawn door for {roomObj.name}.");
            return;
        }

        Vector2Int gridPos = GetRoomGridPosition(roomObj);
        Room roomData = null;
        roomDataByGrid.TryGetValue(gridPos, out roomData);

        Vector3 roomCenter = roomObj.transform.position;
        Vector3 offset = Vector3.zero;

        if (doorPointName.Contains("Right"))
        {
            float halfWidth = (roomData != null) ? (roomData.width * tileSize.x) / 2f : tileSize.x / 2f;
            offset = new Vector3(halfWidth, 0, 0);
        }
        else if (doorPointName.Contains("Left"))
        {
            float halfWidth = (roomData != null) ? (roomData.width * tileSize.x) / 2f : tileSize.x / 2f;
            offset = new Vector3(-halfWidth, 0, 0);
        }
        else if (doorPointName.Contains("Top"))
        {
            float halfHeight = (roomData != null) ? (roomData.height * tileSize.y) / 2f : tileSize.y / 2f;
            offset = new Vector3(0, halfHeight, 0);
        }
        else if (doorPointName.Contains("Bottom"))
        {
            float halfHeight = (roomData != null) ? (roomData.height * tileSize.y) / 2f : tileSize.y / 2f;
            offset = new Vector3(0, -halfHeight, 0);
        }

        Vector3 spawnPos = roomCenter + offset;
        GameObject fallbackDoor = Instantiate(doorPrefab, spawnPos, Quaternion.identity, roomObj.transform);
        DoorPortal fallbackPortal = fallbackDoor.AddComponent<DoorPortal>();
        fallbackPortal.roomGridPosition = gridPos;
        fallbackPortal.targetRoomGridPosition = targetRoomGridPos;
        fallbackPortal.direction = dir;

        Debug.LogWarning($"Spawned fallback door for {roomObj.name} at {spawnPos} (no named hook '{doorPointName}' found).");
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
        if (renderers.Length ==0)
        {
            Destroy(temp);
            return Vector2.one;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i =1; i < renderers.Length; i++)
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

        if (roomsParent != null) Destroy(roomsParent.gameObject);
        occupiedPositions.Clear();
        roomGameObjects.Clear();
        Random.InitState(System.DateTime.Now.Millisecond);
        int nextRoomCount = Random.Range(8,12);
        DungeonLoader.Instance.LoadNextDungeon();
        CreateDungeon();
        if (player != null)
        {
            Room spawnRoom = loader.GetDungeonData().rooms.Find(r => r.type == "spawn");
            if (spawnRoom != null)
            {
                Vector3 newPosition = new Vector3(spawnRoom.x * tileSize.x, spawnRoom.y * tileSize.y,0);
                player.transform.position = newPosition;
            }
        }
    }

    // Returns a mapping from original positions to final positions after resolving overlaps
    Dictionary<Vector2Int, Vector2Int> FixOverlappingRooms(DungeonData data)
    {
        // Work on a copy sorted by area (largest first) so big rooms get placed first.
        List<Room> roomsBySize = new List<Room>(data.rooms);
        roomsBySize.Sort((a, b) => (b.width * b.height).CompareTo(a.width * a.height));

        // capture original positions (from the freshly-loaded data) for mapping
        Dictionary<Room, Vector2Int> roomOriginal = new Dictionary<Room, Vector2Int>();
        foreach (var r in data.rooms)
            roomOriginal[r] = new Vector2Int(r.x, r.y);

        HashSet<Vector2Int> usedTiles = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> originalToFinal = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Room, Vector2Int> roomFinal = new Dictionary<Room, Vector2Int>();

        bool FootprintOverlaps(int anchorX, int anchorY, Room r)
        {
            for (int x = anchorX; x < anchorX + r.width; x++)
            for (int y = anchorY; y < anchorY + r.height; y++)
            if (usedTiles.Contains(new Vector2Int(x, y)))
                return true;
            return false;
        }

        foreach (Room room in roomsBySize)
        {
            Vector2Int original = roomOriginal[room];
            Vector2Int final = original;

            if (FootprintOverlaps(final.x, final.y, room))
            {
                int step =1;
                bool found = false;
                while (!found)
                {
                    // ring search: top/bottom rows then left/right columns
                    for (int dx = -step; dx <= step; dx++)
                    {
                        var candTop = new Vector2Int(original.x + dx, original.y - step);
                        if (!FootprintOverlaps(candTop.x, candTop.y, room)) { final = candTop; found = true; break; }

                        var candBottom = new Vector2Int(original.x + dx, original.y + step);
                        if (!FootprintOverlaps(candBottom.x, candBottom.y, room)) { final = candBottom; found = true; break; }
                    }
                    if (found) break;

                    for (int dy = -step +1; dy <= step -1; dy++)
                    {
                        var candLeft = new Vector2Int(original.x - step, original.y + dy);
                        if (!FootprintOverlaps(candLeft.x, candLeft.y, room)) { final = candLeft; found = true; break; }

                        var candRight = new Vector2Int(original.x + step, original.y + dy);
                        if (!FootprintOverlaps(candRight.x, candRight.y, room)) { final = candRight; found = true; break; }
                    }
                    step++;
                    if (step >500)
                    {
                        Debug.LogError("FixOverlappingRooms: couldn't find free spot for a room after500 steps. Forcing placement at original.");
                        final = original;
                        break;
                    }
                }
            }

            // record chosen final for this room (don't mutate data.rooms yet)
            roomFinal[room] = final;

            // mark tiles used by this room footprint (so subsequent rooms consider it)
            for (int x = final.x; x < final.x + room.width; x++)
            for (int y = final.y; y < final.y + room.height; y++)
                usedTiles.Add(new Vector2Int(x, y));

            // record mapping from original coords to final coords if not already present
            if (!originalToFinal.ContainsKey(original))
                originalToFinal[original] = final;
            else if (originalToFinal[original] != final)
                Debug.LogWarning($"Multiple rooms share original position {original}. First mapped to {originalToFinal[original]}, another mapped to {final}.");
        }

        // Apply final anchors to the actual data.rooms now that all finals are decided
        for (int i =0; i < data.rooms.Count; i++)
        {
            var r = data.rooms[i];
            if (roomFinal.ContainsKey(r))
            {
                var f = roomFinal[r];
                r.x = f.x;
                r.y = f.y;
            }
        }

        // debug log (optional)
        foreach (var kvp in originalToFinal)
        {
            Debug.Log($"Room original {kvp.Key} -> final {kvp.Value}");
        }

        return originalToFinal;


    }
}

