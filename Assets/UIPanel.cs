using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI instructionText; // Drag your TMP component here
    public Button actionButton; // Optional - your button if needed
    public GameObject panel; // The dark transparency panel
    
    [Header("UI Settings")]
    public bool autoFindComponents = true;
    
    private void Start()
    {
        if (autoFindComponents)
        {
            FindUIComponents();
        }
    }
    
    private void FindUIComponents()
    {
        // Auto-find components if not assigned
        if (instructionText == null)
        {
            instructionText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (actionButton == null)
        {
            actionButton = GetComponentInChildren<Button>();
        }
        
        if (panel == null)
        {
            // Look for a child GameObject named "Panel" or with Image component
            Transform panelTransform = transform.Find("Panel");
            if (panelTransform != null)
            {
                panel = panelTransform.gameObject;
            }
            else
            {
                // Fallback: find first child with Image component
                Image imageComponent = GetComponentInChildren<Image>();
                if (imageComponent != null)
                {
                    panel = imageComponent.gameObject;
                }
            }
        }
        
        // Debug log what was found
        Debug.Log($"UI Panel Setup - Instruction Text: {(instructionText != null ? "Found" : "Missing")}, " +
                  $"Button: {(actionButton != null ? "Found" : "Missing")}, " +
                  $"Panel: {(panel != null ? "Found" : "Missing")}");
    }
    
    /// <summary>
    /// Update the instruction text
    /// </summary>
    /// <param name="instruction">The instruction text to display</param>
    public void UpdateInstruction(string instruction)
    {
        if (instructionText != null)
        {
            instructionText.text = instruction;
            Debug.Log($"UI Updated: {instruction}");
        }
        else
        {
            Debug.LogWarning("Instruction text component not found!");
        }
    }
    
    /// <summary>
    /// Update instruction with progress information
    /// </summary>
    /// <param name="instruction">The instruction text</param>
    /// <param name="progress">Progress value between 0 and 1</param>
    public void UpdateProgress(string instruction, float progress)
    {
        if (instructionText != null)
        {
            // Add progress bar using text characters
            int progressChars = Mathf.RoundToInt(progress * 20); // 20 character progress bar
            string progressBar = "[" + new string('█', progressChars) + new string('░', 20 - progressChars) + "]";
            
            instructionText.text = $"{instruction}\n{progressBar} {(progress * 100):F0}%";
            Debug.Log($"UI Progress Updated: {instruction} - {(progress * 100):F0}%");
        }
        else
        {
            Debug.LogWarning("Instruction text component not found!");
        }
    }
    
    /// <summary>
    /// Show or hide the entire UI panel
    /// </summary>
    /// <param name="show">True to show, false to hide</param>
    public void SetPanelVisible(bool show)
    {
        gameObject.SetActive(show);
    }
    
    /// <summary>
    /// Enable or disable the action button
    /// </summary>
    /// <param name="enabled">True to enable, false to disable</param>
    public void SetButtonEnabled(bool enabled)
    {
        if (actionButton != null)
        {
            actionButton.interactable = enabled;
        }
    }
    
    /// <summary>
    /// Set the button text (if it has a Text component)
    /// </summary>
    /// <param name="buttonText">Text to display on button</param>
    public void SetButtonText(string buttonText)
    {
        if (actionButton != null)
        {
            TextMeshProUGUI buttonTextComponent = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonTextComponent != null)
            {
                buttonTextComponent.text = buttonText;
            }
            else
            {
                // Fallback to regular Text component
                Text regularText = actionButton.GetComponentInChildren<Text>();
                if (regularText != null)
                {
                    regularText.text = buttonText;
                }
            }
        }
    }
    
    /// <summary>
    /// Clear the instruction text
    /// </summary>
    public void ClearInstruction()
    {
        UpdateInstruction("");
    }
}