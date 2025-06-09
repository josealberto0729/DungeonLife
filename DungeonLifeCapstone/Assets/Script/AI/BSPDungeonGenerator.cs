using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class BSPDungeonGenerator : MonoBehaviour
{
    [Header("Generation Options")]
    public bool generateJson = true;  

    [Header("Dungeon Settings")]
    public int dungeonWidth = 64;
    public int dungeonHeight = 64;
    public int minRoomSize = 6;
    public int maxDepth = 5;

    [Header("Prefabs")]
    public GameObject roomPrefab;
    public GameObject corridorPrefab;

    private BSPNode root;
    private List<nRoom> rooms = new List<nRoom>();
    private List<Corridor> corridors = new List<Corridor>();

    void Start()
    {
        // Clear previous
        rooms.Clear();
        corridors.Clear();

        root = new BSPNode(0, 0, dungeonWidth, dungeonHeight, 0, maxDepth, minRoomSize);
        root.Split();

        root.GenerateRooms(rooms);

        ConnectRooms(root);

        DrawDebug();

        if (generateJson)
        {
            SaveDungeonJson();
        }

        InstantiateDungeon();
        CheckRoomOverlaps();
    }

    // Connect sibling rooms by corridors recursively
    void ConnectRooms(BSPNode node)
    {
        if (node == null) return;

        if (node.left != null && node.right != null)
        {
            // Connect rooms from left and right subtrees
            nRoom leftRoom = node.left.GetRoom();
            nRoom rightRoom = node.right.GetRoom();

            if (leftRoom != null && rightRoom != null)
            {
                CreateCorridor(leftRoom, rightRoom);
            }

            ConnectRooms(node.left);
            ConnectRooms(node.right);
        }
    }
    Vector2Int GetExitPoint(nRoom from, nRoom to)
    {
        Vector2Int centerFrom = from.GetCenter();
        Vector2Int centerTo = to.GetCenter();

        int x = Mathf.Clamp(centerTo.x, from.x, from.x + from.width - 1);
        int y = Mathf.Clamp(centerTo.y, from.y, from.y + from.height - 1);

        // If centerTo is outside from in X, clamp to X edge. If inside, keep its X value
        // Same for Y

        // Now move to the edge instead of possible inside
        // Which wall is closer: left/right or top/bottom
        int dx = centerTo.x - centerFrom.x;
        int dy = centerTo.y - centerFrom.y;

        if (Mathf.Abs(dx) > Mathf.Abs(dy))
            x = dx > 0 ? from.x + from.width - 1 : from.x; // right or left edge
        else
            y = dy > 0 ? from.y + from.height - 1 : from.y; // top or bottom edge

        return new Vector2Int(x, y);
    }

    void CreateCorridor(nRoom a, nRoom b)
    {
        Vector2Int pointA = GetExitPoint(a, b);
        Vector2Int pointB = GetExitPoint(b, a);

        // Rest of code as before:
        int cornerX = pointB.x;
        int cornerY = pointA.y;

        // Horizontal
        corridors.Add(new Corridor(
            new Vector2Int(Mathf.Min(pointA.x, cornerX), cornerY),
            new Vector2Int(Mathf.Abs(pointA.x - cornerX) + 1, 1)));

        // Vertical
        corridors.Add(new Corridor(
            new Vector2Int(cornerX, Mathf.Min(pointA.y, pointB.y)),
            new Vector2Int(1, Mathf.Abs(pointA.y - pointB.y) + 1))
        );
    }

    void DrawDebug()
    {
        // Draw rooms in green
        foreach (var room in rooms)
        {
            Vector3 bl = new Vector3(room.x, 0, room.y);
            Vector3 br = new Vector3(room.x + room.width, 0, room.y);
            Vector3 tl = new Vector3(room.x, 0, room.y + room.height);
            Vector3 tr = new Vector3(room.x + room.width, 0, room.y + room.height);

            Debug.DrawLine(bl, br, Color.green, 100f);
            Debug.DrawLine(br, tr, Color.green, 100f);
            Debug.DrawLine(tr, tl, Color.green, 100f);
            Debug.DrawLine(tl, bl, Color.green, 100f);
        }

        // Draw corridors in red
        foreach (var cor in corridors)
        {
            Vector3 bl = new Vector3(cor.x, 0, cor.y);
            Vector3 br = new Vector3(cor.x + cor.width, 0, cor.y);
            Vector3 tl = new Vector3(cor.x, 0, cor.y + cor.height);
            Vector3 tr = new Vector3(cor.x + cor.width, 0, cor.y + cor.height);

            Debug.DrawLine(bl, br, Color.red, 100f);
            Debug.DrawLine(br, tr, Color.red, 100f);
            Debug.DrawLine(tr, tl, Color.red, 100f);
            Debug.DrawLine(tl, bl, Color.red, 100f);
        }
    }

    void SaveDungeonJson()
    {
        nDungeonData data = new nDungeonData
        {
            rooms = new List<nRoomData>(),
            corridors = new List<nRoomData>()
        };

        foreach (var r in rooms)
            data.rooms.Add(new nRoomData(r.x, r.y, r.width, r.height));
        foreach (var c in corridors)
            data.corridors.Add(new nRoomData(c.x, c.y, c.width, c.height));

        string json = JsonUtility.ToJson(data, true);

#if UNITY_EDITOR
        // Create Resources/Dungeons if it doesn't exist
        string resourcesDir = Path.Combine(Application.dataPath, "Resources/Dungeons");
        if (!Directory.Exists(resourcesDir))
            Directory.CreateDirectory(resourcesDir);
        // Use unique file names to avoid overwriting
        string fileName = "dungeon_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
        string path = Path.Combine(resourcesDir, fileName);

        File.WriteAllText(path, json);
        Debug.Log($"Dungeon saved to {path}");

        UnityEditor.AssetDatabase.Refresh(); // Make file appear in Project window
#endif
    }

    void InstantiateDungeon()
    {
        // Instantiate rooms
        foreach (var room in rooms)
        {
            Vector3 pos = new Vector3(room.x + room.width / 2f, 0, room.y + room.height / 2f);
            Instantiate(roomPrefab, pos, Quaternion.identity, transform);
        }

        // Instantiate corridors
        foreach (var cor in corridors)
        {
            Vector3 pos = new Vector3(cor.x + cor.width / 2f, 0, cor.y + cor.height / 2f);
            Instantiate(corridorPrefab, pos, Quaternion.identity, transform);
        }
    }
    void CheckRoomOverlaps()
    {
        for (int i = 0; i < rooms.Count; i++)
            for (int j = i + 1; j < rooms.Count; j++)
                if (AABBOverlap(rooms[i], rooms[j]))
                    Debug.LogWarning($"Room {i} overlaps room {j}!");
    }
    bool AABBOverlap(nRoom a, nRoom b)
    {
        return a.x < b.x + b.width && a.x + a.width > b.x &&
               a.y < b.y + b.height && a.y + a.height > b.y;
    }
}


#region Data Classes

[System.Serializable]
public class BSPNode
{
    public int x, y, width, height, depth;
    public BSPNode left, right;

    private int maxDepth, minRoomSize;
    private nRoom room;

    public BSPNode(int x, int y, int width, int height, int depth, int maxDepth, int minRoomSize)
    {
        this.x = x; this.y = y; this.width = width; this.height = height;
        this.depth = depth; this.maxDepth = maxDepth; this.minRoomSize = minRoomSize;
    }

    public void Split()
    {
        if (depth >= maxDepth || (width < minRoomSize * 2 && height < minRoomSize * 2))
            return;

        bool splitHorizontally = (width < height) ? true : false;
        if (width > height && width / height >= 1.25f) splitHorizontally = false;
        if (height > width && height / width >= 1.25f) splitHorizontally = true;

        int max = (splitHorizontally ? height : width) - minRoomSize;
        if (max <= minRoomSize) return;

        int split = Random.Range(minRoomSize, max);

        if (splitHorizontally)
        {
            left = new BSPNode(x, y, width, split, depth + 1, maxDepth, minRoomSize);
            right = new BSPNode(x, y + split, width, height - split, depth + 1, maxDepth, minRoomSize);
        }
        else
        {
            left = new BSPNode(x, y, split, height, depth + 1, maxDepth, minRoomSize);
            right = new BSPNode(x + split, y, width - split, height, depth + 1, maxDepth, minRoomSize);
        }

        left.Split();
        right.Split();
    }

    public void GenerateRooms(List<nRoom> rooms)
    {
        if (left != null || right != null)
        {
            if (left != null) left.GenerateRooms(rooms);
            if (right != null) right.GenerateRooms(rooms);

            // room is null because this node is split
            room = null;
        }
        else
        {
            int maxRoomWidth = Mathf.Max(minRoomSize, width - 2);
            int maxRoomHeight = Mathf.Max(minRoomSize, height - 2);

            if (maxRoomWidth >= minRoomSize && maxRoomHeight >= minRoomSize)
            {
                int roomWidth = Random.Range(minRoomSize, maxRoomWidth + 1);
                int roomHeight = Random.Range(minRoomSize, maxRoomHeight + 1);

                int roomX = x + Random.Range(1, width - roomWidth);
                int roomY = y + Random.Range(1, height - roomHeight);

                room = new nRoom(roomX, roomY, roomWidth, roomHeight);
                rooms.Add(room);
            }
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (AABBOverlap(rooms[i], rooms[j]))
                    {
                        Debug.LogWarning($"Overlap between room {i} and room {j}");
                    }
                }
            }

            // Helper
            bool AABBOverlap(nRoom a, nRoom b)
            {
                return a.x < b.x + b.width && a.x + a.width > b.x &&
                       a.y < b.y + b.height && a.y + a.height > b.y;
            }
        }
    }

    public nRoom GetRoom()
    {
        if (room != null)
            return room;

        nRoom leftRoom = null;
        nRoom rightRoom = null;

        if (left != null)
            leftRoom = left.GetRoom();
        if (right != null)
            rightRoom = right.GetRoom();

        if (leftRoom == null) return rightRoom;
        if (rightRoom == null) return leftRoom;

        // Randomly choose one if both exist
        return Random.value > 0.5f ? leftRoom : rightRoom;
    }
}

[System.Serializable]
public class nRoom
{
    public int x, y, width, height;

    public nRoom(int x, int y, int width, int height)
    {
        this.x = x; this.y = y; this.width = width; this.height = height;
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int(x + width / 2, y + height / 2);
    }
}

[System.Serializable]
public class Corridor
{
    public int x, y, width, height;

    public Corridor(Vector2Int pos, Vector2Int size)
    {
        x = pos.x; y = pos.y; width = size.x; height = size.y;
    }
}

// Data container for JSON serialization
[System.Serializable]
public class nDungeonData
{
    public List<nRoomData> rooms;
    public List<nRoomData> corridors;
}

[System.Serializable]
public class nRoomData
{
    public int x, y, width, height;

    public nRoomData(int x, int y, int width, int height)
    {
        this.x = x; this.y = y; this.width = width; this.height = height;
    }
}

#endregion
