using UnityEngine;

public class DoorPortal : MonoBehaviour
{
    public Vector2Int roomGridPosition;
    public string direction;
    public Vector2Int targetRoomGridPosition;
    private Transform player;
    private float minDistance = 1f;

    private void Start()
    {
        player = DungeonSpawner.Instance.playerPrefab.transform;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player collided to the door");
            var teleportFlag = other.GetComponent<PlayerTeleportDistanceFlag>();
            Debug.Log("player can teleport "+teleportFlag.CanTeleport());
            if (teleportFlag != null && teleportFlag.CanTeleport())
            {
                Vector2Int targetRoomGridPos = targetRoomGridPosition;
                if (DungeonSpawner.Instance.roomGameObjects.TryGetValue(targetRoomGridPos, out GameObject targetRoom))
                {
                    string targetDoorName = GetOppositeDoorName(direction);
                    Transform targetDoorPoint = targetRoom.transform.Find(targetDoorName);

                    if (targetDoorPoint != null)
                    {
                        other.transform.position = targetDoorPoint.position;
                        teleportFlag.SetLastTeleportPosition(targetDoorPoint.position);
                        Debug.Log($"Teleported player to room at {targetRoomGridPos}, door: {targetDoorName}");
                    }
                    else
                    {
                        Debug.LogWarning("Target door point not found in destination room.");
                    }
                }
            }
        }
    }

    Vector2Int GetConnectedRoomPosition()
    {
        return direction switch
        {
            "Right" => roomGridPosition + Vector2Int.right,
            "Left" => roomGridPosition + Vector2Int.left,
            "Top" => roomGridPosition + Vector2Int.up,
            "Bottom" => roomGridPosition + Vector2Int.down,
            _ => roomGridPosition
        };
    }

    string GetOppositeDoorName(string dir)
    {
        return dir switch
        {
            "Right" => "DoorLeft",
            "Left" => "DoorRight",
            "Top" => "DoorBottom",
            "Bottom" => "DoorTop",
            _ => ""
        };
    }
}
