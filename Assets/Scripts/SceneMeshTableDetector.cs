using UnityEngine;
using System.Collections;

public class SceneMeshTableDetector : MonoBehaviour
{
    [Header("Surgical Table Setup")]
    public GameObject surgicalTablePrefab;
    
    [Header("Detection Settings")]
    public float tableHeightMin = 0.6f; // 60cm minimum
    public float tableHeightMax = 1.2f; // 120cm maximum  
    public float minSurfaceArea = 0.5f; // Minimum surface area
    public LayerMask sceneMeshLayer = -1;
    
    [Header("Placement")]
    public bool autoDetectOnStart = true;
    public float detectionDelay = 3f; // Wait for scene mesh to load
    
    private bool hasPlacedTable = false;
    private Transform playerHead;
    
    private void Start()
    {
        // Get player head reference
        GameObject cameraRig = GameObject.Find("OVRCameraRig");
        if (cameraRig != null)
        {
            playerHead = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
        }
        
        if (autoDetectOnStart)
        {
            StartCoroutine(DelayedTableDetection());
        }
    }
    
    private IEnumerator DelayedTableDetection()
    {
        yield return new WaitForSeconds(detectionDelay);
        DetectAndPlaceTable();
    }
    
    public void DetectAndPlaceTable()
    {
        if (hasPlacedTable || playerHead == null) return;
        
        // Find all potential table surfaces using raycasting
        Vector3 bestTablePosition = Vector3.zero;
        bool foundSuitableTable = false;
        float bestScore = 0f;
        
        // Cast rays in multiple directions around the player
        int rayCount = 16;
        float maxDistance = 5f;
        
        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i / (float)rayCount) * 360f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), -0.5f, Mathf.Sin(angle)).normalized;
            
            Ray ray = new Ray(playerHead.position, direction);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, maxDistance, sceneMeshLayer))
            {
                // Check if this could be a table surface
                if (IsValidTableSurface(hit))
                {
                    float score = ScoreTablePosition(hit.point);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTablePosition = hit.point;
                        foundSuitableTable = true;
                    }
                }
            }
        }
        
        if (foundSuitableTable)
        {
            PlaceTableAtPosition(bestTablePosition);
        }
        else
        {
            Debug.Log("No suitable table surface found. Retrying in 5 seconds...");
            Invoke(nameof(DetectAndPlaceTable), 5f);
        }
    }
    
    private bool IsValidTableSurface(RaycastHit hit)
    {
        // Check surface height relative to player
        float playerHeight = playerHead.position.y;
        float surfaceHeight = hit.point.y;
        float relativeHeight = playerHeight - surfaceHeight;
        
        // Must be within table height range
        if (relativeHeight < tableHeightMin || relativeHeight > tableHeightMax)
            return false;
        
        // Check if surface is roughly horizontal (normal pointing up)
        if (Vector3.Dot(hit.normal, Vector3.up) < 0.7f)
            return false;
        
        // Test surface area by casting additional rays
        return TestSurfaceArea(hit.point, hit.normal);
    }
    
    private bool TestSurfaceArea(Vector3 center, Vector3 normal)
    {
        int validHits = 0;
        int totalTests = 12;
        float testRadius = 0.4f; // Test area around hit point
        
        for (int i = 0; i < totalTests; i++)
        {
            float angle = (i / (float)totalTests) * 360f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * testRadius;
            Vector3 testPoint = center + offset + Vector3.up * 0.1f;
            
            RaycastHit testHit;
            if (Physics.Raycast(testPoint, Vector3.down, out testHit, 0.3f, sceneMeshLayer))
            {
                // Check if hit point is close to original surface
                if (Vector3.Distance(testHit.point, center) <= testRadius * 1.2f)
                {
                    validHits++;
                }
            }
        }
        
        float surfaceRatio = validHits / (float)totalTests;
        return surfaceRatio >= 0.6f; // 60% of test points should hit surface
    }
    
    private float ScoreTablePosition(Vector3 position)
    {
        float score = 0f;
        
        // Prefer surfaces in front of player
        Vector3 toSurface = (position - playerHead.position).normalized;
        float forwardDot = Vector3.Dot(playerHead.forward, toSurface);
        score += forwardDot * 50f; // Up to 50 points for being in front
        
        // Prefer closer surfaces (but not too close)
        float distance = Vector3.Distance(playerHead.position, position);
        if (distance > 1f && distance < 3f)
            score += (3f - distance) * 20f; // Up to 40 points for good distance
        
        // Prefer surfaces at comfortable height
        float relativeHeight = playerHead.position.y - position.y;
        float idealHeight = 0.8f; // 80cm
        float heightScore = 30f - Mathf.Abs(relativeHeight - idealHeight) * 30f;
        score += Mathf.Max(0f, heightScore);
        
        return score;
    }
    
    private void PlaceTableAtPosition(Vector3 position)
    {
        if (surgicalTablePrefab == null)
        {
            Debug.LogError("No surgical table prefab assigned!");
            return;
        }
        
        // Adjust position slightly above surface
        Vector3 spawnPosition = position + Vector3.up * 0.02f;
        
        // Face the player
        Vector3 directionToPlayer = (playerHead.position - spawnPosition);
        directionToPlayer.y = 0; // Keep horizontal
        Quaternion rotation = Quaternion.LookRotation(directionToPlayer.normalized);
        
        GameObject surgicalTable = Instantiate(surgicalTablePrefab, spawnPosition, rotation);
        hasPlacedTable = true;
        
        Debug.Log($"Surgical table placed at {spawnPosition}");
    }
    
    // Manual controls for testing
    private void Update()
    {
        // Manual trigger with controller button (for testing)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) && !hasPlacedTable)
        {
            DetectAndPlaceTable();
        }
        
        // Reset with secondary trigger
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            ResetTable();
        }
    }
    
    public void ResetTable()
    {
        GameObject existingTable = GameObject.FindWithTag("SurgicalTable");
        if (existingTable != null)
        {
            Destroy(existingTable);
        }
        hasPlacedTable = false;
    }
}