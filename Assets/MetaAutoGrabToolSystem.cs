using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MetaAutoGrabToolSystem : MonoBehaviour
{
    [Header("Tool Setup")]
    public GameObject[] dentalTools;
    public Transform toolSpawnParent;
    
    [Header("Visual Feedback")]
    public Material outlineMaterial;
    public Color outlineColor = Color.cyan;
    public bool enableHoverOutlines = true;
    public float outlineScale = 1.03f; // How much bigger the outline should be
    public bool useSmartScaling = true; // Automatically adjust outline for large objects
    public float maxOutlineThickness = 0.1f; // Maximum outline thickness in world units
    
    [Header("Audio")]
    public AudioClip toolGrabSound;
    public AudioClip toolReleaseSound;
    public AudioClip toolHoverSound;
    
    [Header("Controller Replacement")]
    public bool enableControllerReplacement = true;
    public bool showDebugControllerInfo = false;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showDistanceDebug = false;
    
    [Header("Detection Settings")]
    public float hoverDistance = 0.15f;
    
    private List<AutoGrabTool> allTools = new List<AutoGrabTool>();
    private AudioSource audioSource;
    
    // Controller replacement tracking
    private Dictionary<Transform, ControllerInfo> controllerInfos = new Dictionary<Transform, ControllerInfo>();
    
    [System.Serializable]
    public class ControllerInfo
    {
        public Transform controllerTransform;
        public List<GameObject> controllerModels = new List<GameObject>();
        public AutoGrabTool heldTool;
        public bool isHoldingTool;
        
        public ControllerInfo(Transform controller)
        {
            controllerTransform = controller;
            FindControllerModels();
        }
        
        public void FindControllerModels()
        {
            controllerModels.Clear();
            
            // Find all renderers in the controller hierarchy
            Renderer[] renderers = controllerTransform.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // Skip outline objects and other non-controller visuals
                if (!renderer.name.ToLower().Contains("outline"))
                {
                    controllerModels.Add(renderer.gameObject);
                }
            }
        }
        
        public void HideControllerModels()
        {
            foreach (GameObject model in controllerModels)
            {
                if (model != null)
                {
                    model.SetActive(false);
                }
            }
        }
        
        public void ShowControllerModels()
        {
            foreach (GameObject model in controllerModels)
            {
                if (model != null)
                {
                    model.SetActive(true);
                }
            }
        }
    }
    
    [System.Serializable]
    public class AutoGrabTool
    {
        public GameObject gameObject;
        public MetaBlocksTool toolComponent;
        public GameObject outlineObject;
        public bool isHovered;
        public Transform leftController;
        public Transform rightController;
        
        public AutoGrabTool(GameObject obj)
        {
            gameObject = obj;
            toolComponent = obj.GetComponent<MetaBlocksTool>();
        }
    }
    
    private void Start()
    {
        InitializeSystem();
    }
    
    private void InitializeSystem()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Create outline material
        if (outlineMaterial == null)
        {
            CreateOutlineMaterial();
        }
        
        // Initialize controller tracking
        InitializeControllerTracking();
        
        // Setup existing tools
        SetupExistingTools();
        
        // Spawn tool prefabs if assigned
        SpawnToolPrefabs();
        
        Debug.Log($"Meta Auto Grab Tool System initialized with {allTools.Count} tools");
    }
    
    private void InitializeControllerTracking()
    {
        if (!enableControllerReplacement) return;
        
        // Find and setup controller tracking
        Transform leftController = GetController("LeftHandAnchor");
        Transform rightController = GetController("RightHandAnchor");
        
        if (leftController != null)
        {
            ControllerInfo leftInfo = new ControllerInfo(leftController);
            controllerInfos[leftController] = leftInfo;
            
            if (showDebugControllerInfo)
            {
                Debug.Log($"Left controller setup: found {leftInfo.controllerModels.Count} visual components");
            }
        }
        
        if (rightController != null)
        {
            ControllerInfo rightInfo = new ControllerInfo(rightController);
            controllerInfos[rightController] = rightInfo;
            
            if (showDebugControllerInfo)
            {
                Debug.Log($"Right controller setup: found {rightInfo.controllerModels.Count} visual components");
            }
        }
        
        Debug.Log($"Controller replacement system initialized for {controllerInfos.Count} controllers");
    }
    
    private void CreateOutlineMaterial()
    {
        outlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        outlineMaterial.color = outlineColor;
        outlineMaterial.SetFloat("_Metallic", 0f);
        outlineMaterial.SetFloat("_Smoothness", 0.8f);
        outlineMaterial.EnableKeyword("_EMISSION");
        outlineMaterial.SetColor("_EmissionColor", outlineColor * 0.3f);
    }
    
    private void SetupExistingTools()
    {
        // Find tools by tag
        GameObject[] taggedTools = GameObject.FindGameObjectsWithTag("Tool");
        foreach (GameObject tool in taggedTools)
        {
            AddAutoGrabTool(tool);
        }
        
        // Find tools by name containing dental keywords
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> processedTools = new List<GameObject>(taggedTools);
        
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            if ((name.Contains("scalpel") || name.Contains("forceps") || 
                 name.Contains("drill") || name.Contains("explorer") || 
                 name.Contains("syringe") || name.Contains("mirror") ||
                 name.Contains("tool")) && !processedTools.Contains(obj))
            {
                AddAutoGrabTool(obj);
                processedTools.Add(obj);
            }
        }
    }
    
    private void SpawnToolPrefabs()
    {
        if (dentalTools == null || dentalTools.Length == 0) return;
        
        Vector3 spawnBase = toolSpawnParent != null ? toolSpawnParent.position : Vector3.zero;
        
        for (int i = 0; i < dentalTools.Length; i++)
        {
            if (dentalTools[i] == null) continue;
            
            Vector3 spawnPos = spawnBase + Vector3.right * (i * 0.2f);
            GameObject spawnedTool = Instantiate(dentalTools[i], spawnPos, Quaternion.identity);
            AddAutoGrabTool(spawnedTool);
        }
    }
    
    private void AddAutoGrabTool(GameObject toolObject)
    {
        // Add MetaBlocksTool component
        MetaBlocksTool metaTool = toolObject.GetComponent<MetaBlocksTool>();
        if (metaTool == null)
        {
            metaTool = toolObject.AddComponent<MetaBlocksTool>();
        }
        
        // Apply Meta's grab interaction automatically
        ApplyMetaGrabInteraction(toolObject);
        
        // Create our tool wrapper
        AutoGrabTool autoTool = new AutoGrabTool(toolObject);
        autoTool.toolComponent = metaTool;
        
        // Initialize the MetaBlocksTool
        metaTool.Initialize(this);
        
        // Create hover outline
        if (enableHoverOutlines)
        {
            CreateHoverOutline(autoTool);
        }
        
        allTools.Add(autoTool);
        
        Debug.Log($"Added auto-grab tool: {toolObject.name}");
    }
    
    private void ApplyMetaGrabInteraction(GameObject toolObject)
    {
#if UNITY_EDITOR
        // This is the magic - we're going to use the same method that Meta's "Add Grab Interaction" uses
        
        // First, ensure we're in a valid state to add components
        if (!Application.isPlaying)
        {
            // We can only do this in edit mode, not at runtime
            Debug.LogWarning("Meta Grab Interaction can only be added in Edit Mode. Please use the context menu option.");
            return;
        }
#endif
        
        // For runtime, we'll add the basic components that Meta's system creates
        AddBasicGrabComponents(toolObject);
    }
    
    private void AddBasicGrabComponents(GameObject toolObject)
    {
        // Add Rigidbody if not present
        Rigidbody rb = toolObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = toolObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // Add Collider if not present
        Collider col = toolObject.GetComponent<Collider>();
        if (col == null)
        {
            // Add appropriate collider
            Bounds bounds = GetObjectBounds(toolObject);
            if (bounds.size.magnitude > 0)
            {
                BoxCollider boxCol = toolObject.AddComponent<BoxCollider>();
                boxCol.size = bounds.size;
                boxCol.center = bounds.center - toolObject.transform.position;
            }
            else
            {
                BoxCollider boxCol = toolObject.AddComponent<BoxCollider>();
                boxCol.size = Vector3.one * 0.1f;
            }
        }
        
        Debug.Log($"Added basic grab components to {toolObject.name}");
    }
    
    private Bounds GetObjectBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        return bounds;
    }
    
    private void CreateHoverOutline(AutoGrabTool tool)
    {
        GameObject toolObject = tool.gameObject;
        
        // Create outline parent
        GameObject outlineParent = new GameObject($"{toolObject.name}_Outline");
        outlineParent.transform.SetParent(toolObject.transform);
        outlineParent.transform.localPosition = Vector3.zero;
        outlineParent.transform.localRotation = Quaternion.identity;
        outlineParent.transform.localScale = Vector3.one; // Don't scale the parent!
        
        // Calculate smart outline scale based on object size
        float smartOutlineScale = CalculateSmartOutlineScale(toolObject);
        
        // Copy mesh renderers for outline
        MeshRenderer[] renderers = toolObject.GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.transform.IsChildOf(outlineParent.transform)) continue;
            
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.mesh == null) continue;
            
            GameObject outlineCopy = new GameObject($"Outline_{renderer.name}");
            outlineCopy.transform.SetParent(outlineParent.transform);
            
            // Copy transform but use local coordinates to avoid scaling issues
            outlineCopy.transform.localPosition = toolObject.transform.InverseTransformPoint(renderer.transform.position);
            outlineCopy.transform.localRotation = Quaternion.Inverse(toolObject.transform.rotation) * renderer.transform.rotation;
            
            // Use smart scaling
            Vector3 finalScale = Vector3.Scale(renderer.transform.localScale, Vector3.one * smartOutlineScale);
            outlineCopy.transform.localScale = finalScale;
            
            // Add mesh components
            MeshFilter outlineMeshFilter = outlineCopy.AddComponent<MeshFilter>();
            outlineMeshFilter.mesh = meshFilter.mesh;
            
            MeshRenderer outlineRenderer = outlineCopy.AddComponent<MeshRenderer>();
            outlineRenderer.material = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            
            Debug.Log($"Created outline for {renderer.name}: smart scale {smartOutlineScale:F3}, final scale {finalScale}");
        }
        
        tool.outlineObject = outlineParent;
        outlineParent.SetActive(false);
    }
    
    private float CalculateSmartOutlineScale(GameObject toolObject)
    {
        if (!useSmartScaling)
        {
            return outlineScale; // Use regular scaling
        }
        
        // Get the object's world scale to understand its actual size
        Vector3 worldScale = toolObject.transform.lossyScale;
        float maxWorldScale = Mathf.Max(worldScale.x, worldScale.y, worldScale.z);
        
        // Calculate how much outline thickness this would create in world units
        float currentThickness = (outlineScale - 1.0f) * maxWorldScale;
        
        // If the thickness would be too large, reduce the outline scale
        if (currentThickness > maxOutlineThickness)
        {
            float adjustedScale = 1.0f + (maxOutlineThickness / maxWorldScale);
            Debug.Log($"Smart scaling for {toolObject.name}: original scale {maxWorldScale:F1}, " +
                     $"adjusted outline scale from {outlineScale:F3} to {adjustedScale:F3}");
            return adjustedScale;
        }
        
        // For very small objects, we might want slightly larger outlines
        if (maxWorldScale < 0.5f)
        {
            float boostedScale = outlineScale + 0.02f; // Add extra 2% for small objects
            Debug.Log($"Smart scaling for small object {toolObject.name}: boosted scale to {boostedScale:F3}");
            return boostedScale;
        }
        
        return outlineScale; // Use normal scaling for medium-sized objects
    }
    
    private void Update()
    {
        if (enableHoverOutlines)
        {
            UpdateHoverDetection();
        }
        
        if (enableControllerReplacement)
        {
            UpdateControllerReplacement();
        }
    }
    
    private void UpdateControllerReplacement()
    {
        // Check each controller for tool grab state changes
        foreach (var controllerPair in controllerInfos)
        {
            Transform controller = controllerPair.Key;
            ControllerInfo controllerInfo = controllerPair.Value;
            
            // Find if any tool is currently being held by this controller
            AutoGrabTool currentlyHeldTool = FindToolHeldByController(controller);
            
            // Check if the held tool state changed
            if (currentlyHeldTool != controllerInfo.heldTool)
            {
                // State changed - update controller visibility
                if (currentlyHeldTool != null)
                {
                    // Tool was grabbed
                    OnToolGrabbedByController(controller, currentlyHeldTool, controllerInfo);
                }
                else
                {
                    // Tool was released
                    OnToolReleasedByController(controller, controllerInfo);
                }
                
                controllerInfo.heldTool = currentlyHeldTool;
                controllerInfo.isHoldingTool = (currentlyHeldTool != null);
            }
        }
    }
    
    private AutoGrabTool FindToolHeldByController(Transform controller)
    {
        foreach (AutoGrabTool tool in allTools)
        {
            if (tool.gameObject != null && IsToolBeingHeldByController(tool, controller))
            {
                return tool;
            }
        }
        return null;
    }
    
    private bool IsToolBeingHeldByController(AutoGrabTool tool, Transform controller)
    {
        // Check if the tool is parented to this controller (Meta's grab system often does this)
        Transform currentParent = tool.gameObject.transform.parent;
        while (currentParent != null)
        {
            if (currentParent == controller)
            {
                return true;
            }
            currentParent = currentParent.parent;
        }
        
        // Alternative check: see if tool is very close to controller (within grab range)
        float distance = Vector3.Distance(tool.gameObject.transform.position, controller.position);
        if (distance < 0.2f) // Very close = likely grabbed
        {
            // Additional check: see if tool has Meta's grabbed components in active state
            if (HasActiveGrabbedComponents(tool.gameObject))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private bool HasActiveGrabbedComponents(GameObject tool)
    {
        // Check for common Meta grab components that indicate the tool is being held
        Component[] components = tool.GetComponents<Component>();
        foreach (Component comp in components)
        {
            string typeName = comp.GetType().Name;
            if (typeName.Contains("Grabbable") || typeName.Contains("GrabInteractable"))
            {
                // Tool has grabbable components - assume it can detect its own grabbed state
                // This is a simplified check; Meta's system handles the actual grab detection
                return true;
            }
        }
        return false;
    }
    
    private void OnToolGrabbedByController(Transform controller, AutoGrabTool tool, ControllerInfo controllerInfo)
    {
        // Hide controller models
        controllerInfo.HideControllerModels();
        
        if (showDebugControllerInfo)
        {
            Debug.Log($"Tool {tool.gameObject.name} grabbed by controller {controller.name} - hiding controller models");
        }
        
        // Play grab sound
        if (toolGrabSound != null)
        {
            audioSource.PlayOneShot(toolGrabSound);
        }
    }
    
    private void OnToolReleasedByController(Transform controller, ControllerInfo controllerInfo)
    {
        // Show controller models
        controllerInfo.ShowControllerModels();
        
        if (showDebugControllerInfo)
        {
            Debug.Log($"Tool released by controller {controller.name} - showing controller models");
        }
        
        // Play release sound
        if (toolReleaseSound != null)
        {
            audioSource.PlayOneShot(toolReleaseSound);
        }
    }
    
    private void UpdateHoverDetection()
    {
        // Find controllers
        Transform leftController = GetController("LeftHandAnchor");
        Transform rightController = GetController("RightHandAnchor");
        
        if (enableDebugLogs)
        {
            Debug.Log($"Update: Left Controller: {leftController != null}, Right Controller: {rightController != null}");
        }
        
        foreach (AutoGrabTool tool in allTools)
        {
            bool wasHovered = tool.isHovered;
            bool isNowHovered = false;
            float closestDistance = float.MaxValue;
            
            // Check hover distance
            if (leftController != null)
            {
                float leftDistance = Vector3.Distance(leftController.position, tool.gameObject.transform.position);
                closestDistance = Mathf.Min(closestDistance, leftDistance);
                if (leftDistance <= hoverDistance) isNowHovered = true;
                
                if (showDistanceDebug && leftDistance < 1f) // Only show when close
                {
                    Debug.Log($"Left to {tool.gameObject.name}: {leftDistance:F3}m (threshold: {hoverDistance:F3}m)");
                }
            }
            
            if (rightController != null)
            {
                float rightDistance = Vector3.Distance(rightController.position, tool.gameObject.transform.position);
                closestDistance = Mathf.Min(closestDistance, rightDistance);
                if (rightDistance <= hoverDistance) isNowHovered = true;
                
                if (showDistanceDebug && rightDistance < 1f) // Only show when close
                {
                    Debug.Log($"Right to {tool.gameObject.name}: {rightDistance:F3}m (threshold: {hoverDistance:F3}m)");
                }
            }
            
            // Update hover state
            if (isNowHovered != wasHovered)
            {
                tool.isHovered = isNowHovered;
                
                if (tool.outlineObject != null)
                {
                    tool.outlineObject.SetActive(isNowHovered);
                }
                
                if (isNowHovered)
                {
                    Debug.Log($"*** HOVERING OVER: {tool.gameObject.name} (closest distance: {closestDistance:F3}m) ***");
                    if (toolHoverSound != null)
                    {
                        audioSource.PlayOneShot(toolHoverSound, 0.3f);
                        Debug.Log("Playing hover sound");
                    }
                    else
                    {
                        Debug.Log("No hover sound assigned");
                    }
                }
                else
                {
                    Debug.Log($"Stopped hovering over: {tool.gameObject.name}");
                }
            }
        }
    }
    
    private Transform GetController(string preferredName)
    {
        // Try multiple possible VR rig names (Meta Building Blocks vs traditional OVR)
        string[] rigNames = {"OVRCameraRig", "XR Origin", "XROrigin", "CameraRig", "Meta XR Origin"};
        
        GameObject cameraRig = null;
        
        // Find the actual camera rig
        foreach (string rigName in rigNames)
        {
            cameraRig = GameObject.Find(rigName);
            if (cameraRig != null)
            {
                Debug.Log($"Found VR rig: {rigName}");
                break;
            }
        }
        
        if (cameraRig == null)
        {
            // Last resort: find any object with "camera", "rig", "origin", or "xr" in name
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                string name = obj.name.ToLower();
                if ((name.Contains("camera") && name.Contains("rig")) || 
                    (name.Contains("xr") && name.Contains("origin")) ||
                    name.Contains("camerarig"))
                {
                    cameraRig = obj;
                    Debug.Log($"Found VR rig (fallback): {obj.name}");
                    break;
                }
            }
        }
        
        if (cameraRig == null)
        {
            return null;
        }
        
        // Try different tracking space names
        Transform trackingSpace = null;
        string[] trackingSpaceNames = {"TrackingSpace", "Tracking Space", "CameraOffset"};
        
        foreach (string spaceName in trackingSpaceNames)
        {
            trackingSpace = cameraRig.transform.Find(spaceName);
            if (trackingSpace != null)
            {
                break;
            }
        }
        
        // If no tracking space, search directly in camera rig
        if (trackingSpace == null)
        {
            trackingSpace = cameraRig.transform;
        }
        
        // Try multiple possible controller names
        string[] leftControllerNames = {"LeftHandAnchor", "LeftControllerAnchor", "Left Controller", "LeftHand", "Left Hand"};
        string[] rightControllerNames = {"RightHandAnchor", "RightControllerAnchor", "Right Controller", "RightHand", "Right Hand"};
        
        // Determine which controller we're looking for
        string[] namesToTry = preferredName.Contains("Left") ? leftControllerNames : rightControllerNames;
        
        // Try each possible name
        foreach (string name in namesToTry)
        {
            Transform controller = FindChildRecursive(trackingSpace, name);
            if (controller != null)
            {
                Debug.Log($"Found controller: {controller.name} at {controller.position}");
                return controller;
            }
        }
        
        // Fallback: look for any object with "left" or "right" in the name
        Transform fallbackController = FindControllerByKeyword(trackingSpace, preferredName.Contains("Left") ? "left" : "right");
        if (fallbackController != null)
        {
            Debug.Log($"Found controller (fallback): {fallbackController.name}");
            return fallbackController;
        }
        
        return null;
    }
    
    // Helper method to search recursively through children
    private Transform FindChildRecursive(Transform parent, string name)
    {
        // Check direct children first
        Transform found = parent.Find(name);
        if (found != null) return found;
        
        // Search recursively through all children
        for (int i = 0; i < parent.childCount; i++)
        {
            found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null) return found;
        }
        
        return null;
    }
    
    // Helper method to find controller by keyword
    private Transform FindControllerByKeyword(Transform parent, string keyword)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.ToLower().Contains(keyword))
            {
                // Additional check: make sure it's likely a controller/hand
                if (child.name.ToLower().Contains("hand") || 
                    child.name.ToLower().Contains("controller") ||
                    child.name.ToLower().Contains("anchor"))
                {
                    return child;
                }
            }
            
            // Search recursively
            Transform found = FindControllerByKeyword(child, keyword);
            if (found != null) return found;
        }
        
        return null;
    }
    
    public Material GetOutlineMaterial()
    {
        return outlineMaterial;
    }
    
    // Public method to register new tools (like extracted teeth)
    public void RegisterTool(GameObject toolObject)
    {
        if (toolObject == null) return;
        
        // Check if already registered
        foreach (AutoGrabTool existingTool in allTools)
        {
            if (existingTool.gameObject == toolObject)
            {
                Debug.Log($"Tool {toolObject.name} already registered");
                return;
            }
        }
        
        // Add the tool using existing logic
        AddAutoGrabTool(toolObject);
        
        Debug.Log($"Registered new tool: {toolObject.name}");
    }
    
    // Context menu options for easy setup
    [ContextMenu("Setup Common Dental Tools")]
    public void SetupCommonDentalTools()
    {
        SetupExistingTools();
        Debug.Log($"Setup complete. Found {allTools.Count} tools.");
    }
    
    [ContextMenu("Test Outline Materials")]
    public void TestOutlineMaterials()
    {
        Debug.Log("=== Testing Outline Materials ===");
        foreach (AutoGrabTool tool in allTools)
        {
            if (tool.outlineObject != null)
            {
                tool.outlineObject.SetActive(true);
                Debug.Log($"Enabled outline for {tool.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"No outline object found for {tool.gameObject.name}");
            }
        }
    }
    
    [ContextMenu("Hide All Outlines")]
    public void HideAllOutlines()
    {
        foreach (AutoGrabTool tool in allTools)
        {
            if (tool.outlineObject != null)
            {
                tool.outlineObject.SetActive(false);
            }
        }
    }
    
    [ContextMenu("Debug Tool Info")]
    public void DebugToolInfo()
    {
        Debug.Log("=== Tool Debug Info ===");
        foreach (AutoGrabTool tool in allTools)
        {
            Debug.Log($"Tool: {tool.gameObject.name}");
            Debug.Log($"  - Has outline: {tool.outlineObject != null}");
            Debug.Log($"  - MeshRenderers: {tool.gameObject.GetComponentsInChildren<MeshRenderer>().Length}");
            
            if (tool.outlineObject != null)
            {
                Debug.Log($"  - Outline children: {tool.outlineObject.transform.childCount}");
            }
        }
    }
    
    [ContextMenu("Debug Outline Scales")]
    public void DebugOutlineScales()
    {
        Debug.Log("=== Outline Scale Debug ===");
        foreach (AutoGrabTool tool in allTools)
        {
            if (tool.outlineObject != null)
            {
                Debug.Log($"Tool: {tool.gameObject.name}");
                Debug.Log($"  Tool Scale: {tool.gameObject.transform.localScale}");
                Debug.Log($"  Tool World Scale: {tool.gameObject.transform.lossyScale}");
                Debug.Log($"  Outline Parent Scale: {tool.outlineObject.transform.localScale}");
                
                // Check each outline child
                for (int i = 0; i < tool.outlineObject.transform.childCount; i++)
                {
                    Transform child = tool.outlineObject.transform.GetChild(i);
                    Debug.Log($"    Outline Child [{i}] {child.name}: local scale {child.localScale}");
                }
            }
        }
    }
    
    [ContextMenu("Debug Controller Models")]
    public void DebugControllerModels()
    {
        Debug.Log("=== Controller Models Debug ===");
        
        foreach (var controllerPair in controllerInfos)
        {
            Transform controller = controllerPair.Key;
            ControllerInfo info = controllerPair.Value;
            
            Debug.Log($"Controller: {controller.name}");
            Debug.Log($"  - Holding tool: {info.isHoldingTool}");
            Debug.Log($"  - Held tool: {(info.heldTool?.gameObject.name ?? "None")}");
            Debug.Log($"  - Model count: {info.controllerModels.Count}");
            
            for (int i = 0; i < info.controllerModels.Count; i++)
            {
                GameObject model = info.controllerModels[i];
                if (model != null)
                {
                    Debug.Log($"    [{i}] {model.name} - Active: {model.activeInHierarchy}");
                }
            }
        }
    }
    
    [ContextMenu("Force Show All Controllers")]
    public void ForceShowAllControllers()
    {
        Debug.Log("Forcing all controller models to show");
        foreach (var controllerPair in controllerInfos)
        {
            controllerPair.Value.ShowControllerModels();
        }
    }
    
    [ContextMenu("Force Hide All Controllers")]
    public void ForceHideAllControllers()
    {
        Debug.Log("Forcing all controller models to hide");
        foreach (var controllerPair in controllerInfos)
        {
            controllerPair.Value.HideControllerModels();
        }
    }
    
    [ContextMenu("Test Controller Replacement")]
    public void TestControllerReplacement()
    {
        Debug.Log("=== Testing Controller Replacement ===");
        
        if (!enableControllerReplacement)
        {
            Debug.LogWarning("Controller replacement is disabled!");
            return;
        }
        
        // Find controllers
        Transform leftController = GetController("LeftHandAnchor");
        Transform rightController = GetController("RightHandAnchor");
        
        Debug.Log($"Left controller found: {leftController != null}");
        Debug.Log($"Right controller found: {rightController != null}");
        
        foreach (var controllerPair in controllerInfos)
        {
            ControllerInfo info = controllerPair.Value;
            Debug.Log($"Controller {controllerPair.Key.name} has {info.controllerModels.Count} visual models");
        }
        
        // Test hide/show cycle
        Debug.Log("Testing hide/show cycle...");
        ForceHideAllControllers();
        
        // Show them again after 2 seconds
        Invoke(nameof(ForceShowAllControllers), 2f);
    }
    
    [ContextMenu("Find All VR Objects")]
    public void FindAllVRObjects()
    {
        Debug.Log("=== Searching for ALL VR-related objects ===");
        
        // Find all GameObjects in scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        Debug.Log("Looking for Camera Rigs:");
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            if (name.Contains("camera") || name.Contains("rig") || name.Contains("ovr") || 
                name.Contains("xr") || name.Contains("origin"))
            {
                Debug.Log($"Found potential rig: {obj.name} at {obj.transform.position}");
                LogHierarchy(obj.transform, 0, 3); // Show 3 levels deep
            }
        }
        
        Debug.Log("Looking for Controller/Hand objects:");
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            if (name.Contains("controller") || name.Contains("hand") || name.Contains("anchor") ||
                name.Contains("left") || name.Contains("right"))
            {
                Debug.Log($"Found potential controller: {obj.name} at {obj.transform.position}");
            }
        }
        
        Debug.Log("Looking for Tracking/Touch objects:");
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            if (name.Contains("tracking") || name.Contains("touch") || name.Contains("space"))
            {
                Debug.Log($"Found tracking object: {obj.name} at {obj.transform.position}");
            }
        }
    }
    
    private void LogHierarchy(Transform parent, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;
        
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}├─ {parent.name}");
        
        for (int i = 0; i < parent.childCount; i++)
        {
            LogHierarchy(parent.GetChild(i), depth + 1, maxDepth);
        }
    }
    
    [ContextMenu("Debug TrackingSpace Children")]
    public void DebugTrackingSpaceChildren()
    {
        Debug.Log("=== VR Rig Debug (Meta Building Blocks Compatible) ===");
        
        // Try to find any VR rig
        string[] rigNames = {"OVRCameraRig", "XR Origin", "XROrigin", "CameraRig", "Meta XR Origin"};
        GameObject cameraRig = null;
        
        foreach (string rigName in rigNames)
        {
            cameraRig = GameObject.Find(rigName);
            if (cameraRig != null)
            {
                Debug.Log($"Found VR rig: {rigName}");
                break;
            }
        }
        
        if (cameraRig == null)
        {
            Debug.Log("Standard VR rigs not found, searching for any camera-related objects...");
            
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                string name = obj.name.ToLower();
                if ((name.Contains("camera") && name.Contains("rig")) || 
                    (name.Contains("xr") && name.Contains("origin")) ||
                    name.Contains("camerarig") || name.Contains("camera"))
                {
                    Debug.Log($"Found potential VR rig: {obj.name} at {obj.transform.position}");
                    if (cameraRig == null) cameraRig = obj; // Use first one found
                }
            }
        }
        
        if (cameraRig != null)
        {
            Debug.Log($"Using VR rig: {cameraRig.name}");
            Debug.Log("VR Rig hierarchy:");
            LogHierarchy(cameraRig.transform, 0, 4); // Show 4 levels deep
            
            // Look for controller-like objects specifically
            Debug.Log("Looking for controller/hand objects in rig:");
            Transform[] allChildren = cameraRig.GetComponentsInChildren<Transform>();
            
            foreach (Transform child in allChildren)
            {
                string name = child.name.ToLower();
                if (name.Contains("hand") || name.Contains("controller") || name.Contains("anchor"))
                {
                    Debug.Log($"  Found controller/hand: {child.name} at {child.position}");
                }
            }
        }
        else
        {
            Debug.LogError("No VR rig found! Make sure you have a Meta Building Blocks camera rig in your scene.");
        }
    }
    
    [ContextMenu("Debug Controller Detection")]
    public void DebugControllerDetection()
    {
        Debug.Log("=== Controller Detection Debug ===");
        
        // Find controllers
        Transform leftController = GetController("LeftHandAnchor");
        Transform rightController = GetController("RightHandAnchor");
        
        Debug.Log($"Left Controller Found: {leftController != null}");
        if (leftController != null)
        {
            Debug.Log($"Left Controller Position: {leftController.position}");
        }
        
        Debug.Log($"Right Controller Found: {rightController != null}");
        if (rightController != null)
        {
            Debug.Log($"Right Controller Position: {rightController.position}");
        }
        
        // Check camera rig
        GameObject cameraRig = GameObject.Find("OVRCameraRig");
        Debug.Log($"OVRCameraRig Found: {cameraRig != null}");
        
        if (cameraRig != null)
        {
            Transform trackingSpace = cameraRig.transform.Find("TrackingSpace");
            Debug.Log($"TrackingSpace Found: {trackingSpace != null}");
            
            if (trackingSpace != null)
            {
                Debug.Log($"TrackingSpace Children:");
                for (int i = 0; i < trackingSpace.childCount; i++)
                {
                    Transform child = trackingSpace.GetChild(i);
                    Debug.Log($"  - {child.name}");
                }
            }
        }
        
        // Test distances to first tool
        if (allTools.Count > 0)
        {
            GameObject firstTool = allTools[0].gameObject;
            Debug.Log($"Testing distances to first tool: {firstTool.name}");
            Debug.Log($"Tool Position: {firstTool.transform.position}");
            
            if (leftController != null)
            {
                float leftDist = Vector3.Distance(leftController.position, firstTool.transform.position);
                Debug.Log($"Left Controller Distance: {leftDist:F3}m (hover threshold: {hoverDistance:F3}m)");
            }
            
            if (rightController != null)
            {
                float rightDist = Vector3.Distance(rightController.position, firstTool.transform.position);
                Debug.Log($"Right Controller Distance: {rightDist:F3}m (hover threshold: {hoverDistance:F3}m)");
            }
        }
    }
    
    [ContextMenu("Apply Meta Grab to All Tools")]
    public void ApplyMetaGrabToAllTools()
    {
        // This will apply the grab interaction to all found tools
        // But you'll need to manually apply "Add Grab Interaction" to get the full Meta setup
        
        SetupExistingTools();
        
        Debug.Log("Applied basic setup to all tools.");
        Debug.Log("For full Meta interaction, please:");
        Debug.Log("1. Select each tool object");
        Debug.Log("2. Right-click -> Add Grab Interaction");
        Debug.Log("3. This will add the complete Meta interaction components");
    }
    
#if UNITY_EDITOR
    [ContextMenu("Apply Meta Grab Interaction to Selected")]
    public void ApplyMetaGrabToSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        foreach (GameObject obj in selectedObjects)
        {
            // Try to use Meta's menu command if available
            // This simulates right-click -> Add Grab Interaction
            AddAutoGrabTool(obj);
            
            Debug.Log($"Applied grab interaction setup to: {obj.name}");
        }
    }
#endif
}