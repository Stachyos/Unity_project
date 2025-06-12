using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global fade-in and fade-out management. The "fade-in" process will only be executed when entering the specified scene.
/// </summary>
public class FadeManager : MonoBehaviour
{
 
    public static FadeManager Instance { get; private set; }


    public CanvasGroup fadeCanvasGroup;


    public float fadeDuration = 0.5f;


    public List<string> fadeInSceneNames = new List<string>();

    private bool isFading = false;

    private void Awake()
    {
        // Singleton check: If there is already one, then destroy the new instance.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Determine whether the currently active scene requires a fade-in effect
        string currentScene = SceneManager.GetActiveScene().name;
        bool shouldFadeIn = fadeInSceneNames.Contains(currentScene);

        if (shouldFadeIn)
        {
            // Initially, set the mask to full black and block all inputs. Then, start fading in.
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeInImmediate());
        }
        else
        {
            // No need for fade-in effect. Simply hide the mask and allow interaction.
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        // Subscription scene loading completion event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // After the scene loading is completed, determine whether to fade in.
        if (fadeInSceneNames.Contains(scene.name))
        {
            StartCoroutine(FadeInImmediate());
        }
        else
        {
            // Directly hide the mask and allow interaction
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            isFading = false;
        }
    }

    /// <summary>
    /// External interface: First, fade out (from transparent to black), then asynchronously load the new scene. When the new scene is loaded, if it is in the list, fade in; otherwise, simply pass it through.
    /// </summary>
    public void FadeToScene(string sceneName)
    {
        if (!isFading)
            StartCoroutine(FadeOutAndLoadScene(sceneName));
    }

    /// <summary>
    /// Fade in immediately: From alpha=1 → alpha=0; During this period, intercept the event until it becomes fully transparent and then allow it to pass.
    /// </summary>
    private IEnumerator FadeInImmediate()
    {
        isFading = true;
        fadeCanvasGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        isFading = false;
    }

    /// <summary>
    /// First, fade out (with alpha ranging from 0 to 1),
    /// during which the input is intercepted; then asynchronously load the scene. OnSceneLoaded will determine
    /// whether to continue fading in based on the scene name.
    /// </summary>
    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        isFading = true;
        fadeCanvasGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;

        yield return SceneManager.LoadSceneAsync(sceneName);
        // After the scene loading is completed, OnSceneLoaded will be called.
    }
}
