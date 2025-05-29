using UnityEngine;

public class SocketPulseAnimation : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float transparency = 0.5f;         // 0 = fully transparent, 1 = fully opaque
    public Color baseColor = Color.red;        
    public Color glowColor = Color.white;      
    
    private MeshRenderer[] socketRenderers;
    private Material[] socketMaterials;
    
    private void Start()
    {
        socketRenderers = GetComponentsInChildren<MeshRenderer>();
        socketMaterials = new Material[socketRenderers.Length];
        
        // Store materials and make them transparent
        for (int i = 0; i < socketRenderers.Length; i++)
        {
            socketMaterials[i] = socketRenderers[i].material;
            
            // Enable transparency on the material
            EnableTransparency(socketMaterials[i]);
        }
        
        // Set initial colors
        UpdateColors();
    }
    
    private void EnableTransparency(Material material)
    {
        // Enable transparency for URP/Built-in pipeline
        material.SetFloat("_Mode", 3); // Transparent mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
    
    private void Update()
    {
        // Create smooth pulse between 0 and 1
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        
        // Interpolate between base color and glow color
        Color currentColor = Color.Lerp(baseColor, glowColor, pulse);
        Color currentEmission = Color.Lerp(baseColor * 0.2f, glowColor * 0.8f, pulse);
        
        // Apply transparency
        currentColor.a = transparency;
        
        // Apply to all materials
        for (int i = 0; i < socketMaterials.Length; i++)
        {
            if (socketMaterials[i] != null)
            {
                socketMaterials[i].color = currentColor;
                socketMaterials[i].SetColor("_EmissionColor", currentEmission);
            }
        }
    }
    
    // Call this method to change the glow colors and transparency
    public void SetGlowColors(Color newBaseColor, Color newGlowColor, float newTransparency = 0.6f)
    {
        baseColor = newBaseColor;
        glowColor = newGlowColor;
        transparency = newTransparency;
        UpdateColors();
    }
    
    // Call this to set glow colors automatically with transparency
    public void SetGlowColor(Color stateColor, float newTransparency = 0.6f)
    {
        baseColor = stateColor * 0.5f;  // Darker version
        glowColor = stateColor * 1.5f;  // Lighter version
        transparency = newTransparency;
        
        // Ensure colors don't go too dark or bright
        baseColor = new Color(
            Mathf.Clamp(baseColor.r, 0.1f, 1f),
            Mathf.Clamp(baseColor.g, 0.1f, 1f),
            Mathf.Clamp(baseColor.b, 0.1f, 1f),
            transparency
        );
        
        glowColor = new Color(
            Mathf.Clamp(glowColor.r, 0f, 1f),
            Mathf.Clamp(glowColor.g, 0f, 1f),
            Mathf.Clamp(glowColor.b, 0f, 1f),
            transparency
        );
        
        UpdateColors();
    }
    
    private void UpdateColors()
    {
        // Apply initial colors immediately
        for (int i = 0; i < socketMaterials.Length; i++)
        {
            if (socketMaterials[i] != null)
            {
                Color initialColor = baseColor;
                initialColor.a = transparency;
                
                socketMaterials[i].color = initialColor;
                socketMaterials[i].SetColor("_EmissionColor", baseColor * 0.2f);
            }
        }
    }
}