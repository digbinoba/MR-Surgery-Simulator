using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ToothExtractionSystem : MonoBehaviour
{
    [Header("Tooth Setup")]
    public GameObject[] availableTeeth; // Assign your separated teeth here
    public Transform patientHead; // Reference to patient head
    
    [Header("Procedure Settings")]
    public bool startProcedureOnBegin = true;
    public float extractionDistance = 0.15f; // How close forceps need to be (increased from 0.05f)
    public float extractionTime = 2f; // How long to hold forceps on tooth
    public bool requireTriggerPress = true; // Whether user needs to press trigger to extract
    
    [Header("Visual Feedback")]
    public Material targetToothMaterial; // Highlight material for target tooth
    public Material extractedToothMaterial; // Material for extracted tooth
    public Color targetHighlightColor = Color.red;
    public bool enablePulseAnimation = true;
    public float pulseSpeed = 2f;
    
    [Header("Audio")]
    public AudioClip toothSelectedSound;
    public AudioClip extractionStartSound;
    public AudioClip extractionCompleteSound;
    public AudioClip successSound;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private GameObject targetTooth;
    private GameObject targetToothHighlight;
    private Material originalToothMaterial;
    private bool procedureStarted = false;
    private bool extractionInProgress = false;
    private float extractionTimer = 0f;
    private AudioSource audioSource;
    private MetaAutoGrabToolSystem toolSystem;
    
    // Procedure states
    public enum ExtractionState
    {
        WaitingToStart,
        ToothHighlighted,
        ForcepsNearTooth,
        ExtractionInProgress,
        ToothExtracted,
        ProcedureComplete
    }
    
    public ExtractionState currentState = ExtractionState.WaitingToStart;
    
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
        
        // Find tool system
        toolSystem = FindObjectOfType<MetaAutoGrabToolSystem>();
        
        // Create target material if not assigned
        if (targetToothMaterial == null)
        {
            CreateTargetToothMaterial();
        }
        
        // Find teeth if not assigned
        if (availableTeeth == null || availableTeeth.Length == 0)
        {
            FindAvailableTeeth();
        }
        
        if (startProcedureOnBegin)
        {
            StartProcedure();
        }
        
        Debug.Log($"Tooth Extraction System initialized with {availableTeeth.Length} teeth");
    }
    
    private void CreateTargetToothMaterial()
    {
        targetToothMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        targetToothMaterial.color = targetHighlightColor;
        targetToothMaterial.SetFloat("_Metallic", 0f);
        targetToothMaterial.SetFloat("_Smoothness", 0.3f);
        targetToothMaterial.EnableKeyword("_EMISSION");
        targetToothMaterial.SetColor("_EmissionColor", targetHighlightColor * 0.5f);
    }
    
    private void FindAvailableTeeth()
    {
        List<GameObject> foundTeeth = new List<GameObject>();
        
        // Look for objects tagged as "Tooth"
        GameObject[] taggedTeeth = GameObject.FindGameObjectsWithTag("Tooth");
        foundTeeth.AddRange(taggedTeeth);
        
        // Look for objects with "tooth" in the name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("tooth") && !foundTeeth.Contains(obj))
            {
                foundTeeth.Add(obj);
            }
        }
        
        availableTeeth = foundTeeth.ToArray();
        
        if (showDebugInfo)
        {
            Debug.Log($"Found {availableTeeth.Length} teeth for extraction");
            foreach (GameObject tooth in availableTeeth)
            {
                Debug.Log($"  - {tooth.name}");
            }
        }
    }
    
    public void StartProcedure()
    {
        if (availableTeeth == null || availableTeeth.Length == 0)
        {
            Debug.LogError("No teeth available for extraction!");
            return;
        }
        
        SelectRandomTargetTooth();
        procedureStarted = true;
        currentState = ExtractionState.ToothHighlighted;
        
        if (showDebugInfo)
        {
            Debug.Log($"Tooth extraction procedure started. Target: {targetTooth.name}");
        }
    }
    
    private void SelectRandomTargetTooth()
    {
        // Select a random tooth from available teeth
        int randomIndex = Random.Range(0, availableTeeth.Length);
        targetTooth = availableTeeth[randomIndex];
        
        // Store original material
        Renderer toothRenderer = targetTooth.GetComponent<Renderer>();
        if (toothRenderer != null)
        {
            originalToothMaterial = toothRenderer.material;
        }
        
        // Create highlight effect
        CreateToothHighlight();
        
        // Play selection sound
        if (toothSelectedSound != null)
        {
            audioSource.PlayOneShot(toothSelectedSound);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Selected tooth for extraction: {targetTooth.name}");
        }
    }
    
    private void CreateToothHighlight()
    {
        if (targetTooth == null) return;
        
        // Create highlight parent object
        targetToothHighlight = new GameObject($"{targetTooth.name}_Highlight");
        targetToothHighlight.transform.SetParent(targetTooth.transform);
        targetToothHighlight.transform.localPosition = Vector3.zero;
        targetToothHighlight.transform.localRotation = Quaternion.identity;
        targetToothHighlight.transform.localScale = Vector3.one;
        
        // Find all mesh renderers in the tooth hierarchy (including children)
        MeshRenderer[] allRenderers = targetTooth.GetComponentsInChildren<MeshRenderer>();
        
        if (showDebugInfo)
        {
            Debug.Log($"Found {allRenderers.Length} mesh renderers in tooth {targetTooth.name}");
        }
        
        // Create highlight copy for each mesh renderer
        foreach (MeshRenderer originalRenderer in allRenderers)
        {
            MeshFilter originalMeshFilter = originalRenderer.GetComponent<MeshFilter>();
            
            if (originalMeshFilter != null && originalMeshFilter.mesh != null)
            {
                // Create highlight copy
                GameObject highlightCopy = new GameObject($"Highlight_{originalRenderer.name}");
                highlightCopy.transform.SetParent(targetToothHighlight.transform);
                
                // Match the original transform exactly
                highlightCopy.transform.position = originalRenderer.transform.position;
                highlightCopy.transform.rotation = originalRenderer.transform.rotation;
                highlightCopy.transform.localScale = originalRenderer.transform.localScale * 1.02f; // Slightly larger
                
                // Add mesh components
                MeshFilter highlightMeshFilter = highlightCopy.AddComponent<MeshFilter>();
                MeshRenderer highlightRenderer = highlightCopy.AddComponent<MeshRenderer>();
                
                highlightMeshFilter.mesh = originalMeshFilter.mesh;
                highlightRenderer.material = targetToothMaterial;
                highlightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                highlightRenderer.receiveShadows = false;
                
                if (showDebugInfo)
                {
                    Debug.Log($"  - Created highlight for: {originalRenderer.name}");
                }
            }
        }
        
        // Add pulsing animation to the parent highlight object
        if (enablePulseAnimation)
        {
            ToothPulseAnimation pulseScript = targetToothHighlight.AddComponent<ToothPulseAnimation>();
            pulseScript.pulseSpeed = pulseSpeed;
            pulseScript.originalScale = targetToothHighlight.transform.localScale;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Created highlight with {targetToothHighlight.transform.childCount} highlight copies for tooth: {targetTooth.name}");
        }
    }
    
    private void Update()
    {
        if (!procedureStarted || targetTooth == null) return;
        
        UpdateProcedureState();
    }
    
    private void UpdateProcedureState()
    {
        switch (currentState)
        {
            case ExtractionState.ToothHighlighted:
                CheckForForcepsProximity();
                break;
                
            case ExtractionState.ForcepsNearTooth:
                CheckExtractionProgress();
                break;
                
            case ExtractionState.ExtractionInProgress:
                UpdateExtractionTimer();
                break;
        }
    }
    
    private void CheckForForcepsProximity()
    {
        GameObject forceps = FindForcepsInHands();
        if (forceps != null)
        {
            float distance = Vector3.Distance(forceps.transform.position, targetTooth.transform.position);
            
            if (showDebugInfo)
            {
                Debug.Log($"Forceps found! Distance to tooth: {distance:F3}m (threshold: {extractionDistance:F3}m)");
            }
            
            if (distance <= extractionDistance)
            {
                currentState = ExtractionState.ForcepsNearTooth;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Forceps near target tooth. Distance: {distance:F3}m");
                }
                
                // Check if user is gripping (if required) or just start extraction
                if (!requireTriggerPress || IsForcepsBeingUsed(forceps))
                {
                    StartExtraction();
                }
            }
        }
        else if (showDebugInfo)
        {
            Debug.Log("No forceps detected in hands");
        }
    }
    
    private void CheckExtractionProgress()
    {
        GameObject forceps = FindForcepsInHands();
        if (forceps != null)
        {
            float distance = Vector3.Distance(forceps.transform.position, targetTooth.transform.position);
            
            if (showDebugInfo)
            {
                Debug.Log($"Checking extraction progress - Distance: {distance:F3}m, Using forceps: {IsForcepsBeingUsed(forceps)}");
            }
            
            if (distance <= extractionDistance && (!requireTriggerPress || IsForcepsBeingUsed(forceps)))
            {
                if (!extractionInProgress)
                {
                    StartExtraction();
                }
            }
            else
            {
                // Forceps moved away or stopped gripping
                if (extractionInProgress)
                {
                    StopExtraction();
                }
                currentState = ExtractionState.ToothHighlighted;
            }
        }
        else
        {
            // No forceps found
            if (extractionInProgress)
            {
                StopExtraction();
            }
            currentState = ExtractionState.ToothHighlighted;
        }
    }
    
    private void UpdateExtractionTimer()
    {
        extractionTimer += Time.deltaTime;
        
        if (showDebugInfo)
        {
            float progress = extractionTimer / extractionTime;
            Debug.Log($"Extraction progress: {progress * 100:F1}% ({extractionTimer:F1}s / {extractionTime:F1}s)");
        }
        
        if (extractionTimer >= extractionTime)
        {
            CompleteExtraction();
            return;
        }
        
        // Check if user is still gripping and close enough
        GameObject forceps = FindForcepsInHands();
        if (forceps == null)
        {
            if (showDebugInfo) Debug.Log("Extraction stopped - no forceps found");
            StopExtraction();
            return;
        }
        
        float distance = Vector3.Distance(forceps.transform.position, targetTooth.transform.position);
        if (distance > extractionDistance)
        {
            if (showDebugInfo) Debug.Log($"Extraction stopped - forceps too far ({distance:F3}m)");
            StopExtraction();
            return;
        }
        
        if (requireTriggerPress && !IsForcepsBeingUsed(forceps))
        {
            if (showDebugInfo) Debug.Log("Extraction stopped - trigger not pressed");
            StopExtraction();
            return;
        }
    }
    
    private GameObject FindForcepsInHands()
    {
        if (toolSystem == null) return null;
        
        // Access the private allTools list through reflection or find forceps manually
        // Since we can't access private fields directly, let's find forceps by searching scene objects
        
        // Method 1: Find all objects with MetaBlocksTool component
        MetaBlocksTool[] allToolComponents = FindObjectsOfType<MetaBlocksTool>();
        
        foreach (MetaBlocksTool toolComponent in allToolComponents)
        {
            if (toolComponent != null && 
                toolComponent.IsToolType(MetaBlocksTool.ToolType.DentalForceps))
            {
                // Check if this tool is being held (close to a controller)
                if (IsToolBeingHeld(toolComponent.gameObject))
                {
                    return toolComponent.gameObject;
                }
            }
        }
        
        // Method 2: Fallback - find by name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("forcep") && IsToolBeingHeld(obj))
            {
                return obj;
            }
        }
        
        return null;
    }
    
    private bool IsToolBeingHeld(GameObject tool)
    {
        // Use the same controller detection logic as MetaAutoGrabToolSystem
        Transform leftController = FindController("LeftHandAnchor");
        Transform rightController = FindController("RightHandAnchor");
        
        if (leftController != null)
        {
            float leftDistance = Vector3.Distance(tool.transform.position, leftController.position);
            if (leftDistance < 0.3f) return true;
        }
        
        if (rightController != null)
        {
            float rightDistance = Vector3.Distance(tool.transform.position, rightController.position);
            if (rightDistance < 0.3f) return true;
        }
        
        return false;
    }
    
    private Transform FindController(string preferredName)
    {
        // Use similar logic to MetaAutoGrabToolSystem's GetController method
        string[] rigNames = {"OVRCameraRig", "XR Origin", "XROrigin", "CameraRig", "Meta XR Origin"};
        
        GameObject cameraRig = null;
        
        // Find the actual camera rig
        foreach (string rigName in rigNames)
        {
            cameraRig = GameObject.Find(rigName);
            if (cameraRig != null)
            {
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
                return controller;
            }
        }
        
        // Fallback: look for any object with "left" or "right" in the name
        return FindControllerByKeyword(trackingSpace, preferredName.Contains("Left") ? "left" : "right");
    }
    
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
    
    private bool IsForcepsBeingUsed(GameObject forceps)
    {
        // Check if user is actively gripping/using the forceps
        // Method 1: Check for trigger/grip input
        bool leftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool rightTriggerPressed = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) || OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);
        
        // Method 2: Check if forceps are being held (close to controller)
        bool forcepsHeld = IsToolBeingHeld(forceps);
        
        if (showDebugInfo && forcepsHeld)
        {
            Debug.Log($"Forceps held: {forcepsHeld}, Left trigger: {leftTriggerPressed}, Right trigger: {rightTriggerPressed}");
        }
        
        // Return true if forceps are held AND trigger is pressed
        return forcepsHeld && (leftTriggerPressed || rightTriggerPressed);
    }
    
    private void StartExtraction()
    {
        extractionInProgress = true;
        extractionTimer = 0f;
        currentState = ExtractionState.ExtractionInProgress;
        
        // Play extraction start sound
        if (extractionStartSound != null)
        {
            audioSource.PlayOneShot(extractionStartSound);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Starting tooth extraction...");
        }
    }
    
    private void StopExtraction()
    {
        extractionInProgress = false;
        extractionTimer = 0f;
        currentState = ExtractionState.ForcepsNearTooth;
        
        if (showDebugInfo)
        {
            Debug.Log("Extraction interrupted");
        }
    }
    
    private void CompleteExtraction()
    {
        extractionInProgress = false;
        currentState = ExtractionState.ToothExtracted;
        
        // Remove highlight
        if (targetToothHighlight != null)
        {
            Destroy(targetToothHighlight);
        }
        
        // Make tooth grabbable BEFORE animation
        MakeToothGrabbable(targetTooth);
        
        // Animate tooth removal
        StartCoroutine(AnimateToothExtraction());
        
        // Play completion sound
        if (extractionCompleteSound != null)
        {
            audioSource.PlayOneShot(extractionCompleteSound);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Tooth extraction completed: {targetTooth.name}");
        }
    }
    
    private void MakeToothGrabbable(GameObject tooth)
    {
        if (tooth == null) return;
    
        // Find and enable the ISDK_HandGrabInteraction child object
        Transform handGrabInteraction = tooth.transform.Find("ISDK_HandGrabInteraction");
        if (handGrabInteraction != null)
        {
            handGrabInteraction.gameObject.SetActive(true);
            Debug.Log($"Enabled ISDK_HandGrabInteraction for {tooth.name}");
        }
        else
        {
            Debug.LogWarning($"ISDK_HandGrabInteraction not found in {tooth.name}");
        }
    }
    private Bounds GetToothBounds(GameObject tooth)
    {
        Bounds bounds = new Bounds(tooth.transform.position, Vector3.zero);
        
        // Get bounds from all child renderers
        MeshRenderer[] renderers = tooth.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        // Convert to local space
        bounds.center = tooth.transform.InverseTransformPoint(bounds.center);
        
        return bounds;
    }
    
    private IEnumerator AnimateToothExtraction()
    {
        if (targetTooth == null) yield break;
        
        // Temporarily disable physics during animation
        Rigidbody toothRigidbody = targetTooth.GetComponent<Rigidbody>();
        bool wasKinematic = false;
        if (toothRigidbody != null)
        {
            wasKinematic = toothRigidbody.isKinematic;
            toothRigidbody.isKinematic = true;
        }
        
        Vector3 startPosition = targetTooth.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * 0.1f; // Move up 10cm
        Quaternion startRotation = targetTooth.transform.rotation;
        
        float duration = 1.5f;
        float elapsed = 0f;
        
        // Move tooth up and rotate
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Use smooth curve for animation
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            targetTooth.transform.position = Vector3.Lerp(startPosition, endPosition, smoothProgress);
            targetTooth.transform.rotation = startRotation * Quaternion.Euler(0, 180f * smoothProgress, 0);
            
            yield return null;
        }
        
        // Re-enable physics after animation
        if (toothRigidbody != null)
        {
            toothRigidbody.isKinematic = wasKinematic;
            // Give it a slight upward velocity so it falls naturally
            toothRigidbody.velocity = Vector3.up * 0.5f;
        }
        
        // Change tooth material to extracted material
        if (extractedToothMaterial != null)
        {
            MeshRenderer[] renderers = targetTooth.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.material = extractedToothMaterial;
            }
        }
        
        // Complete procedure
        CompleteProcedure();
    }
    
    private void CompleteProcedure()
    {
        currentState = ExtractionState.ProcedureComplete;
        procedureStarted = false;
        
        // Play success sound
        if (successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Tooth extraction procedure completed successfully!");
        }
    }
    
    // Public methods for external control
    public void ResetProcedure()
    {
        // Reset tooth to original state
        if (targetTooth != null && originalToothMaterial != null)
        {
            Renderer toothRenderer = targetTooth.GetComponent<Renderer>();
            if (toothRenderer != null)
            {
                toothRenderer.material = originalToothMaterial;
            }
            
            // Reset position (if moved)
            // You might want to store original position/rotation
        }
        
        // Clean up highlight
        if (targetToothHighlight != null)
        {
            Destroy(targetToothHighlight);
        }
        
        // Reset state
        currentState = ExtractionState.WaitingToStart;
        procedureStarted = false;
        extractionInProgress = false;
        extractionTimer = 0f;
        targetTooth = null;
        
        Debug.Log("Procedure reset");
    }
    
    public void StartNewExtraction()
    {
        ResetProcedure();
        StartProcedure();
    }
    
    // Debug methods
    [ContextMenu("Start Extraction Procedure")]
    public void ManualStartProcedure()
    {
        StartProcedure();
    }
    
    [ContextMenu("Reset Extraction Procedure")]
    public void ManualResetProcedure()
    {
        ResetProcedure();
    }
    
    [ContextMenu("Debug Tooth Structure")]
    public void DebugToothStructure()
    {
        Debug.Log("=== Tooth Structure Debug ===");
        
        if (availableTeeth == null || availableTeeth.Length == 0)
        {
            Debug.Log("No teeth available");
            return;
        }
        
        foreach (GameObject tooth in availableTeeth)
        {
            if (tooth != null)
            {
                Debug.Log($"Tooth: {tooth.name}");
                
                // Check direct components
                MeshRenderer directRenderer = tooth.GetComponent<MeshRenderer>();
                MeshFilter directMeshFilter = tooth.GetComponent<MeshFilter>();
                Debug.Log($"  - Direct MeshRenderer: {directRenderer != null}");
                Debug.Log($"  - Direct MeshFilter: {directMeshFilter != null}");
                
                // Check all child renderers
                MeshRenderer[] childRenderers = tooth.GetComponentsInChildren<MeshRenderer>();
                Debug.Log($"  - Total MeshRenderers (including children): {childRenderers.Length}");
                
                foreach (MeshRenderer renderer in childRenderers)
                {
                    Debug.Log($"    └─ {renderer.name} (Material: {renderer.material?.name ?? "None"})");
                }
                
                Debug.Log($"  - Children count: {tooth.transform.childCount}");
                for (int i = 0; i < tooth.transform.childCount; i++)
                {
                    Transform child = tooth.transform.GetChild(i);
                    MeshRenderer childRenderer = child.GetComponent<MeshRenderer>();
                    Debug.Log($"    └─ Child: {child.name} (Has MeshRenderer: {childRenderer != null})");
                }
                
                Debug.Log(""); // Empty line for readability
            }
        }
    }
    
    [ContextMenu("Test Highlight on First Tooth")]
    public void TestHighlightOnFirstTooth()
    {
        if (availableTeeth == null || availableTeeth.Length == 0)
        {
            Debug.Log("No teeth available for testing");
            return;
        }
        
        // Clear any existing highlight
        if (targetToothHighlight != null)
        {
            Destroy(targetToothHighlight);
        }
        
        // Set first tooth as target and create highlight
        targetTooth = availableTeeth[0];
        CreateToothHighlight();
        
        Debug.Log($"Test highlight created for: {targetTooth.name}");
    }
    
    [ContextMenu("Test Make First Tooth Grabbable")]
    public void TestMakeFirstToothGrabbable()
    {
        if (availableTeeth == null || availableTeeth.Length == 0)
        {
            Debug.Log("No teeth available for testing");
            return;
        }
        
        GameObject testTooth = availableTeeth[0];
        MakeToothGrabbable(testTooth);
        
        Debug.Log($"Made {testTooth.name} grabbable for testing");
    }
    
    [ContextMenu("Debug Procedure State")]
    public void DebugProcedureState()
    {
        Debug.Log($"=== Tooth Extraction Debug ===");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Target Tooth: {(targetTooth != null ? targetTooth.name : "None")}");
        Debug.Log($"Procedure Started: {procedureStarted}");
        Debug.Log($"Extraction in Progress: {extractionInProgress}");
        Debug.Log($"Extraction Timer: {extractionTimer:F2}s / {extractionTime:F2}s");
        
        GameObject forceps = FindForcepsInHands();
        Debug.Log($"Forceps in hands: {(forceps != null ? forceps.name : "None")}");
        
        if (forceps != null && targetTooth != null)
        {
            float distance = Vector3.Distance(forceps.transform.position, targetTooth.transform.position);
            Debug.Log($"Forceps to tooth distance: {distance:F3}m (threshold: {extractionDistance:F3}m)");
        }
    }
}

// Animation component for tooth pulsing
public class ToothPulseAnimation : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public Vector3 originalScale = Vector3.one;
    public float pulseIntensity = 0.1f;
    
    private void Update()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        float scaleMultiplier = 1f + (pulse * pulseIntensity);
        transform.localScale = originalScale * scaleMultiplier;
    }
}