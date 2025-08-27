using UnityEngine;

public class PlayerTeleportDistanceFlag : MonoBehaviour
{
    private Vector3 lastTeleportPosition = Vector3.positiveInfinity;
    private float requiredDistance = 0.5f;
    private float lastTeleportTime = -1f;
    [SerializeField] private float teleportCooldown = 1f; // 0.2 seconds

    public bool CanTeleport()
    {
        return Time.time - lastTeleportTime > teleportCooldown;
    }

    public void SetLastTeleportPosition(Vector3 newPos)
    {
        lastTeleportTime = Time.time;
    }

}
