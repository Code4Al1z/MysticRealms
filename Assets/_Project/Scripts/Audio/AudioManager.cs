using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private uint playerBankID = 0;

    void Start()
    {
        // Init.bnk is auto-loaded by Wwise, so just load Player_SFX
        AKRESULT playerResult = AkUnitySoundEngine.LoadBank("Player_SFX", out playerBankID);
        if (playerResult == AKRESULT.AK_Success)
            Debug.Log("Player_SFX.bnk loaded successfully");
        else
            Debug.LogError($"Failed to load Player_SFX.bnk: {playerResult}");
    }

    void OnDestroy()
    {
        if (playerBankID != 0)
        {
            AkUnitySoundEngine.UnloadBank(playerBankID, IntPtr.Zero);
            playerBankID = 0;
        }
    }
}