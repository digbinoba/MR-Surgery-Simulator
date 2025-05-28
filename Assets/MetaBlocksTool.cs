using UnityEngine;
using Oculus.Interaction;

public class MetaBlocksTool : MonoBehaviour
{
    [Header("Tool Configuration")]
    public string toolName = "Dental Tool";
    public ToolType toolType = ToolType.DentalExplorer;
    
    [Header("Visual Feedback")]
    public bool showOutlineWhenHovered = true;
    public float outlineScale = 1.02f;
    
    private MetaAutoGrabToolSystem toolSystem;
    private GameObject outlineObject;
    private bool isGrabbed = false;
    private bool isHovered = false;
    private Grabbable grabbableComponent;
    private Rigidbody toolRigidbody;
    
    public enum ToolType
    {
        DentalExplorer,
        DentalDrill,
        DentalForceps,
        Scalpel,
        Syringe,
        DentalMirror,
        Excavator
    }
    
    private void Awake()
    {
        // Get components
        grabbableComponent = GetComponent<Grabbable>();
        toolRigidbody = GetComponent<Rigidbody>();
        
        // Auto-detect tool type from name
        AutoDetectToolType();
    }
    
    private void AutoDetectToolType()
    {
        string objName = gameObject.name.ToLower();
        
        if (objName.Contains("explorer") || objName.Contains("probe"))
            toolType = ToolType.DentalExplorer;
        else if (objName.Contains("drill"))
            toolType = ToolType.DentalDrill;
        else if (objName.Contains("forceps") || objName.Contains("pliers"))
            toolType = ToolType.DentalForceps;
        else if (objName.Contains("scalpel") || objName.Contains("blade"))
            toolType = ToolType.Scalpel;
        else if (objName.Contains("syringe") || objName.Contains("needle"))
            toolType = ToolType.Syringe;
        else if (objName.Contains("mirror"))
            toolType = ToolType.DentalMirror;
        else if (objName.Contains("excavator"))
            toolType = ToolType.Excavator;
        
        if (string.IsNullOrEmpty(toolName) || toolName == "Dental Tool")
        {
            toolName = gameObject.name;
        }
    }
    
    // public void Initialize(MetaAutoGrabToolSystem system)
    // {
    //     toolSystem = system;
    //     
    //     // Create outline for hover effect
    //     if (showOutlineWhenHovered)
    //     {
    //         CreateOutlineObject();
    //     }
    //     
    //     Debug.Log($"Initialized Meta Blocks Tool: {toolName} ({toolType})");
    // }
    
    // Overloaded Initialize method for the new system
    public void Initialize(MetaAutoGrabToolSystem autoSystem)
    {
        // Create outline for hover effect
        if (showOutlineWhenHovered)
        {
            CreateOutlineObjectForAutoSystem(autoSystem);
        }
        
        Debug.Log($"Initialized Meta Auto Grab Tool: {toolName} ({toolType})");
    }
    
    private void CreateOutlineObjectForAutoSystem(MetaAutoGrabToolSystem autoSystem)
    {
        // Create outline parent
        outlineObject = new GameObject($"{toolName}_Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * outlineScale;
        
        // Copy all mesh renderers for outline
        MeshRenderer[] originalRenderers = GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer originalRenderer in originalRenderers)
        {
            if (originalRenderer.transform.IsChildOf(outlineObject.transform)) continue;
            
            // Create outline copy
            GameObject outlineCopy = new GameObject($"Outline_{originalRenderer.name}");
            outlineCopy.transform.SetParent(outlineObject.transform);
            
            // Match transform relative to outline parent
            Vector3 relativePos = outlineObject.transform.InverseTransformPoint(originalRenderer.transform.position);
            Quaternion relativeRot = Quaternion.Inverse(outlineObject.transform.rotation) * originalRenderer.transform.rotation;
            
            outlineCopy.transform.localPosition = relativePos;
            outlineCopy.transform.localRotation = relativeRot;
            outlineCopy.transform.localScale = originalRenderer.transform.localScale;
            
            // Copy mesh
            MeshFilter originalMeshFilter = originalRenderer.GetComponent<MeshFilter>();
            if (originalMeshFilter != null && originalMeshFilter.mesh != null)
            {
                MeshFilter outlineMeshFilter = outlineCopy.AddComponent<MeshFilter>();
                outlineMeshFilter.mesh = originalMeshFilter.mesh;
                
                MeshRenderer outlineRenderer = outlineCopy.AddComponent<MeshRenderer>();
                outlineRenderer.material = autoSystem.GetOutlineMaterial();
                outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
            }
        }
        
        // Start with outline disabled
        outlineObject.SetActive(false);
    }
    
    private void CreateOutlineObject()
    {
        // Create outline parent
        outlineObject = new GameObject($"{toolName}_Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * outlineScale;
        
        // Copy all mesh renderers for outline
        MeshRenderer[] originalRenderers = GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer originalRenderer in originalRenderers)
        {
            if (originalRenderer.transform.IsChildOf(outlineObject.transform)) continue;
            
            // Create outline copy
            GameObject outlineCopy = new GameObject($"Outline_{originalRenderer.name}");
            outlineCopy.transform.SetParent(outlineObject.transform);
            
            // Match transform relative to outline parent
            Vector3 relativePos = outlineObject.transform.InverseTransformPoint(originalRenderer.transform.position);
            Quaternion relativeRot = Quaternion.Inverse(outlineObject.transform.rotation) * originalRenderer.transform.rotation;
            
            outlineCopy.transform.localPosition = relativePos;
            outlineCopy.transform.localRotation = relativeRot;
            outlineCopy.transform.localScale = originalRenderer.transform.localScale;
            
            // Copy mesh
            MeshFilter originalMeshFilter = originalRenderer.GetComponent<MeshFilter>();
            if (originalMeshFilter != null && originalMeshFilter.mesh != null)
            {
                MeshFilter outlineMeshFilter = outlineCopy.AddComponent<MeshFilter>();
                outlineMeshFilter.mesh = originalMeshFilter.mesh;
                
                MeshRenderer outlineRenderer = outlineCopy.AddComponent<MeshRenderer>();
                outlineRenderer.material = toolSystem.GetOutlineMaterial();
                outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
            }
        }
        
        // Start with outline disabled
        outlineObject.SetActive(false);
    }
    
    public void OnHovered(bool hovered)
    {
        isHovered = hovered;
        
        // Show/hide outline
        if (outlineObject != null)
        {
            outlineObject.SetActive(hovered && !isGrabbed);
        }
        
        // Optional: Add other hover effects
        if (hovered && !isGrabbed)
        {
            StartCoroutine(HoverPulseEffect());
        }
    }
    
    private System.Collections.IEnumerator HoverPulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.05f;
        float duration = 0.3f;
        float elapsed = 0f;
        
        // Scale up
        while (elapsed < duration && isHovered && !isGrabbed)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < duration && isHovered && !isGrabbed)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }
        
        // Reset scale
        if (!isGrabbed)
        {
            transform.localScale = originalScale;
        }
    }
    
    public void OnGrabbed()
    {
        isGrabbed = true;
        isHovered = false;
        
        // Hide outline
        if (outlineObject != null)
        {
            outlineObject.SetActive(false);
        }
        
        // Configure physics for being held
        if (toolRigidbody != null)
        {
            toolRigidbody.useGravity = false;
            // Meta's interaction system will handle kinematic state
        }
        
        // Tool-specific grab behavior
        HandleToolSpecificGrab();
        
        Debug.Log($"Meta Blocks Tool grabbed: {toolName}");
    }
    
    public void OnReleased()
    {
        isGrabbed = false;
        
        // Configure physics for being dropped
        if (toolRigidbody != null)
        {
            toolRigidbody.useGravity = true;
            // Meta's interaction system will handle kinematic state
        }
        
        // Tool-specific release behavior
        HandleToolSpecificRelease();
        
        Debug.Log($"Meta Blocks Tool released: {toolName}");
    }
    
    private void HandleToolSpecificGrab()
    {
        switch (toolType)
        {
            case ToolType.DentalDrill:
                // Could start drill sound/animation
                break;
            case ToolType.Syringe:
                // Could show fluid level
                break;
            case ToolType.DentalMirror:
                // Could enable reflection effect
                break;
        }
    }
    
    private void HandleToolSpecificRelease()
    {
        switch (toolType)
        {
            case ToolType.DentalDrill:
                // Stop drill effects
                break;
        }
    }
    
    // Public getters
    public bool IsGrabbed() => isGrabbed;
    public bool IsHovered() => isHovered;
    public ToolType GetToolType() => toolType;
    public string GetToolName() => toolName;
    public Grabbable GetGrabbable() => grabbableComponent;
    
    // Utility methods for dental system integration
    public bool IsToolType(ToolType type)
    {
        return toolType == type;
    }
    
    public bool IsAnyOfToolTypes(params ToolType[] types)
    {
        foreach (ToolType type in types)
        {
            if (toolType == type) return true;
        }
        return false;
    }
    
    // Quick setup methods
    [ContextMenu("Setup as Dental Explorer")]
    private void SetupAsDentalExplorer()
    {
        toolType = ToolType.DentalExplorer;
        toolName = "Dental Explorer";
        gameObject.name = "DentalExplorer";
    }
    
    [ContextMenu("Setup as Dental Drill")]
    private void SetupAsDentalDrill()
    {
        toolType = ToolType.DentalDrill;
        toolName = "Dental Drill";
        gameObject.name = "DentalDrill";
    }
    
    [ContextMenu("Setup as Forceps")]
    private void SetupAsForceps()
    {
        toolType = ToolType.DentalForceps;
        toolName = "Dental Forceps";
        gameObject.name = "DentalForceps";
    }
}