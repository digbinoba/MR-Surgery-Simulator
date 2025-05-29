using UnityEngine;
using System.Collections;

public class AnesthesiaState : ExtractionStateBase
{
    [Header("Anesthesia Settings")]
    public float anesthesiaDistance = 0.1f;
    public float anesthesiaTime = 3f;
    public AudioClip anesthesiaSound;
    public float gracePeriod = 1f; // Allow 1 second out of range
    
    private bool anesthesiaComplete = false;
    private bool anesthesiaPermanentlyComplete = false; // Add this flag
    private float anesthesiaTimer = 0f;
    private bool anesthesiaInProgress = false;
    private float outOfRangeTimer = 0f; // Add this
    
    public override void OnEnterState()
    {
        base.OnEnterState();
        anesthesiaComplete = false;
        anesthesiaTimer = 0f;
        anesthesiaInProgress = false;
        
        // Update UI panel
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Step 1: Administer local anesthesia near the highlighted tooth");
        }
    }
    
    public override void UpdateState()
    {
        // If anesthesia is permanently complete, don't do anything
        if (anesthesiaPermanentlyComplete)
        {
            Debug.Log("Anesthesia already complete, not processing further input");
            return;
        }
    
        CheckForAnesthesiaProximity();
    }
    
    private void CheckForAnesthesiaProximity()
    {
        // Don't process any input if anesthesia is permanently complete
        if (anesthesiaPermanentlyComplete)
        {
            return;
        }
        
        GameObject syringe = FindSyringeInHands();
        if (syringe != null && TargetTooth != null)
        {
            float distance = Vector3.Distance(syringe.transform.position, TargetTooth.transform.position);
            bool inRange = distance <= anesthesiaDistance;
            bool triggerPressed = IsSyringeBeingUsed(syringe);
        
            if (ShowDebugInfo)
            {
                Debug.Log($"Syringe distance: {distance:F3}m | In range: {inRange} | Trigger: {triggerPressed}");
            }
        
            if (inRange && triggerPressed)
            {
                // Reset out of range timer when back in range
                outOfRangeTimer = 0f;
            
                if (!anesthesiaInProgress)
                {
                    StartAnesthesia();
                }
                else
                {
                    UpdateAnesthesiaTimer();
                }
            }
            else if (anesthesiaInProgress)
            {
                // Only stop if out of range for too long
                if (!inRange || !triggerPressed)
                {
                    outOfRangeTimer += Time.deltaTime;
                
                    if (outOfRangeTimer >= gracePeriod)
                    {
                        StopAnesthesia();
                        outOfRangeTimer = 0f;
                    }
                    else
                    {
                        // Still in grace period - continue injection
                        UpdateAnesthesiaTimer();
                    
                        if (ShowDebugInfo)
                        {
                            Debug.Log($"Grace period: {outOfRangeTimer:F1}s / {gracePeriod:F1}s");
                        }
                    }
                }
            }
        }
    }
    
    private GameObject FindSyringeInHands()
    {
        MetaBlocksTool[] allToolComponents = FindObjectsOfType<MetaBlocksTool>();
        
        foreach (MetaBlocksTool toolComponent in allToolComponents)
        {
            if (toolComponent != null && 
                (toolComponent.name.ToLower().Contains("syringe") || 
                 toolComponent.name.ToLower().Contains("anesthesia")))
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
    
    private void StartAnesthesia()
    {
        anesthesiaInProgress = true;
        anesthesiaTimer = 0f;
        
        if (anesthesiaSound != null && AudioSource != null)
        {
            AudioSource.PlayOneShot(anesthesiaSound);
        }
        
        if (ShowDebugInfo)
        {
            Debug.Log("Starting anesthesia administration...");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Hold trigger and keep syringe near tooth...");
        }
    }
    
    private void UpdateAnesthesiaTimer()
    {
        anesthesiaTimer += Time.deltaTime;
        
        // Update UI with progress
        if (extractionSystem.uiPanel != null)
        {
            float progress = anesthesiaTimer / anesthesiaTime;
            extractionSystem.uiPanel.UpdateProgress($"Injecting... {anesthesiaTimer:F1}s / {anesthesiaTime:F1}s", progress);
        }
        
        if (anesthesiaTimer >= anesthesiaTime)
        {
            CompleteAnesthesia();
        }
    }
    
    private void StopAnesthesia()
    {
        anesthesiaInProgress = false;
        anesthesiaTimer = 0f;
        
        if (ShowDebugInfo)
        {
            Debug.Log("Anesthesia interrupted");
        }
        
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Keep syringe close to tooth and hold trigger");
        }
    }
    
    private void CompleteAnesthesia()
    {
        anesthesiaInProgress = false;
        anesthesiaComplete = true;
        anesthesiaPermanentlyComplete = true; // Set permanent flag
    
        Debug.Log("Anesthesia administration complete! Waiting for numbness...");
    
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Anesthesia complete! Wait for numbness to take effect...");
        }
    
        // Brief wait period then transition
        StartCoroutine(AnesthesiaWaitPeriod());
    }
    
    private IEnumerator AnesthesiaWaitPeriod()
    {
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Numbness effect complete, transitioning to extraction");
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Now use forceps to extract the tooth");
        }
        
        // Transition to next state
        TransitionToState(ToothExtractionSystem.ExtractionState.ToothHighlighted);
    }
    
    public bool IsAnesthesiaComplete()
    {
        return anesthesiaPermanentlyComplete;
    }
    public void ResetAnesthesia()
    {
        anesthesiaComplete = false;
        anesthesiaPermanentlyComplete = false;
        anesthesiaInProgress = false;
        anesthesiaTimer = 0f;
    }
}