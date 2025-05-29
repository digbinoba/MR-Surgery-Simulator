using UnityEngine;

// Base class for all extraction states
public abstract class ExtractionStateBase : MonoBehaviour
{
    protected ToothExtractionSystem extractionSystem;
    
    public virtual void Initialize(ToothExtractionSystem system)
    {
        extractionSystem = system;
    }
    
    // Called when entering this state
    public virtual void OnEnterState()
    {
        if (extractionSystem.showDebugInfo)
        {
            Debug.Log($"Entering state: {GetType().Name}");
        }
    }
    
    // Called every frame while in this state
    public virtual void UpdateState()
    {
        // Override in derived classes
    }
    
    // Called when exiting this state
    public virtual void OnExitState()
    {
        if (extractionSystem.showDebugInfo)
        {
            Debug.Log($"Exiting state: {GetType().Name}");
        }
    }
    
    // Helper method to transition to another state
    protected void TransitionToState(ToothExtractionSystem.ExtractionState newState)
    {
        extractionSystem.TransitionToState(newState);
    }
    
    // Helper methods to access common extraction system properties
    protected GameObject TargetTooth => extractionSystem.targetTooth;
    protected bool ShowDebugInfo => extractionSystem.showDebugInfo;
    protected AudioSource AudioSource => extractionSystem.GetAudioSource();
}