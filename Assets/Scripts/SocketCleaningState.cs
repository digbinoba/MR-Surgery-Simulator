using UnityEngine;
using System.Collections;

public class SocketCleaningState : ExtractionStateBase
{
    [Header("Socket Cleaning Settings")]
    public float cleaningDistance = 0.15f; // Distance to socket
    public float cleaningTime = 5f; // 5 seconds of cleaning
    public AudioClip cleaningSound;
    
    private bool cleaningComplete = false;
    private bool cleaningPermanentlyComplete = false;
    private float cleaningTimer = 0f;
    private bool cleaningInProgress = false;
    
    public override void OnEnterState()
    {
        base.OnEnterState();
        cleaningComplete = false;
        cleaningTimer = 0f;
        cleaningInProgress = false;
        // Change socket to blue for cleaning
        
        extractionSystem.UpdateSocketIndicatorColor(Color.blue, "Socket Cleaning");
        // Update UI panel
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Step 3: Use irrigation syringe to clean the empty socket");
        }
    }
    
    public override void UpdateState()
    {
        // If cleaning is permanently complete, don't do anything
        if (cleaningPermanentlyComplete)
        {
            Debug.Log("Socket cleaning already complete, not processing further input");
            return;
        }
        
        CheckForSyringeProximity();
    }
    
    private void CheckForSyringeProximity()
    {
        GameObject irrigationSyringe = FindIrrigationSyringeInHands();
        if (irrigationSyringe != null)
        {
            // Use the socket position instead of the moved tooth
            Vector3 socketPosition = extractionSystem.SocketPosition;
            float distance = Vector3.Distance(irrigationSyringe.transform.position, socketPosition);
        
            if (ShowDebugInfo)
            {
                Debug.Log($"Irrigation syringe distance to socket: {distance:F3}m (threshold: {cleaningDistance:F3}m)");
            }
        
            if (distance <= cleaningDistance)
            {
                if (IsSyringeBeingUsed(irrigationSyringe))
                {
                    if (!cleaningInProgress)
                    {
                        StartCleaning();
                    }
                    else
                    {
                        UpdateCleaningTimer();
                    }
                }
                else if (cleaningInProgress)
                {
                    StopCleaning();
                }
            }
            else if (cleaningInProgress)
            {
                StopCleaning();
            }
        }
    }
    
    private GameObject FindIrrigationSyringeInHands()
    {
        MetaBlocksTool[] allToolComponents = FindObjectsOfType<MetaBlocksTool>();
        
        foreach (MetaBlocksTool toolComponent in allToolComponents)
        {
            if (toolComponent != null && 
                (toolComponent.name.ToLower().Contains("irrigationsyringe") || 
                 toolComponent.name.ToLower().Contains("irrigation") ||
                 toolComponent.name.ToLower().Contains("cleaning")))
            {
                if (extractionSystem.IsToolBeingHeld(toolComponent.gameObject))
                {
                    return toolComponent.gameObject;
                }
            }
        }
        return null;
    }
    
    private bool IsSyringeBeingUsed(GameObject syringe)
    {
        bool leftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        bool rightTriggerPressed = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger);
        bool syringeHeld = extractionSystem.IsToolBeingHeld(syringe);
        
        return syringeHeld && (leftTriggerPressed || rightTriggerPressed);
    }
    
    private void StartCleaning()
    {
        cleaningInProgress = true;
        cleaningTimer = 0f;
        
        if (cleaningSound != null && AudioSource != null)
        {
            AudioSource.PlayOneShot(cleaningSound);
        }
        
        if (ShowDebugInfo)
        {
            Debug.Log("Starting socket cleaning...");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Hold trigger and irrigate the socket...");
        }
    }
    
    private void UpdateCleaningTimer()
    {
        cleaningTimer += Time.deltaTime;
        
        // Update UI with progress
        if (extractionSystem.uiPanel != null)
        {
            float progress = cleaningTimer / cleaningTime;
            extractionSystem.uiPanel.UpdateProgress($"Cleaning socket... {cleaningTimer:F1}s / {cleaningTime:F1}s", progress);
        }
        
        if (cleaningTimer >= cleaningTime)
        {
            CompleteCleaning();
        }
    }
    
    private void StopCleaning()
    {
        cleaningInProgress = false;
        cleaningTimer = 0f;
        
        if (ShowDebugInfo)
        {
            Debug.Log("Socket cleaning interrupted");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Keep irrigation syringe close to socket and hold trigger");
        }
    }
    
    private void CompleteCleaning()
    {
        cleaningInProgress = false;
        cleaningComplete = true;
        cleaningPermanentlyComplete = true;
        
        if (ShowDebugInfo)
        {
            Debug.Log("Socket cleaning complete!");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Socket cleaning complete! Socket is now clean and ready.");
        }
        
        // Wait period then transition to next step (or completion)
        StartCoroutine(CleaningWaitPeriod());
    }
    
    private IEnumerator CleaningWaitPeriod()
    {
        yield return new WaitForSeconds(2f);
        
        // Update UI for next step
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Socket cleaned! Ready for implant placement.");
        }
    
        // Transition to implant placement instead of completion
        TransitionToState(ToothExtractionSystem.ExtractionState.ImplantPlacement);
    }
    
    public bool IsCleaningComplete()
    {
        return cleaningPermanentlyComplete;
    }
    
    public void ResetCleaning()
    {
        cleaningComplete = false;
        cleaningPermanentlyComplete = false;
        cleaningInProgress = false;
        cleaningTimer = 0f;
    }
}