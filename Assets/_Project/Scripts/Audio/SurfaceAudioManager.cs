using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages surface-based audio responses for Wwise integration (Footstep System)
/// Reads surface type from Shader Graph enum and triggers appropriate Wwise events
/// Fully expandable via Inspector - no hardcoded surface types
/// </summary>
public class SurfaceAudioManager : MonoBehaviour
{
    [Header("Shader Configuration")]
    [Tooltip("Name of the shader property that contains the surface type enum (e.g., '_SURFACETYPE')")]
    public string shaderEnumPropertyName = "_SURFACETYPE";

    [Header("Surface Audio Mappings")]
    [Tooltip("Map each surface type enum value to its corresponding Wwise events")]
    public List<SurfaceAudioMapping> surfaceMappings = new List<SurfaceAudioMapping>();

    [Header("Switch Configuration (If Using Switch Containers)")]
    [Tooltip("Array of Wwise switches - one per surface type (optional, for Switch Container approach)")]
    public AK.Wwise.Switch[] surfaceSwitches = new AK.Wwise.Switch[7];

    [Header("Global Audio Settings")]
    [Tooltip("Optional RTPC to set the current surface type as a numeric value")]
    public AK.Wwise.RTPC surfaceTypeRTPC;

    [Tooltip("Enable debug logging to see surface type detection")]
    public bool enableDebugLog = false;

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
    /// </summary>
    public void SetCurrentSurface(int surfaceIndex)
    {
        if (currentSurfaceIndex == surfaceIndex) return;

        currentSurfaceIndex = surfaceIndex;

        // Find matching mapping
        SurfaceAudioMapping mapping = GetMappingForIndex(surfaceIndex);
        currentSurfaceName = mapping != null ? mapping.surfaceName : "Unknown";

        // Set the Wwise switch (if using Switch Container approach)
        if (surfaceIndex >= 0 && surfaceIndex < surfaceSwitches.Length)
        {
            if (surfaceSwitches[surfaceIndex] != null)
            {
                surfaceSwitches[surfaceIndex].SetValue(gameObject);

                if (enableDebugLog)
                    Debug.Log($"[Wwise] Switch set to: {surfaceSwitches[surfaceIndex].Name}");
            }
        }

        // Update Wwise RTPC if assigned
        if (surfaceTypeRTPC != null)
        {
            surfaceTypeRTPC.SetGlobalValue(surfaceIndex);
        }

        if (enableDebugLog)
            Debug.Log($"[SurfaceAudio] Surface changed to: {currentSurfaceName} (Index: {surfaceIndex})");
    }

    /// <summary>
    /// Called when player takes a footstep - triggers surface-specific footstep sound
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
    }

    /// <summary>
    /// Called when player jumps - triggers surface-specific jump sound
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
    /// Get current surface index
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
    /// This creates empty mappings that you can fill in the Inspector
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
/// </summary>
[System.Serializable]
public class SurfaceAudioMapping
{
    [Header("Surface Identification")]
    [Tooltip("Human-readable name for this surface (e.g., 'Wood', 'Metal')")]
    public string surfaceName = "Unnamed Surface";

    [Tooltip("The enum index value from the shader (0 = Default, 1 = Wood, 2 = Metal, etc.)")]
    public int surfaceEnumIndex = 0;

    [Header("Wwise Events - Footstep System")]
    [Tooltip("Wwise event to play for footsteps on this surface")]
    public AK.Wwise.Event footstepEvent;

    [Tooltip("Wwise event to play when jumping from this surface")]
    public AK.Wwise.Event jumpEvent;

    [Tooltip("Wwise event to play when landing on this surface")]
    public AK.Wwise.Event landEvent;

    [Header("Optional: Surface-Specific RTPCs")]
    [Tooltip("Optional RTPC to control when on this surface (e.g., footstep pitch)")]
    public AK.Wwise.RTPC surfaceSpecificRTPC;

    [Tooltip("Value to set the RTPC to when on this surface")]
    public float rtpcValue = 0f;
}