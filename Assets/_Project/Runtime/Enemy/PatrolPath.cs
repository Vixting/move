using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private float pointSize = 0.5f;
    
    public Transform[] GetPatrolPoints()
    {
        return patrolPoints;
    }
    
    public int GetPointCount()
    {
        return patrolPoints != null ? patrolPoints.Length : 0;
    }
    
    public Transform GetPointAt(int index)
    {
        if (patrolPoints == null || index < 0 || index >= patrolPoints.Length)
        {
            return null;
        }
        
        return patrolPoints[index];
    }
    
    public Vector3 GetPositionAt(int index)
    {
        Transform point = GetPointAt(index);
        return point != null ? point.position : transform.position;
    }
    
    public void AssignToEnemy(EnemyAI enemy)
    {
        if (enemy == null || patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }
        
        // Use reflection to assign patrol points to the enemy
        var patrolPointsField = typeof(EnemyAI).GetField("patrolPoints", 
                            System.Reflection.BindingFlags.NonPublic | 
                            System.Reflection.BindingFlags.Instance);
        
        if (patrolPointsField != null)
        {
            patrolPointsField.SetValue(enemy, patrolPoints);
        }
        else
        {
            Debug.LogWarning("Could not assign patrol points to enemy - field not found");
        }
    }
    
    public void AssignToEnemies()
    {
        // Find all enemies within a certain radius and assign this patrol path
        EnemyAI[] nearbyEnemies = FindObjectsOfType<EnemyAI>();
        
        foreach (var enemy in nearbyEnemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < 10f) // Only assign to enemies within 10 units
            {
                AssignToEnemy(enemy);
            }
        }
    }
    
    private void OnValidate()
    {
        // Create patrol points if none exist
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            patrolPoints = new Transform[4];
            
            for (int i = 0; i < 4; i++)
            {
                GameObject point = new GameObject($"PatrolPoint_{i+1}");
                point.transform.SetParent(transform);
                
                // Position in a square around the patrol path
                float angle = i * 90f * Mathf.Deg2Rad;
                float radius = 5f;
                Vector3 position = transform.position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                
                point.transform.position = position;
                patrolPoints[i] = point.transform;
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!drawGizmos || patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }
        
        Gizmos.color = gizmoColor;
        
        // Draw points
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            
            Gizmos.DrawSphere(patrolPoints[i].position, pointSize);
            
            // Draw line to next point
            if (i < patrolPoints.Length - 1 && patrolPoints[i+1] != null)
            {
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i+1].position);
            }
        }
        
        // Draw line from last to first point to complete the loop
        if (patrolPoints.Length > 1 && patrolPoints[0] != null && patrolPoints[patrolPoints.Length-1] != null)
        {
            Gizmos.DrawLine(patrolPoints[patrolPoints.Length-1].position, patrolPoints[0].position);
        }
    }
}