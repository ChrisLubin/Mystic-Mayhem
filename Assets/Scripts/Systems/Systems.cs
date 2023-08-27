using UnityEngine;

public class Systems : PersistentSingleton<Systems>
{
    protected override void Awake()
    {
        base.Awake();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Debug.isDebugBuild ? 45 : 120;
    }
}