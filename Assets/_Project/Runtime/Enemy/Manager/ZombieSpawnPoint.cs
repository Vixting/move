using UnityEngine;

public class ZombieSpawnPoint : MonoBehaviour
{
    [SerializeField] private int minZombies = 1;
    [SerializeField] private int maxZombies = 3;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool respawnZombies = false;
    [SerializeField] private float respawnTime = 120f;
    [SerializeField] private GameObject[] customZombiePrefabs;

    public int MinZombies => minZombies;
    public int MaxZombies => maxZombies;
    public float SpawnRadius => spawnRadius;
    public bool SpawnOnStart => spawnOnStart;
    public bool RespawnZombies => respawnZombies;
    public float RespawnTime => respawnTime;
    public GameObject[] CustomZombiePrefabs => customZombiePrefabs;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}