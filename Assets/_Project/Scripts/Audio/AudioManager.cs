using System;
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private List<AK.Wwise.Bank> banks = new List<AK.Wwise.Bank>();

    void Awake()
    {
        for (int i = 0; i < banks.Count; i++)
        {
            if (banks[i] != null)
            {
                AKRESULT result = AkUnitySoundEngine.LoadBank(banks[i].ToString(), out uint bankID);
                if (result == AKRESULT.AK_Success)
                    Debug.Log($"{banks[i].ToString()} loaded successfully");
                else
                    Debug.LogError($"Failed to load {banks[i].ToString()}: {result}");
            }
            else
            {
                Debug.LogWarning($"Bank at index {i} is null, skipping load.");
            }
        }
    }

    void OnDestroy()
    {
        for (int i = 0;i < banks.Count;i++)
        {
            if (banks[i] != null)
            {
                AKRESULT result = AkUnitySoundEngine.UnloadBank(banks[i].ToString(), IntPtr.Zero);
                if (result == AKRESULT.AK_Success)
                    Debug.Log($"{banks[i].ToString()} unloaded successfully");
                else
                    Debug.LogError($"Failed to unload {banks[i].ToString()}: {result}");
            }
            else
            {
                Debug.LogWarning($"Bank at index {i} is null, skipping unload.");
            }
        }
    }
}