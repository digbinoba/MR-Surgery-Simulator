using UnityEngine;

public class FollowUserCanvas : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target; // Assign CenterEyeAnchor here
    public float followSpeed = 2f;
    public float rotationSpeed = 2f;
    public Vector3 offset = new Vector3(0, 0, 2f); // Distance from user
    
    [Header("Positioning")]
    public bool followPosition = true;
    public bool followRotation = true;
    public bool onlyYRotation = true; // Only rotate around Y axis for comfort
    
    [Header("Panel Position Presets")]
    public PanelPosition panelPosition = PanelPosition.Center;
    
    [Header("Controller Toggle")]
    public bool enableControllerToggle = true;
    public bool useLeftController = true; // X/Y buttons
    public bool useRightController = true; // A/B buttons
    
    private bool canvasVisible = true;
    private Canvas canvasComponent;
    
    public enum PanelPosition
    {
        Center,
        Left,
        Right,
        FarLeft,
        FarRight,
        TopLeft,
        TopRight
    }
    
    private void Start()
    {
        // Get canvas component
        canvasComponent = GetComponent<Canvas>();
        
        // Auto-find the camera if not assigned
        if (target == null)
        {
            GameObject cameraRig = GameObject.Find("OVRCameraRig");
            if (cameraRig != null)
            {
                target = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
            }
        }
        
        // Set offset based on panel position
        SetOffsetFromPosition();
    }
    
    private void SetOffsetFromPosition()
    {
        switch (panelPosition)
        {
            case PanelPosition.Center:
                offset = new Vector3(0, 0, 2f);
                break;
            case PanelPosition.Left:
                offset = new Vector3(-1f, 0, 2f);
                break;
            case PanelPosition.Right:
                offset = new Vector3(1f, 0, 2f);
                break;
            case PanelPosition.FarLeft:
                offset = new Vector3(-1.8f, 0, 2f);
                break;
            case PanelPosition.FarRight:
                offset = new Vector3(1.8f, 0, 2f);
                break;
            case PanelPosition.TopLeft:
                offset = new Vector3(-1f, 0.5f, 2f);
                break;
            case PanelPosition.TopRight:
                offset = new Vector3(1f, 0.5f, 2f);
                break;
        }
    }
    
    private void Update()
    {
        if (target == null) return;
        
        // Handle controller input for toggling canvas
        HandleControllerInput();
        
        // Only update position/rotation when canvas is visible
        if (!canvasVisible) return;
        
        // Follow position
        if (followPosition)
        {
            Vector3 targetPosition = target.position + target.TransformDirection(offset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        
        // Follow rotation
        if (followRotation)
        {
            Quaternion targetRotation;
            
            if (onlyYRotation)
            {
                // Only rotate around Y axis (prevents tilting)
                Vector3 direction = (transform.position - target.position).normalized;
                targetRotation = Quaternion.LookRotation(direction);
                targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
            }
            else
            {
                targetRotation = target.rotation;
            }
            
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void HandleControllerInput()
    {
        if (!enableControllerToggle) return;
        
        bool togglePressed = false;
        
        // Left Controller - X or Y button
        if (useLeftController)
        {
            if (OVRInput.GetDown(OVRInput.Button.Three) || // X button
                OVRInput.GetDown(OVRInput.Button.Four))    // Y button
            {
                togglePressed = true;
            }
        }
        
        // Right Controller - A or B button  
        if (useRightController)
        {
            if (OVRInput.GetDown(OVRInput.Button.One) ||  // A button
                OVRInput.GetDown(OVRInput.Button.Two))    // B button
            {
                togglePressed = true;
            }
        }
        
        if (togglePressed)
        {
            ToggleCanvas();
        }
    }
    
    public void ToggleCanvas()
    {
        canvasVisible = !canvasVisible;
        canvasComponent.enabled = canvasVisible;
        
        // Optional: Add fade effect or sound here
    }
    
    public void ShowCanvas()
    {
        canvasVisible = true;
        canvasComponent.enabled = true;
    }
    
    public void HideCanvas()
    {
        canvasVisible = false;
        canvasComponent.enabled = false;
    }
}