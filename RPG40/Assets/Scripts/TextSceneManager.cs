using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// The logic of TextScene: Display a group of TMP texts in sequence, and finally enable the NextButton.
/// Only after both client sides click Next will the server truly switch to the GamePlay1 scene.
/// </summary>
/// <summary>
/// This class inherits a class in Mirror, I override part of its function.
/// </summary>
public class TextSceneManager : NetworkBehaviour
{


    public TMP_Text[] textLines;

    
    public float fadeDuration = 0.5f;


    public Button nextButton;

    // The local record has been viewed and the "Next" button has been clicked.
    private bool localClicked = false;

    // The server maintains the count of how many clients have clicked "Next".
    [SyncVar]
    private int readyCount = 0;

    private int linesIndex = 0;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // When the scene is loading, set the alpha value of all text to 0 and disable the buttons
        foreach (var t in textLines)
        {
            var c = t.color;
            c.a = 0f;
            t.color = c;
        }
        nextButton.interactable = false;
        nextButton.onClick.AddListener(OnNextClicked);

        // If it is a Host or Server, initialize the readyCount
        if (isServer)
            readyCount = 0;

        // Start the coroutine to gradually change the first line of text in sequence
        if (isLocalPlayer)
            StartCoroutine(FadeInNextLine());
    }

    /// <summary>
    /// Coroutine: Gradually display the elements of textLines[0..n] in sequence until all of them are displayed completely, then enable the nextButton.
    /// </summary>
    private IEnumerator FadeInNextLine()
    {
        while (linesIndex < textLines.Length)
        {
            TMP_Text current = textLines[linesIndex];
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                Color c = current.color;
                c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                current.color = c;
                yield return null;
            }
            // alpha = 1
            Color final = current.color;
            final.a = 1f;
            current.color = final;

            linesIndex++;

            // Before the next section, you can wait for the player to click on the screen.
// If you want "the next section to appear automatically in sequence", delete the following two lines.
// If you want "the next section to appear upon a single click", keep this section.
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        }

        // After all the text has been displayed in a gradient manner, activate the "nextButton"
        nextButton.interactable = true;
    }

    /// <summary>
    /// When the user clicks the "Next" button, the following procedure is executed.
    /// </summary>
    private void OnNextClicked()
    {
        if (localClicked) return;
        localClicked = true;
        nextButton.interactable = false; // Prevent multiple clicks

        // Notify the server that the local click has been completed.
        CmdNotifyReady();
    }

    [Command]
    private void CmdNotifyReady(NetworkConnectionToClient sender = null)
    {
        readyCount++;

        // If all the connections have clicked "Next", then let the server switch to GamePlay1.
        if (readyCount == NetworkServer.connections.Count)
        {
            
            readyCount = 0;
            
            NetworkManager.singleton.ServerChangeScene("GamePlay1");
        }
    }
}
