using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(transform.position, 1f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}