using System.Collections;
using UnityEngine;
using UnityEngine.UI;                   
using UnityEngine.SceneManagement;      
using TMPro;

public class ClickAddText : MonoBehaviour
{


    public TMP_Text[] tmpTexts;


    public float fadeDuration = 0.5f;


    public Button skipButton;


    public string nextSceneName;

    private int nextIndex = 0;
    private bool allTextsShown = false;    
    private bool isFading = false;         

    private void Start()
    {
       
        
        Debug.Log($"ClickAddText.Start()，tmpTexts.Length = {tmpTexts.Length}");
        foreach (var t in tmpTexts)
        {
            
            Color c = t.color;
            c.a = 0f;
            t.color = c;
            
        }


        // If there is at least one text, then let the first gradient appear.
        if (tmpTexts.Length > 0)
        {
            StartCoroutine(FadeInText(tmpTexts[0]));
            nextIndex = 1;
            isFading = true;  // The first item is fading in.
        }

        // Register a callback for the "Skip" button (if already assigned)
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }

    private void Update()
    {
        // Listen for the left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // If there is a text fading in, simply ignore this click.
            if (isFading) return;

            // If there are still some characters not displayed, continue to gradually show the next one.
            if (nextIndex < tmpTexts.Length)
            {
                StartCoroutine(FadeInText(tmpTexts[nextIndex]));
                nextIndex++;
                isFading = true;  // Mark the beginning of the new fade-in sequence
            }
            else
            {
                // nextIndex >= tmpTexts.Length indicates that all the text has been displayed completely.
                // If you click again, it will load the next scene.
                if (allTextsShown)
                {
                    LoadNextScene();
                }
            }
        }
    }

    /// <summary>
    /// The skip button is triggered when it is clicked.
    /// </summary>
    private void OnSkipButtonClicked()
    {
        // Whether all of them are displayed or not, directly move to the next scene.
        LoadNextScene();
    }

    /// <summary>
    /// The method for loading the next scene
    /// </summary>
    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // If the "nextSceneName" is specified, use it to load the scene.
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("If you haven't set the nextSceneName in the Inspector, you won't be able to jump to the next scene!");
        }
    }

    /// <summary>
    /// Coroutine: Fade in and display a single TMP_Text
    /// </summary>
    private IEnumerator FadeInText(TMP_Text text)
    {
        float timer = 0f;
        Color c = text.color;

        // alpha = 0 to alpha = 1
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            text.color = c;
            yield return null;
        }

        // Ensure that the final alpha value is 1
        c.a = 1f;
        text.color = c;

        // Once this text has fully faded in, the "fading in" indicator can be removed.
        isFading = false;

// If this is the last text, mark "All text has been displayed"
// Note: nextIndex has been incremented before the StartCoroutine, so when nextIndex == tmpTexts.Length
// it indicates that the last element in the array was just processed.
        if (nextIndex >= tmpTexts.Length)
        {
            allTextsShown = true;
        }
    }
}
