using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages surface-based audio responses for Wwise integration (Footstep System)
/// Reads surface type from Shader Graph enum and triggers appropriate Wwise events
/// Fully expandable via Inspector - dynamically supports any number of surface types
/// 
/// WWISE SETUP REQUIRED:
/// - Switch Group: "SurfaceType" with switches for each surface (e.g., Default, Wood, Metal, etc.)
/// - Events: "Player_Footstep", "Player_Land" (optionally "Player_Jump")
/// - Switch Containers for each event routing to correct surface sounds
/// 
/// EXPANDABILITY:
/// - Add new surface: Add switch to Wwise, add to surfaceSwitches list, add to surfaceMappings
/// - Remove surface: Remove from all three locations
/// - No code changes needed!
/// </summary>
public class SurfaceAudioManager : MonoBehaviour
{
    [Header("Shader Configuration")]
    [Tooltip("Name of the shader property that contains the surface type enum (e.g., '_SURFACETYPE')")]
    [SerializeField] private string shaderEnumPropertyName = "_SURFACETYPE";

    [Header("Switch Configuration - REQUIRED FOR WWISE")]
    [Tooltip("List of Wwise switches - one per surface type. Must match number of surface mappings.")]
    [SerializeField] private List<AK.Wwise.Switch> surfaceSwitches = new List<AK.Wwise.Switch>();

    [Header("Surface Audio Mappings")]
    [Tooltip("Map each surface type enum value to its corresponding Wwise events. Size MUST be 7.")]
    [SerializeField] private List<SurfaceAudioMapping> surfaceMappings = new List<SurfaceAudioMapping>();

    [Header("Global Audio Settings - Optional")]
    [Tooltip("Optional RTPC to set the current surface type as a numeric value (0-6)")]
    [SerializeField] private AK.Wwise.RTPC surfaceTypeRTPC;

    [Header("Debug")]
    [Tooltip("Enable debug logging to see surface type detection in Console")]
    [SerializeField] private bool enableDebugLog = false;

    // Current surface state
    private int currentSurfaceIndex = -1;
    private string currentSurfaceName = "Unknown";

    // Cache for performance
    private Dictionary<Collider, int> colliderSurfaceCache = new Dictionary<Collider, int>();
    private int shaderPropertyID;

    private void Awake()
    {
        // Convert shader property name to ID for faster lookups
        shaderPropertyID = Shader.PropertyToID(shaderEnumPropertyName);

        // Initialize default mappings if empty
        if (surfaceMappings.Count == 0)
        {
            InitializeDefaultMappings();
        }

        // Validate switch list matches mappings count
        if (surfaceSwitches.Count != surfaceMappings.Count)
        {
            Debug.LogWarning($"[SurfaceAudioManager] surfaceSwitches count ({surfaceSwitches.Count}) does not match surfaceMappings count ({surfaceMappings.Count}). " +
                           "Make sure to assign one switch per surface mapping.");
        }

        // Check for null switches
        for (int i = 0; i < surfaceSwitches.Count; i++)
        {
            if (surfaceSwitches[i] == null)
            {
                Debug.LogWarning($"[SurfaceAudioManager] surfaceSwitches[{i}] is null. Assign all switches in Inspector.");
            }
        }
    }

    /// <summary>
    /// Updates the current surface based on the collider the player is touching
    /// Call this from PlayerController when detecting ground contact
    /// </summary>
    public void UpdateCurrentSurface(Collider hitCollider)
    {
        if (hitCollider == null) return;

        // Check cache first for performance
        if (colliderSurfaceCache.TryGetValue(hitCollider, out int cachedSurfaceIndex))
        {
            SetCurrentSurface(cachedSurfaceIndex);
            return;
        }

        // Get material from collider's renderer
        Renderer renderer = hitCollider.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null)
        {
            if (enableDebugLog)
                Debug.LogWarning($"[SurfaceAudio] No renderer or material found on {hitCollider.name}");
            return;
        }

        Material material = renderer.sharedMaterial;

        // Try to get the enum value from the shader
        if (material.HasProperty(shaderPropertyID))
        {
            // Get the enum as a float (Unity stores enums as floats in shaders)
            float enumValue = material.GetFloat(shaderPropertyID);
            int surfaceIndex = Mathf.RoundToInt(enumValue);

            // Cache the result
            colliderSurfaceCache[hitCollider] = surfaceIndex;

            SetCurrentSurface(surfaceIndex);
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning($"[SurfaceAudio] Material '{material.name}' does not have property '{shaderEnumPropertyName}'");
        }
    }

    /// <summary>
    /// Manually set the current surface by index
    /// Automatically sets the Wwise switch for Switch Container routing
    /// </summary>
    public void SetCurrentSurface(int surfaceIndex)
    {
        if (currentSurfaceIndex == surfaceIndex) return;

        currentSurfaceIndex = surfaceIndex;

        // Find matching mapping
        SurfaceAudioMapping mapping = GetMappingForIndex(surfaceIndex);
        currentSurfaceName = mapping != null ? mapping.surfaceName : "Unknown";

        // CRITICAL: Set the Wwise switch (for Switch Container approach)
        if (surfaceIndex >= 0 && surfaceIndex < surfaceSwitches.Count)
        {
            if (surfaceSwitches[surfaceIndex] != null)
            {
                surfaceSwitches[surfaceIndex].SetValue(gameObject);

                if (enableDebugLog)
                    Debug.Log($"[Wwise] Switch set to: {surfaceSwitches[surfaceIndex].Name}");
            }
            else
            {
                Debug.LogWarning($"[SurfaceAudio] surfaceSwitches[{surfaceIndex}] is not assigned! Assign it in Inspector.");
            }
        }
        else if (surfaceIndex >= surfaceSwitches.Count)
        {
            Debug.LogWarning($"[SurfaceAudio] Surface index {surfaceIndex} is out of range. surfaceSwitches has {surfaceSwitches.Count} elements. Add more switches or check shader enum values.");
        }

        // Optional: Update Wwise RTPC if assigned
        if (surfaceTypeRTPC != null)
        {
            surfaceTypeRTPC.SetGlobalValue(surfaceIndex);
        }

        if (enableDebugLog)
            Debug.Log($"[SurfaceAudio] Surface changed to: {currentSurfaceName} (Index: {surfaceIndex})");
    }

    /// <summary>
    /// Called when player takes a footstep - triggers surface-specific footstep sound
    /// Uses EITHER the assigned event in surfaceMappings OR relies on Switch Container
    /// </summary>
    public void OnFootstep(GameObject emitter)
    {
        SurfaceAudioMapping mapping = GetMappingForIndex(currentSurfaceIndex);
        if (mapping != null && mapping.footstepEvent != null)
        {
            mapping.footstepEvent.Post(emitter);

            if (enableDebugLog)
                Debug.Log($"[SurfaceAudio] Footstep sound triggered for {currentSurfaceName}");
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"[SurfaceAudio] No footstep event assigned for surface: {currentSurfaceName}");
        }
    }

    /// <summary>
    /// Called when player jumps - triggers surface-specific jump sound
    /// OPTIONAL: Only used if you have jump sounds. Can skip for minimal version.
    /// </summary>
    public void OnJump(GameObject emitter)
    {
        SurfaceAudioMapping mapping = GetMappingForIndex(currentSurfaceIndex);
        if (mapping != null && mapping.jumpEvent != null)
        {
            mapping.jumpEvent.Post(emitter);

            if (enableDebugLog)
                Debug.Log($"[SurfaceAudio] Jump sound triggered for {currentSurfaceName}");
        }
    }

    /// <summary>
    /// Called when player lands - triggers surface-specific landing sound
    /// </summary>
    public void OnLand(GameObject emitter)
    {
        SurfaceAudioMapping mapping = GetMappingForIndex(currentSurfaceIndex);
        if (mapping != null && mapping.landEvent != null)
        {
            mapping.landEvent.Post(emitter);

            if (enableDebugLog)
                Debug.Log($"[SurfaceAudio] Land sound triggered for {currentSurfaceName}");
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"[SurfaceAudio] No land event assigned for surface: {currentSurfaceName}");
        }
    }

    /// <summary>
    /// Get the mapping that matches the current surface index
    /// </summary>
    private SurfaceAudioMapping GetMappingForIndex(int index)
    {
        foreach (SurfaceAudioMapping mapping in surfaceMappings)
        {
            if (mapping.surfaceEnumIndex == index)
            {
                return mapping;
            }
        }
        return null;
    }

    /// <summary>
    /// Get current surface name (useful for debugging or UI)
    /// </summary>
    public string GetCurrentSurfaceName()
    {
        return currentSurfaceName;
    }

    /// <summary>
    /// Get current surface index (0-6)
    /// </summary>
    public int GetCurrentSurfaceIndex()
    {
        return currentSurfaceIndex;
    }

    /// <summary>
    /// Clear the collider cache (useful if materials change at runtime)
    /// </summary>
    public void ClearCache()
    {
        colliderSurfaceCache.Clear();
        if (enableDebugLog)
            Debug.Log("[SurfaceAudio] Cache cleared");
    }

    /// <summary>
    /// Initialize with default surface types from your shader
    /// Creates empty mappings that you fill in the Inspector
    /// </summary>
    private void InitializeDefaultMappings()
    {
        // Create mappings for the 7 surface types in your shader
        string[] defaultSurfaces = { "Default", "Wood", "Metal", "Stone", "Leaves", "Grass", "Soil" };

        for (int i = 0; i < defaultSurfaces.Length; i++)
        {
            surfaceMappings.Add(new SurfaceAudioMapping
            {
                surfaceName = defaultSurfaces[i],
                surfaceEnumIndex = i,
                footstepEvent = null,
                jumpEvent = null,
                landEvent = null
            });
        }

        if (enableDebugLog)
            Debug.Log($"[SurfaceAudio] Initialized {defaultSurfaces.Length} default surface mappings");
    }

    /// <summary>
    /// Debug visualization in Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!enableDebugLog) return;

        // Display current surface info in Scene view
#if UNITY_EDITOR
        if (currentSurfaceIndex >= 0)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Surface: {currentSurfaceName} ({currentSurfaceIndex})");
        }
#endif
    }
}

/// <summary>
/// Serializable class that maps a surface type to its Wwise events
/// Fully editable in the Inspector
/// 
/// FOR SWITCH CONTAINER APPROACH (RECOMMENDED):
/// - Assign the SAME event to all 7 surfaces (e.g., all use "Player_Footstep")
/// - The Switch Container in Wwise routes to correct sound based on active switch
/// 
/// FOR INDIVIDUAL EVENTS APPROACH:
/// - Assign different events per surface (e.g., "Player_Footstep_Wood", "Player_Footstep_Metal", etc.)
/// </summary>
[System.Serializable]
public class SurfaceAudioMapping
{
    [Header("Surface Identification")]
    [Tooltip("Human-readable name (e.g., 'Wood', 'Metal'). Must match shader enum!")]
    public string surfaceName = "Unnamed Surface";

    [Tooltip("Enum index from shader (0=Default, 1=Wood, 2=Metal, 3=Stone, 4=Leaves, 5=Grass, 6=Soil)")]
    public int surfaceEnumIndex = 0;

    [Header("Wwise Events")]
    [Tooltip("Wwise event for footsteps. For Switch Container: Use 'Player_Footstep' for ALL surfaces")]
    public AK.Wwise.Event footstepEvent;

    [Tooltip("Wwise event for jumps (OPTIONAL). For Switch Container: Use 'Player_Jump' for ALL surfaces")]
    public AK.Wwise.Event jumpEvent;

    [Tooltip("Wwise event for lands. For Switch Container: Use 'Player_Land' for ALL surfaces")]
    public AK.Wwise.Event landEvent;
}