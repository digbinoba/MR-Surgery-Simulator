using UnityEngine;
using Meta.XR.MRUtilityKit;

public class MRUKDebugTablePlacer : MonoBehaviour
{
    [Header("Surgical Table Setup")]
    public GameObject surgicalTablePrefab;
    
    [Header("Fallback Placement")]
    public float fallbackDistance = 2f;
    public Vector3 fallbackOffset = new Vector3(0, -1f, 0);
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool showDebugSphere = true;
    
    [Header("Table Orientation")]
    public bool alignToFloorPlane = true; 
    public bool facePlayerWhenAligned = true; 
    public Vector3 tableRotationOffset = Vector3.zero; 
    public bool debugTableOrientation = true;
    
    [Header("Model Orientation Fix")]
    public Vector3 modelUpDirection = Vector3.up; // What direction is "up" for your model?
    public Vector3 modelForwardDirection = Vector3.forward; // What direction is "forward" for your model?
    
    private Transform playerHead;
    private GameObject placedTable;
    private bool hasPlacedTable = false;
    private GameObject debugSphere;
    
    private void Start()
    {
        // Try multiple ways to find player head
        FindPlayerHead();
        
        // Check MRUK immediately
        CheckMRUKStatus();
        
        // Keep checking MRUK status
        InvokeRepeating(nameof(CheckMRUKStatus), 1f, 2f);
    }
    
    private void FindPlayerHead()
    {
        LogDebug("=== Finding Player Head ===");
        
        // Method 1: Standard OVR hierarchy
        GameObject cameraRig = GameObject.Find("OVRCameraRig");
        if (cameraRig != null)
        {
            LogDebug($"Found OVRCameraRig: {cameraRig.name}");
            
            // Try different possible paths
            string[] possiblePaths = {
                "TrackingSpace/CenterEyeAnchor",
                "CenterEyeAnchor", 
                "TrackingSpace/MainCamera",
                "MainCamera"
            };
            
            foreach (string path in possiblePaths)
            {
                Transform found = cameraRig.transform.Find(path);
                if (found != null)
                {
                    playerHead = found;
                    LogDebug($"Player head found via path: {path}");
                    return;
                }
                else
                {
                    LogDebug($"Path not found: {path}");
                }
            }
        }
        else
        {
            LogDebug("OVRCameraRig not found, trying alternatives");
        }
        
        // Method 2: Look for XR Origin
        GameObject xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin != null)
        {
            LogDebug($"Found XR Origin: {xrOrigin.name}");
            Camera cam = xrOrigin.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerHead = cam.transform;
                LogDebug($"Using XR Origin camera: {cam.name}");
                return;
            }
        }
        
        // Method 3: Find main camera directly
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerHead = mainCam.transform;
            LogDebug($"Using main camera: {mainCam.name}");
            return;
        }
        
        // Method 4: Find any camera
        Camera[] allCameras = FindObjectsOfType<Camera>();
        LogDebug($"Found {allCameras.Length} cameras in scene");
        
        foreach (Camera cam in allCameras)
        {
            LogDebug($"Camera found: {cam.name} at {cam.transform.position}");
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                playerHead = cam.transform;
                LogDebug($"Using active camera: {cam.name}");
                return;
            }
        }
        
        LogDebug("ERROR: Could not find player head/camera!");
        
        // Method 5: Debug hierarchy
        DebugCameraHierarchy();
    }
    
    private void DebugCameraHierarchy()
    {
        LogDebug("=== Debugging Scene Hierarchy ===");
        
        // Find all root objects
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
        foreach (GameObject root in rootObjects)
        {
            if (root.name.ToLower().Contains("camera") || 
                root.name.ToLower().Contains("ovr") || 
                root.name.ToLower().Contains("xr") ||
                root.name.ToLower().Contains("origin"))
            {
                LogDebug($"Potential camera root: {root.name}");
                LogDebugHierarchy(root.transform, 0, 3); // Show 3 levels deep
            }
        }
    }
    
    private void LogDebugHierarchy(Transform parent, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        
        string indent = new string(' ', depth * 2);
        LogDebug($"{indent}├─ {parent.name} (Active: {parent.gameObject.activeInHierarchy})");
        
        // Check if this has a camera
        Camera cam = parent.GetComponent<Camera>();
        if (cam != null)
        {
            LogDebug($"{indent}   └─ HAS CAMERA (enabled: {cam.enabled})");
        }
        
        for (int i = 0; i < parent.childCount; i++)
        {
            LogDebugHierarchy(parent.GetChild(i), depth + 1, maxDepth);
        }
    }
    
    private void CheckMRUKStatus()
    {
        LogDebug("=== MRUK Status Check ===");
        
        // Check if MRUK exists
        if (MRUK.Instance == null)
        {
            LogDebug("MRUK.Instance is NULL!");
            return;
        }
        
        LogDebug("MRUK.Instance found");
        
        // Check current room
        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null)
        {
            LogDebug("MRUK.GetCurrentRoom() returned NULL");
            
            // Try to find room manually
            MRUKRoom[] allRooms = FindObjectsOfType<MRUKRoom>();
            LogDebug($"Found {allRooms.Length} MRUKRoom objects in scene");
            
            if (allRooms.Length > 0)
            {
                currentRoom = allRooms[0];
                LogDebug($"Using first room found: {currentRoom.name}");
            }
        }
        else
        {
            LogDebug($"Current room found: {currentRoom.name}");
        }
        
        if (currentRoom != null)
        {
            LogDebug($"Room has floor anchor: {currentRoom.FloorAnchor != null}");
            LogDebug($"Room has {currentRoom.WallAnchors.Count} wall anchors");
            
            if (currentRoom.FloorAnchor != null)
            {
                Vector3 floorPos = currentRoom.FloorAnchor.transform.position;
                LogDebug($"Floor position: {floorPos}");
                
                // Show debug sphere at floor position
                if (showDebugSphere)
                {
                    ShowDebugSphere(floorPos, Color.green);
                }
            }
        }
    }
    
    private void ShowDebugSphere(Vector3 position, Color color)
    {
        if (debugSphere != null)
        {
            Destroy(debugSphere);
        }
        
        debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.transform.position = position;
        debugSphere.transform.localScale = Vector3.one * 0.2f;
        debugSphere.GetComponent<Renderer>().material.color = color;
        debugSphere.name = "MRUK_Debug_Sphere";
        
        // Remove collider so it doesn't interfere
        Destroy(debugSphere.GetComponent<Collider>());
        
        LogDebug($"Debug sphere placed at {position}");
    }
    
    public void AttemptTablePlacement()
    {
        LogDebug("=== Attempting Table Placement ===");
        
        if (hasPlacedTable)
        {
            LogDebug("Table already placed");
            return;
        }
        
        if (surgicalTablePrefab == null)
        {
            LogDebug("ERROR: No surgical table prefab assigned!");
            return;
        }
        
        if (playerHead == null)
        {
            LogDebug("ERROR: Player head not found!");
            return;
        }
        
        // Try MRUK placement first
        bool success = TryMRUKPlacement();
        
        // Fall back to simple placement if MRUK fails
        if (!success)
        {
            LogDebug("MRUK placement failed, using fallback");
            TryFallbackPlacement();
        }
    }
    
    private bool TryMRUKPlacement()
    {
        if (MRUK.Instance == null)
        {
            LogDebug("MRUK.Instance is null, cannot place table");
            return false;
        }
        
        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null)
        {
            // Try to find room manually
            MRUKRoom[] allRooms = FindObjectsOfType<MRUKRoom>();
            if (allRooms.Length > 0)
            {
                currentRoom = allRooms[0];
                LogDebug($"Using manually found room: {currentRoom.name}");
            }
            else
            {
                LogDebug("No MRUK rooms found");
                return false;
            }
        }
        
        if (currentRoom.FloorAnchor == null)
        {
            LogDebug("No floor anchor in current room");
            return false;
        }
        
        // Calculate position in front of player, projected onto floor
        Vector3 playerPos = playerHead.position;
        Vector3 forwardDir = playerHead.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();
        
        Vector3 targetPos = playerPos + forwardDir * 1.5f;
        
        // Project onto floor plane
        Vector3 floorPos = currentRoom.FloorAnchor.transform.position;
        Vector3 floorNormal = currentRoom.FloorAnchor.transform.up;
        
        Vector3 toTarget = targetPos - floorPos;
        float distanceToFloor = Vector3.Dot(toTarget, floorNormal);
        Vector3 finalPos = targetPos - floorNormal * distanceToFloor;
        
        // Adjust slightly above floor
        finalPos += floorNormal * 0.02f; // Use floor normal instead of just up
        
        LogDebug($"Placing table at MRUK position: {finalPos}");
        PlaceTableAtPosition(finalPos, currentRoom.FloorAnchor.transform, floorNormal);
        
        return true;
    }
    
    private void TryFallbackPlacement()
    {
        // Simple placement in front of player
        Vector3 playerPos = playerHead.position;
        Vector3 forwardDir = playerHead.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();
        
        Vector3 tablePos = playerPos + forwardDir * fallbackDistance + fallbackOffset;
        
        LogDebug($"Placing table at fallback position: {tablePos}");
        PlaceTableAtPosition(tablePos, null, Vector3.up); // Use default up vector for fallback
    }
    
    private void PlaceTableAtPosition(Vector3 position, Transform parent, Vector3 surfaceNormal)
    {
        Quaternion rotation = Quaternion.identity;
        
        if (alignToFloorPlane)
        {
            // Step 1: Create the desired orientation (aligned to floor, facing player)
            Vector3 desiredUp = surfaceNormal;
            Vector3 desiredForward = Vector3.forward; // Default forward
            
            if (facePlayerWhenAligned && playerHead != null)
            {
                // Get direction to player projected onto the floor plane
                Vector3 directionToPlayer = (playerHead.position - position);
                Vector3 projectedDirection = directionToPlayer - Vector3.Project(directionToPlayer, surfaceNormal);
                
                if (projectedDirection.magnitude > 0.1f)
                {
                    desiredForward = projectedDirection.normalized;
                }
            }
            
            // Create orthogonal coordinate system
            Vector3 desiredRight = Vector3.Cross(desiredUp, desiredForward).normalized;
            desiredForward = Vector3.Cross(desiredRight, desiredUp).normalized;
            
            // Step 2: Account for model's actual up/forward directions
            // Create rotation that aligns model's up to desired up, and model's forward to desired forward
            Quaternion modelToWorld = Quaternion.LookRotation(modelForwardDirection, modelUpDirection);
            Quaternion worldToDesired = Quaternion.LookRotation(desiredForward, desiredUp);
            
            // Combined rotation: first undo model's orientation, then apply desired orientation
            rotation = worldToDesired * Quaternion.Inverse(modelToWorld);
            
            LogDebug($"Model up: {modelUpDirection}, Model forward: {modelForwardDirection}");
            LogDebug($"Desired up: {desiredUp}, Desired forward: {desiredForward}");
        }
        else
        {
            // Original behavior - just face player with world up
            Vector3 directionToPlayer = (playerHead.position - position);
            directionToPlayer.y = 0;
            
            if (directionToPlayer.magnitude > 0.1f)
            {
                rotation = Quaternion.LookRotation(directionToPlayer.normalized);
            }
        }
        
        // Apply rotation offset for fine-tuning
        if (tableRotationOffset != Vector3.zero)
        {
            Quaternion offsetRotation = Quaternion.Euler(tableRotationOffset);
            rotation = rotation * offsetRotation;
        }
        
        // Instantiate table
        placedTable = Instantiate(surgicalTablePrefab, position, rotation);
        
        // Parent to floor anchor if available
        if (parent != null)
        {
            placedTable.transform.SetParent(parent);
        }
        
        hasPlacedTable = true;
        LogDebug($"Table successfully placed at {position} with rotation {rotation.eulerAngles}");
        LogDebug($"Surface normal: {surfaceNormal}, Aligned to plane: {alignToFloorPlane}");
        
        // Debug table orientation
        if (debugTableOrientation)
        {
            ShowTableOrientationDebug(surfaceNormal);
        }
        
        // Show debug sphere at table position
        if (showDebugSphere)
        {
            ShowDebugSphere(position, Color.blue);
        }
    }
    
    private void ShowTableOrientationDebug(Vector3 surfaceNormal)
    {
        if (placedTable == null) return;
        
        Vector3 tablePos = placedTable.transform.position;
        Vector3 tableForward = placedTable.transform.forward;
        Vector3 tableUp = placedTable.transform.up;
        Vector3 tableRight = placedTable.transform.right;
        
        // Draw orientation lines
        Debug.DrawRay(tablePos, tableForward * 0.5f, Color.blue, 10f);    // Forward = Blue
        Debug.DrawRay(tablePos, tableUp * 0.3f, Color.green, 10f);        // Up = Green  
        Debug.DrawRay(tablePos, tableRight * 0.3f, Color.red, 10f);       // Right = Red
        Debug.DrawRay(tablePos, surfaceNormal * 0.4f, Color.yellow, 10f); // Surface normal = Yellow
        
        LogDebug($"Table orientation - Forward: {tableForward}, Up: {tableUp}, Right: {tableRight}");
        LogDebug($"Surface normal: {surfaceNormal}, Angle difference: {Vector3.Angle(tableUp, surfaceNormal):F1}°");
    }
    
    public void RemoveTable()
    {
        if (placedTable != null)
        {
            Destroy(placedTable);
            hasPlacedTable = false;
            LogDebug("Table removed");
        }
        
        if (debugSphere != null)
        {
            Destroy(debugSphere);
        }
    }
    
    private void Update()
    {
        // A button - place table
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            LogDebug("A button pressed - attempting placement");
            AttemptTablePlacement();
        }
        
        // B button - remove table
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            LogDebug("B button pressed - removing table");
            RemoveTable();
        }
        
        // X button - debug MRUK status
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            LogDebug("X button pressed - checking MRUK status");
            CheckMRUKStatus();
        }
        
        // Y button - force fallback placement
        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            LogDebug("Y button pressed - forcing fallback placement");
            if (!hasPlacedTable)
            {
                TryFallbackPlacement();
            }
        }
        
        // Menu button - try to find player head again
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            LogDebug("Menu button pressed - trying to find player head again");
            FindPlayerHead();
        }
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MRUKDebug] {message}");
        }
    }
    
    // Manual debug methods
    [ContextMenu("Check MRUK Status")]
    private void ManualMRUKCheck()
    {
        CheckMRUKStatus();
    }
    
    [ContextMenu("Force Table Placement")]
    private void ManualTablePlacement()
    {
        AttemptTablePlacement();
    }
    
    [ContextMenu("List All MRUK Objects")]
    private void ListAllMRUKObjects()
    {
        LogDebug("=== All MRUK Objects in Scene ===");
        
        MRUKRoom[] rooms = FindObjectsOfType<MRUKRoom>();
        LogDebug($"MRUKRoom objects: {rooms.Length}");
        
        MRUKAnchor[] anchors = FindObjectsOfType<MRUKAnchor>();
        LogDebug($"MRUKAnchor objects: {anchors.Length}");
        
        foreach (var anchor in anchors)
        {
            LogDebug($"Anchor: {anchor.name}, Position: {anchor.transform.position}");
        }
    }
}