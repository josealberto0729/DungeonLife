using System.Collections.Generic;
using UnityEngine;

public class ProceduralDungeonGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int roomCount = 10;
    public Vector2Int roomSizeMin = new Vector2Int(2, 2);
    public Vector2Int roomSizeMax = new Vector2Int(4, 4);
    public int dungeonWidth = 20;
    public int dungeonHeight = 20;

    public DungeonLoader loader;

    void Awake()
    {
        DungeonData generatedData = GenerateDungeon();
        loader.SetDungeonData(generatedData); // Pass to DungeonSpawner
    }

    DungeonData GenerateDungeon()
    {
        DungeonData dungeon = new DungeonData();
        dungeon.rooms = new List<Room>();
        dungeon.connections = new List<Connection>();

        HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();
        Vector2Int currentPos = Vector2Int.zero;

        for (int i = 0; i < roomCount; i++)
        {
            Room room = new Room();
            room.x = currentPos.x;
            room.y = currentPos.y;
            room.width = Random.Range(roomSizeMin.x, roomSizeMax.x + 1);
            room.height = Random.Range(roomSizeMin.y, roomSizeMax.y + 1);
            room.type = GetRoomType(i);
            room.enemies = GenerateRandomEnemies();
            room.powerups = new List<Powerup>();
            room.treasures = new List<Treasure>();

            dungeon.rooms.Add(room);
            usedPositions.Add(currentPos);

            // Pick new direction
            Vector2Int dir = GetRandomDirection();
            Vector2Int nextPos = currentPos + dir;

            // Create connection
            if (i > 0)
            {
                Connection conn = new Connection
                {
                    fromX = currentPos.x,
                    fromY = currentPos.y,
                    toX = nextPos.x,
                    toY = nextPos.y
                };
                dungeon.connections.Add(conn);
            }

            currentPos = nextPos;
        }

        return dungeon;
    }

    string GetRoomType(int index)
    {
        if (index == 0) return "spawn";
        if (index == roomCount - 1) return "boss";
        if (Random.value < 0.2f) return "treasure";
        return "normal";
    }

    Vector2Int GetRandomDirection()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };
        return directions[Random.Range(0, directions.Length)];
    }

    List<Enemy> GenerateRandomEnemies()
    {
        List<Enemy> enemies = new List<Enemy>();

        int count = Random.Range(1, 3); // Spawn 1–2 enemies
        for (int i = 0; i < count; i++)
        {
            Enemy enemy = new Enemy();
            enemy.type = (Random.value > 0.5f) ? "melee" : "ranged";
            enemy.patrolRadius = Random.Range(1, 4);
            enemies.Add(enemy);
        }

        return enemies;
    }
}
