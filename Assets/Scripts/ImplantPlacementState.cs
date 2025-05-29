using UnityEngine;
using System.Collections;

public class ImplantPlacementState : ExtractionStateBase
{
    [Header("Implant Placement Settings")]
    public float toolDistance = 0.15f; // Distance to socket
    public float drillingTime = 5f; // 5 seconds of drilling
    public float screwingTime = 3f; // 3 seconds of screwing
    public AudioClip drillingSound;
    public AudioClip screwingSound;
    public AudioClip placementSound;
    
    [Header("Implant Objects")]
    public GameObject implantScrewPrefab; // The implant screw object
    
    // Phase tracking
    public enum ImplantPhase
    {
        Drilling,
        PlacingScrew,
        ScrewingIn,
        Complete
    }
    
    private ImplantPhase currentPhase = ImplantPhase.Drilling;
    
    // Drilling phase
    private bool drillingComplete = false;
    private float drillingTimer = 0f;
    private bool drillingInProgress = false;
    
    // Placement phase
    private bool screwPlaced = false;
    private GameObject placedImplantScrew;
    
    // Screwing phase
    private bool screwingComplete = false;
    private float screwingTimer = 0f;
    private bool screwingInProgress = false;
    
    // Overall completion
    private bool implantPermanentlyComplete = false;
    
    public override void OnEnterState()
    {
        base.OnEnterState();
        
        // Reset all phase tracking
        currentPhase = ImplantPhase.Drilling;
        drillingComplete = false;
        drillingTimer = 0f;
        drillingInProgress = false;
        screwPlaced = false;
        screwingComplete = false;
        screwingTimer = 0f;
        screwingInProgress = false;
        implantPermanentlyComplete = false;
        
        // Change socket to yellow for drilling
        extractionSystem.UpdateSocketIndicatorColor(Color.yellow, "Drilling");
        
        // Clean up any existing implant screw
        if (placedImplantScrew != null)
        {
            Destroy(placedImplantScrew);
            placedImplantScrew = null;
        }
        
        // Update UI panel
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Step 4: Use drill to prepare the socket for implant placement");
        }
    }
    
    public override void UpdateState()
    {
        if (implantPermanentlyComplete)
        {
            Debug.Log("Implant placement already complete, not processing further input");
            return;
        }
        
        switch (currentPhase)
        {
            case ImplantPhase.Drilling:
                CheckForDrilling();
                break;
                
            case ImplantPhase.PlacingScrew:
                CheckForScrewPlacement();
                break;
                
            case ImplantPhase.ScrewingIn:
                CheckForScrewing();
                break;
        }
    }
    
    #region Drilling Phase
    
    private void CheckForDrilling()
    {
        GameObject drill = FindDrillInHands();
        if (drill != null)
        {
            Vector3 socketPosition = extractionSystem.SocketPosition;
            float distance = Vector3.Distance(drill.transform.position, socketPosition);
        
            // Update UI with distance debugging
            if (extractionSystem.uiPanel != null)
            {
                extractionSystem.uiPanel.UpdateInstruction($"Drill the socket\nDistance to socket: {distance:F3}m (need < {toolDistance:F3}m)");
            }
        
            if (ShowDebugInfo)
            {
                Debug.Log($"Drill distance to socket: {distance:F3}m (threshold: {toolDistance:F3}m)");
            }
        
            if (distance <= toolDistance)
            {
                if (IsDrillBeingUsed(drill))
                {
                    if (!drillingInProgress)
                    {
                        StartDrilling();
                    }
                    else
                    {
                        UpdateDrillingTimer();
                    }
                }
                else if (drillingInProgress)
                {
                    StopDrilling();
                }
            }
            else if (drillingInProgress)
            {
                StopDrilling();
            }
        }
        else if (extractionSystem.uiPanel != null)
        {
            // Show helpful message when no drill detected
            extractionSystem.uiPanel.UpdateInstruction("Grab the dental drill and position it near the socket");
        }
    }
    
    private GameObject FindDrillInHands()
    {
        MetaBlocksTool[] allToolComponents = FindObjectsOfType<MetaBlocksTool>();
        
        foreach (MetaBlocksTool toolComponent in allToolComponents)
        {
            if (toolComponent != null && 
                (toolComponent.name.ToLower().Contains("drill") || 
                 toolComponent.name.ToLower().Contains("dental drill")))
            {
                if (extractionSystem.IsToolBeingHeld(toolComponent.gameObject))
                {
                    return toolComponent.gameObject;
                }
            }
        }
        return null;
    }
    
    private bool IsDrillBeingUsed(GameObject drill)
    {
        bool leftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        bool rightTriggerPressed = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);
        bool drillHeld = extractionSystem.IsToolBeingHeld(drill);
        
        return drillHeld && (leftTriggerPressed || rightTriggerPressed);
    }
    
    private void StartDrilling()
    {
        drillingInProgress = true;
        drillingTimer = 0f;
        
        if (drillingSound != null && AudioSource != null)
        {
            AudioSource.PlayOneShot(drillingSound);
        }
        
        if (ShowDebugInfo)
        {
            Debug.Log("Starting socket drilling...");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Hold trigger and drill the socket...");
        }
    }
    
    private void UpdateDrillingTimer()
    {
        drillingTimer += Time.deltaTime;
        
        // Update UI with progress
        if (extractionSystem.uiPanel != null)
        {
            float progress = drillingTimer / drillingTime;
            extractionSystem.uiPanel.UpdateProgress($"Drilling socket... {drillingTimer:F1}s / {drillingTime:F1}s", progress);
        }
        
        if (drillingTimer >= drillingTime)
        {
            CompleteDrilling();
        }
    }
    
    private void StopDrilling()
    {
        drillingInProgress = false;
        drillingTimer = 0f;
        
        if (ShowDebugInfo)
        {
            Debug.Log("Drilling interrupted");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Keep drill close to socket and hold trigger");
        }
    }
    
    private void CompleteDrilling()
    {
        drillingInProgress = false;
        drillingComplete = true;
        currentPhase = ImplantPhase.PlacingScrew;
    
        if (ShowDebugInfo)
        {
            Debug.Log("Socket drilling complete! Ready for implant screw placement");
        }
    
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Drilling complete! Now grab your screw and place it into the socket");
        }
    
        // Find the existing screw in the scene instead of creating one
        FindExistingScrew();
    }
    
    #endregion
    
    #region Screw Placement Phase
    
    private void CreateImplantScrew()
    {
        if (extractionSystem.SocketPosition == Vector3.zero) return;
    
        if (implantScrewPrefab != null)
        {
            // Spawn screw further away so it doesn't auto-trigger placement
            Vector3 screwPosition = extractionSystem.SocketPosition + Vector3.up * 0.15f; // 15cm above instead of 5cm
            placedImplantScrew = Instantiate(implantScrewPrefab, screwPosition, Quaternion.identity);
            placedImplantScrew.name = "PlacedImplantScrew";
        
            if (ShowDebugInfo)
            {
                Debug.Log($"Created implant screw from prefab: {placedImplantScrew.name} at position: {screwPosition}");
            }
        }
        else
        {
            Debug.LogWarning("No implant screw prefab assigned!");
            return;
        }
    
        // Make sure the screw has the necessary components for grabbing
        if (placedImplantScrew != null)
        {
            // Enable hand grab interaction
            Transform handGrabInteraction = placedImplantScrew.transform.Find("ISDK_HandGrabInteraction");
            if (handGrabInteraction != null)
            {
                handGrabInteraction.gameObject.SetActive(true);
                if (ShowDebugInfo)
                {
                    Debug.Log("Enabled ISDK_HandGrabInteraction on implant screw");
                }
            }
        
            // Add rigidbody if missing
            Rigidbody screwRb = placedImplantScrew.GetComponent<Rigidbody>();
            if (screwRb == null)
            {
                screwRb = placedImplantScrew.AddComponent<Rigidbody>();
                screwRb.mass = 0.005f; // Very light
            }
        
            if (ShowDebugInfo)
            {
                Debug.Log($"Implant screw ready for manual placement: {placedImplantScrew.name}");
            }
        }
    }
    
    private void CheckForScrewPlacement()
    {
        if (placedImplantScrew != null)
        {
            Vector3 socketIndicatorPosition = Vector3.zero;
        
            if (extractionSystem.socketIndicator != null)
            {
                socketIndicatorPosition = extractionSystem.socketIndicator.transform.position;
            }
            else
            {
                socketIndicatorPosition = extractionSystem.SocketPosition;
            }
        
            //float distance = Vector3.Distance(placedImplantScrew.transform.position, socketIndicatorPosition);
        
            Vector3 socketPosition = extractionSystem.SocketPosition;
            float distance = Vector3.Distance(placedImplantScrew.transform.position, socketPosition);
            // Use the new screw-specific grab detection
            bool screwBeingHeld = IsScrewBeingHeld(placedImplantScrew);
        
            if (ShowDebugInfo)
            {
                Debug.Log($"Screw distance to socket: {distance:F3}m, Being held: {screwBeingHeld}");
            }
        
            // Update UI
            if (extractionSystem.uiPanel != null)
            {
                string grabStatus = screwBeingHeld ? "HELD" : "NOT HELD";
                extractionSystem.uiPanel.UpdateInstruction($"Grab and place screw in socket\nDistance: {distance:F3}m (need < 0.05m)\nStatus: {grabStatus}");
            }
        
            // Only allow placement if screw is close AND being held
            if (distance <= 0.05f && screwBeingHeld)
            {
                CompleteScrewPlacement();
            }
        }
        else if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Could not find screw in scene!");
        }
    }
    private void FindExistingScrew()
    {
        // Look for existing screw in the scene by name or component
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
    
        foreach (GameObject obj in allObjects)
        {
            // Adjust this condition based on your screw's name or components
            if (obj.name.ToLower().Contains("screw") || obj.name.ToLower().Contains("implant"))
            {
                // Make sure it has hand grab interaction
                Transform handGrabInteraction = obj.transform.Find("ISDK_HandGrabInteraction");
                if (handGrabInteraction != null)
                {
                    placedImplantScrew = obj;
                
                    // Enable hand grab interaction
                    handGrabInteraction.gameObject.SetActive(true);
                
                    if (ShowDebugInfo)
                    {
                        Debug.Log($"Found existing screw in scene: {placedImplantScrew.name}");
                    }
                    break;
                }
            }
        }
    
        if (placedImplantScrew == null)
        {
            Debug.LogWarning("Could not find existing screw in scene! Make sure your screw has 'screw' or 'implant' in its name and has ISDK_HandGrabInteraction child.");
        }
    }
    private void CompleteScrewPlacement()
    {
        screwPlaced = true;
        currentPhase = ImplantPhase.ScrewingIn;
        
        if (placementSound != null && AudioSource != null)
        {
            AudioSource.PlayOneShot(placementSound);
        }
        
        if (ShowDebugInfo)
        {
            Debug.Log("Implant screw placed! Ready for screwing in");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Screw placed! Now use screwdriver to secure the implant");
        }
        
        // Make screw kinematic so it doesn't fall while being screwed
        if (placedImplantScrew != null)
        {
            Rigidbody screwRb = placedImplantScrew.GetComponent<Rigidbody>();
            if (screwRb != null)
            {
                screwRb.isKinematic = true;
            }
        }
    }
    
    #endregion
    
    #region Screwing Phase
    
    private void CheckForScrewing()
    {
        GameObject screwdriver = FindScrewdriverInHands();
        if (screwdriver != null && placedImplantScrew != null)
        {
            float distance = Vector3.Distance(screwdriver.transform.position, placedImplantScrew.transform.position);
            
            if (ShowDebugInfo)
            {
                Debug.Log($"Screwdriver distance to implant: {distance:F3}m (threshold: {toolDistance:F3}m)");
            }
            
            if (distance <= toolDistance)
            {
                if (IsScrewdriverBeingUsed(screwdriver))
                {
                    if (!screwingInProgress)
                    {
                        StartScrewing();
                    }
                    else
                    {
                        UpdateScrewingTimer();
                    }
                }
                else if (screwingInProgress)
                {
                    StopScrewing();
                }
            }
            else if (screwingInProgress)
            {
                StopScrewing();
            }
        }
    }
    
    private GameObject FindScrewdriverInHands()
    {
        MetaBlocksTool[] allToolComponents = FindObjectsOfType<MetaBlocksTool>();
        
        foreach (MetaBlocksTool toolComponent in allToolComponents)
        {
            if (toolComponent != null && 
                (toolComponent.name.ToLower().Contains("screwdriver") || 
                 toolComponent.name.ToLower().Contains("driver") ||
                 toolComponent.name.ToLower().Contains("screw")))
            {
                if (extractionSystem.IsToolBeingHeld(toolComponent.gameObject))
                {
                    return toolComponent.gameObject;
                }
            }
        }
        return null;
    }
    
    private bool IsScrewdriverBeingUsed(GameObject screwdriver)
    {
        bool leftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        bool rightTriggerPressed = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);
        bool screwdriverHeld = extractionSystem.IsToolBeingHeld(screwdriver);
        
        return screwdriverHeld && (leftTriggerPressed || rightTriggerPressed);
    }
    
    private void StartScrewing()
    {
        screwingInProgress = true;
        screwingTimer = 0f;
        
        if (screwingSound != null && AudioSource != null)
        {
            AudioSource.PlayOneShot(screwingSound);
        }
        
        if (ShowDebugInfo)
        {
            Debug.Log("Starting implant screwing...");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Hold trigger and screw in the implant...");
        }
    }
    
    private void UpdateScrewingTimer()
    {
        screwingTimer += Time.deltaTime;
        
        // Animate the screw rotation and movement
        if (placedImplantScrew != null)
        {
            float rotationSpeed = 180f; // degrees per second
            float moveSpeed = 0.01f; // units per second downward
            
            placedImplantScrew.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            placedImplantScrew.transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        }
        
        // Update UI with progress
        if (extractionSystem.uiPanel != null)
        {
            float progress = screwingTimer / screwingTime;
            extractionSystem.uiPanel.UpdateProgress($"Screwing implant... {screwingTimer:F1}s / {screwingTime:F1}s", progress);
        }
        
        if (screwingTimer >= screwingTime)
        {
            CompleteScrewing();
        }
    }
    
    private void StopScrewing()
    {
        screwingInProgress = false;
        screwingTimer = 0f;
        
        if (ShowDebugInfo)
        {
            Debug.Log("Screwing interrupted");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Keep screwdriver close to implant and hold trigger");
        }
    }
    
    private void CompleteScrewing()
    {
        screwingInProgress = false;
        screwingComplete = true;
        currentPhase = ImplantPhase.Complete;
        implantPermanentlyComplete = true;
        
        if (ShowDebugInfo)
        {
            Debug.Log("Implant placement complete!");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Implant placement complete! Implant successfully secured.");
        }
        
        // Wait period then transition to next step or completion
        StartCoroutine(ImplantWaitPeriod());
    }
    
    private IEnumerator ImplantWaitPeriod()
    {
        yield return new WaitForSeconds(3f);
        
        // Update UI for completion
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Dental implant procedure complete!");
        }
        
        // Transition to completion
        TransitionToState(ToothExtractionSystem.ExtractionState.ProcedureComplete);
    }
    
    #endregion
    
    public bool IsImplantComplete()
    {
        return implantPermanentlyComplete;
    }
    
    public void ResetImplant()
    {
        currentPhase = ImplantPhase.Drilling;
        drillingComplete = false;
        drillingTimer = 0f;
        drillingInProgress = false;
        screwPlaced = false;
        screwingComplete = false;
        screwingTimer = 0f;
        screwingInProgress = false;
        implantPermanentlyComplete = false;
        
        if (placedImplantScrew != null)
        {
            Destroy(placedImplantScrew);
            placedImplantScrew = null;
        }
    }
    [ContextMenu("Debug Screw Grab Detection")]
    public void DebugScrewGrabDetection()
    {
        if (placedImplantScrew == null)
        {
            Debug.Log("No screw found!");
            return;
        }
    
        Debug.Log($"=== SCREW GRAB DEBUG ===");
        Debug.Log($"Screw name: {placedImplantScrew.name}");
        Debug.Log($"Screw position: {placedImplantScrew.transform.position}");
    
        // Check for ISDK component
        Transform handGrabInteraction = placedImplantScrew.transform.Find("ISDK_HandGrabInteraction");
        Debug.Log($"Has ISDK_HandGrabInteraction: {handGrabInteraction != null}");
        if (handGrabInteraction != null)
        {
            Debug.Log($"ISDK component active: {handGrabInteraction.gameObject.activeSelf}");
        }
    
        // Check controllers
        Transform leftController = extractionSystem.FindController("LeftHandAnchor");
        Transform rightController = extractionSystem.FindController("RightHandAnchor");
    
        Debug.Log($"Left controller found: {leftController != null}");
        Debug.Log($"Right controller found: {rightController != null}");
    
        if (leftController != null)
        {
            float leftDistance = Vector3.Distance(placedImplantScrew.transform.position, leftController.position);
            Debug.Log($"Distance to left controller: {leftDistance:F3}m");
        }
    
        if (rightController != null)
        {
            float rightDistance = Vector3.Distance(placedImplantScrew.transform.position, rightController.position);
            Debug.Log($"Distance to right controller: {rightDistance:F3}m");
        }
    
        // Test the IsToolBeingHeld method directly
        bool isHeld = extractionSystem.IsToolBeingHeld(placedImplantScrew);
        Debug.Log($"IsToolBeingHeld result: {isHeld}");
    }
    private bool IsScrewBeingHeld(GameObject screw)
    {
        if (screw == null) return false;
    
        // Check if the screw's rigidbody is kinematic (usually means it's being held)
        Rigidbody screwRb = screw.GetComponent<Rigidbody>();
        if (screwRb != null && screwRb.isKinematic)
        {
            if (ShowDebugInfo)
            {
                Debug.Log("Screw is kinematic (likely being held)");
            }
            return true;
        }
    
        // Alternative: Check distance to controllers
        Transform leftController = FindController("LeftHandAnchor");
        Transform rightController = FindController("RightHandAnchor");
    
        float grabDistance = 0.2f; // 20cm threshold for being "held"
    
        if (leftController != null)
        {
            float leftDistance = Vector3.Distance(screw.transform.position, leftController.position);
            if (leftDistance < grabDistance)
            {
                if (ShowDebugInfo)
                {
                    Debug.Log($"Screw close to left controller: {leftDistance:F3}m");
                }
                return true;
            }
        }
    
        if (rightController != null)
        {
            float rightDistance = Vector3.Distance(screw.transform.position, rightController.position);
            if (rightDistance < grabDistance)
            {
                if (ShowDebugInfo)
                {
                    Debug.Log($"Screw close to right controller: {rightDistance:F3}m");
                }
                return true;
            }
        }
    
        return false;
    }
// Helper method to find controllers (copy from ToothExtractionSystem)
    private Transform FindController(string preferredName)
    {
        string[] rigNames = {"OVRCameraRig", "XR Origin", "XROrigin", "CameraRig", "Meta XR Origin"};
    
        GameObject cameraRig = null;
    
        foreach (string rigName in rigNames)
        {
            cameraRig = GameObject.Find(rigName);
            if (cameraRig != null) break;
        }
    
        if (cameraRig == null) return null;
    
        Transform trackingSpace = null;
        string[] trackingSpaceNames = {"TrackingSpace", "Tracking Space", "CameraOffset"};
    
        foreach (string spaceName in trackingSpaceNames)
        {
            trackingSpace = cameraRig.transform.Find(spaceName);
            if (trackingSpace != null) break;
        }
    
        if (trackingSpace == null)
            trackingSpace = cameraRig.transform;
    
        string[] leftControllerNames = {"LeftHandAnchor", "LeftControllerAnchor", "Left Controller", "LeftHand", "Left Hand"};
        string[] rightControllerNames = {"RightHandAnchor", "RightControllerAnchor", "Right Controller", "RightHand", "Right Hand"};
    
        string[] namesToTry = preferredName.Contains("Left") ? leftControllerNames : rightControllerNames;
    
        foreach (string name in namesToTry)
        {
            Transform controller = FindChildRecursive(trackingSpace, name);
            if (controller != null) return controller;
        }
    
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;
    
        for (int i = 0; i < parent.childCount; i++)
        {
            found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null) return found;
        }
    
        return null;
    }
}