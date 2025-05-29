using UnityEngine;

public class ExtractionToothState : ExtractionStateBase
{
    private bool extractionStarted = false; // Add this flag
    public override void OnEnterState()
    {
        base.OnEnterState();
        //extractionStarted = false;
        // Update UI
        if (extractionSystem.uiPanel != null)
        {
            extractionSystem.uiPanel.UpdateInstruction("Step 2: Use dental forceps to extract the highlighted tooth");
        }
    }
    
    public override void UpdateState()
    {
        // Debug log to see if this state is being called
        Debug.Log("ExtractionToothState.UpdateState() called");
        
        /*// Check if anesthesia was completed (if required)
        AnesthesiaState anesthesiaState = extractionSystem.GetState<AnesthesiaState>();
        if (anesthesiaState != null && !anesthesiaState.IsAnesthesiaComplete())
        {
            // Force back to anesthesia if not completed
            Debug.Log("Anesthesia not complete, returning to anesthesia state");
            TransitionToState(ToothExtractionSystem.ExtractionState.AnesthesiaRequired);
            return;
        }*/
        Debug.Log("Anesthesia complete, checking for forceps...");
        CheckForForcepsProximity();
    }
    
    private void CheckForForcepsProximity()
    {
        GameObject forceps = extractionSystem.FindForcepsInHands();
        Debug.Log($"Forceps found: {(forceps != null ? forceps.name : "None")}");
    
        if (forceps != null && TargetTooth != null)
        {
            float distance = Vector3.Distance(forceps.transform.position, TargetTooth.transform.position);
            Debug.Log($"Forceps distance to tooth: {distance:F3}m");
        
            if (distance <= extractionSystem.extractionDistance)
            {
                Debug.Log("Forceps in range! Transitioning to ForcepsNearTooth state");
            
                // Update UI
                if (extractionSystem.uiPanel != null)
                {
                    extractionSystem.uiPanel.UpdateInstruction("Hold forceps on tooth to extract...");
                }
            
                // Use TransitionToState to properly clear the handler
                TransitionToState(ToothExtractionSystem.ExtractionState.ForcepsNearTooth);
                Debug.Log($"Transitioned to: {extractionSystem.currentState}");
            }
        }
    }
}