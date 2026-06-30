using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages surface-based audio responses for Wwise integration (Footstep System)
/// Reads surface type from Shader Graph enum and triggers appropriate Wwise events.
///
/// Safe to use on multiple GameObjects simultaneously (player + golem etc).
/// All Wwise switch state is scoped to the emitter GameObject — no global bleed.
///
/// WWISE SETUP REQUIRED:
/// - Switch Group: "SurfaceType" with switches for each surface (Default, Wood, Metal, etc.)
/// - Events: one footstep event per character type (e.g. Play_Player_Footstep, Play_Golem_Footstep)
/// - Switch Containers for each event routing to correct surface sounds
///
/// EXPANDABILITY:
/// - Add new surface: Add switch to Wwise, add to surfaceSwitches list, add to surfaceMappings
/// - No code changes needed
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
    [Tooltip("Map each surface type enum value to its corresponding Wwise events.")]
    [SerializeField] private List<SurfaceAudioMapping> surfaceMappings = new List<SurfaceAudioMapping>();

    [Header("Debug")]
    [Tooltip("Enable debug logging to see surface type detection in Console")]
    [SerializeField] private bool enableDebugLog = false;

    // Current surface state — local to this instance, never shared
    private int currentSurfaceIndex = -1;
    private string currentSurfaceName = "Unknown";

    // The emitter this manager drives — set via Initialise() or falls back to own GameObject
    private GameObject emitter;

    // Cache for performance
    private Dictionary<Collider, int> colliderSurfaceCache = new Dictionary<Collider, int>();
    private int shaderPropertyID;

    private void Awake()
    {
        shaderPropertyID = Shader.PropertyToID(shaderEnumPropertyName);

        if (surfaceMappings.Count == 0)
            InitializeDefaultMappings();

        if (surfaceSwitches.Count != surfaceMappings.Count)
        {
            Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: surfaceSwitches count " +
                $"({surfaceSwitches.Count}) does not match surfaceMappings count ({surfaceMappings.Count}).");
        }

        // Default emitter is own GameObject — overridden by Initialise() if needed
        emitter = gameObject;
    }

    /// <summary>
    /// Call this from GolemFootstepHandler.Initialise() to tell the manager
    /// which GameObject to post events and set switches on.
    /// Not needed for the player since the manager sits on the player directly.
    /// </summary>
    public void Initialise(GameObject soundEmitter)
    {
        emitter = soundEmitter;
    }

    public void UpdateCurrentSurface(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            if (enableDebugLog)
                Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: UpdateCurrentSurface called with null collider.");
            return;
        }

        if (colliderSurfaceCache.TryGetValue(hitCollider, out int cachedSurfaceIndex))
        {
            SetCurrentSurface(cachedSurfaceIndex);
            return;
        }

        Renderer renderer = hitCollider.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null)
        {
            if (enableDebugLog)
                Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: No renderer/material on {hitCollider.name}. Defaulting to surface 0.");
            SetCurrentSurface(0);
            return;
        }

        Material material = renderer.sharedMaterial;

        if (material.HasProperty(shaderPropertyID))
        {
            float enumValue = material.GetFloat(shaderPropertyID);
            int surfaceIndex = Mathf.RoundToInt(enumValue);
            colliderSurfaceCache[hitCollider] = surfaceIndex;
            SetCurrentSurface(surfaceIndex);

            if (enableDebugLog)
                Debug.Log($"[SurfaceAudioManager] on {gameObject.name}: Surface detected: {surfaceIndex} from {hitCollider.name}");
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: Material '{material.name}' missing property '{shaderEnumPropertyName}'. Defaulting to 0.");
            SetCurrentSurface(0);
        }
    }

    /// <summary>
    /// Sets the current surface index and updates the Wwise switch on the emitter.
    /// Scoped entirely to the emitter — never touches global state.
    /// </summary>
    public void SetCurrentSurface(int surfaceIndex)
    {
        if (currentSurfaceIndex == surfaceIndex) return;

        currentSurfaceIndex = surfaceIndex;

        SurfaceAudioMapping mapping = GetMappingForIndex(surfaceIndex);
        currentSurfaceName = mapping != null ? mapping.surfaceName : "Unknown";

        // Set the switch on the emitter only — this is what prevents cross-contamination
        // between the player SurfaceAudioManager and the golem SurfaceAudioManager.
        // Each emitter GameObject holds its own Wwise switch state independently.
        if (surfaceIndex >= 0 && surfaceIndex < surfaceSwitches.Count)
        {
            if (surfaceSwitches[surfaceIndex] != null)
            {
                surfaceSwitches[surfaceIndex].SetValue(emitter);

                if (enableDebugLog)
                    Debug.Log($"[SurfaceAudioManager] on {gameObject.name}: Switch set to '{surfaceSwitches[surfaceIndex].Name}' on emitter '{emitter.name}'");
            }
            else
            {
                Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: surfaceSwitches[{surfaceIndex}] is not assigned.");
            }
        }
        else if (surfaceIndex >= surfaceSwitches.Count)
        {
            Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: Surface index {surfaceIndex} out of range. " +
                $"surfaceSwitches has {surfaceSwitches.Count} elements.");
        }

        if (enableDebugLog)
            Debug.Log($"[SurfaceAudioManager] on {gameObject.name}: Surface → {currentSurfaceName} (index {surfaceIndex})");
    }

    /// <summary>
    /// Called when a footstep should fire. Pass the collider that was just
    /// stepped on so the surface is resolved and the switch set in the same
    /// call as the post — this guarantees the sound matches the surface under
    /// the foot at the exact moment of contact, even on the first step onto
    /// a new surface. If hitCollider is omitted, falls back to whatever
    /// surface was last set via UpdateCurrentSurface.
    /// </summary>
    public void OnFootstep(GameObject eventEmitter, Collider hitCollider = null)
    {
        if (hitCollider != null)
            UpdateCurrentSurface(hitCollider);

        ApplySwitchToEmitter(eventEmitter);

        SurfaceAudioMapping mapping = GetMappingForIndex(currentSurfaceIndex);
        if (mapping != null && mapping.footstepEvent != null)
        {
            mapping.footstepEvent.Post(eventEmitter);

            if (enableDebugLog)
                Debug.Log($"[SurfaceAudioManager] on {gameObject.name}: Footstep posted for '{currentSurfaceName}' on '{eventEmitter.name}'");
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: No footstep event for surface '{currentSurfaceName}'");
        }
    }

    /// <summary>
    /// Called when the character jumps. Pass the collider being jumped from
    /// so the surface resolves correctly even on the first jump from a new surface.
    /// </summary>
    public void OnJump(GameObject eventEmitter, Collider hitCollider = null)
    {
        if (hitCollider != null)
            UpdateCurrentSurface(hitCollider);

        ApplySwitchToEmitter(eventEmitter);

        SurfaceAudioMapping mapping = GetMappingForIndex(currentSurfaceIndex);
        if (mapping != null && mapping.jumpEvent != null)
            mapping.jumpEvent.Post(eventEmitter);
    }

    /// <summary>
    /// Called when the character lands. Pass the collider that was landed on
    /// so the surface resolves and switch is set in the same call as the post —
    /// this is what fixes the "first landing on a new surface plays the old
    /// surface's sound" bug, since the switch is no longer set on a separate,
    /// earlier call than the event post.
    /// </summary>
    public void OnLand(GameObject eventEmitter, Collider hitCollider = null)
    {
        if (hitCollider != null)
            UpdateCurrentSurface(hitCollider);

        ApplySwitchToEmitter(eventEmitter);

        SurfaceAudioMapping mapping = GetMappingForIndex(currentSurfaceIndex);
        if (mapping != null && mapping.landEvent != null)
        {
            mapping.landEvent.Post(eventEmitter);

            if (enableDebugLog)
                Debug.Log($"[SurfaceAudioManager] on {gameObject.name}: Land posted for '{currentSurfaceName}' on '{eventEmitter.name}'");
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"[SurfaceAudioManager] on {gameObject.name}: No land event for surface '{currentSurfaceName}'");
        }
    }

    /// <summary>
    /// Clears the collider-to-surface cache. Call this if materials change at runtime.
    /// </summary>
    public void ClearCache()
    {
        colliderSurfaceCache.Clear();
        if (enableDebugLog)
            Debug.Log($"[SurfaceAudioManager] on {gameObject.name}: Cache cleared.");
    }

    public string GetCurrentSurfaceName() => currentSurfaceName;
    public int GetCurrentSurfaceIndex() => currentSurfaceIndex;

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Re-applies the current surface switch to a specific emitter.
    /// Called before each event post to guarantee correctness.
    /// </summary>
    private void ApplySwitchToEmitter(GameObject target)
    {
        if (currentSurfaceIndex < 0 || currentSurfaceIndex >= surfaceSwitches.Count) return;
        if (surfaceSwitches[currentSurfaceIndex] == null) return;
        surfaceSwitches[currentSurfaceIndex].SetValue(target);
    }

    private SurfaceAudioMapping GetMappingForIndex(int index)
    {
        foreach (SurfaceAudioMapping mapping in surfaceMappings)
        {
            if (mapping.surfaceEnumIndex == index)
                return mapping;
        }
        return null;
    }

    private void InitializeDefaultMappings()
    {
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
            Debug.Log($"[SurfaceAudioManager] on {gameObject.name}: Initialised {defaultSurfaces.Length} default surface mappings.");
    }

    private void OnDrawGizmos()
    {
        if (!enableDebugLog) return;
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
/// Maps a surface type index to its Wwise events.
/// Assign the same footstep event to all surfaces when using Switch Containers.
/// </summary>
[System.Serializable]
public class SurfaceAudioMapping
{
    [Header("Surface Identification")]
    [Tooltip("Human-readable name matching the shader enum value.")]
    public string surfaceName = "Unnamed Surface";

    [Tooltip("Enum index from shader (0=Default, 1=Wood, 2=Metal, 3=Stone, 4=Leaves, 5=Grass, 6=Soil)")]
    public int surfaceEnumIndex = 0;

    [Header("Wwise Events")]
    [Tooltip("Footstep event. For Switch Containers use the same event on all surfaces.")]
    public AK.Wwise.Event footstepEvent;

    [Tooltip("Jump event (optional).")]
    public AK.Wwise.Event jumpEvent;

    [Tooltip("Land event.")]
    public AK.Wwise.Event landEvent;
}