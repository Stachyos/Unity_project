using UnityEngine;

public class FadeManagerLoader
{

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadFadeManager()
    {
        // As soon as the project is launched, prefabs will be loaded from Resources and instantiated.
        GameObject prefab = Resources.Load<GameObject>("FadeManagerPrefab");
        if (prefab != null)
        {
            GameObject.Instantiate(prefab);
            //At this point, the Awake() method of the FadeManagerPrefab will be executed immediately,
            //and the alpha value of the mask CanvasGroup will be set to 1 (completely black), blocking all input.
        }
        else
        {
            Debug.LogError("[FadeManagerLoader] Could not find FadeManagerPrefab in Resources/Fade.");
        }
    }
}