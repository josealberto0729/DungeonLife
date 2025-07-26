using UnityEngine;

public class PlayerTeleportDistanceFlag : MonoBehaviour
{
    private Vector3 lastTeleportPosition = Vector3.positiveInfinity;
    private float requiredDistance = 0.5f;

    public bool CanTeleport()
    {
        return Vector3.Distance(transform.position, lastTeleportPosition) > requiredDistance;
    }

    public void SetLastTeleportPosition(Vector3 newPos)
    {
        lastTeleportPosition = newPos;
    }
}
