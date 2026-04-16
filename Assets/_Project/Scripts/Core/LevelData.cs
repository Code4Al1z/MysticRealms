using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Mystic Realms/Level Data")]
public class LevelData : ScriptableObject
{
    public string levelName = "Level 1";
    public string nextSceneName = "";
    public int requiredCollectables = 10;
}