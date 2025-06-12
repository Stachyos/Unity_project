using UnityEngine;
using UnityEngine.SceneManagement;  
namespace GameLogic.Runtime
{
    
    public class TitleScreenController : MonoBehaviour
    {
        
        public string nextSceneName = "GameScene";


        public float hintBlinkInterval = 0.5f;

        private UnityEngine.UI.Text hintTextUI; 
        private TMPro.TextMeshProUGUI hintTextTMP; 
        private float blinkTimer;

        private bool useTMP = false;

        void Start()
        {
            // Try to obtain the TextMeshPro component. If it is not available, then obtain the regular Text component.
            hintTextTMP = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (hintTextTMP != null)
            {
                useTMP = true;
            }
            else
            {
                hintTextUI = GetComponentInChildren<UnityEngine.UI.Text>();
                useTMP = false;
            }

            blinkTimer = 0f;
        }

        void Update()
        {
            // 1. Detect "Click anywhere" 
// On a PC, Mouse0 represents the left button, and Mouse1 represents the right button. If you need to detect for mobile devices/touch screens, you can use the condition Input.touchCount > 0
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                StartGame();
            }

            //  the text "Click anywhere start the game" to flash
            if (hintBlinkInterval > 0f)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= hintBlinkInterval)
                {
                    blinkTimer = 0f;
                    ToggleHintVisibility();
                }
            }
        }

        private void ToggleHintVisibility()
        {
            if (useTMP && hintTextTMP != null)
            {
                hintTextTMP.enabled = !hintTextTMP.enabled;
            }
            else if (hintTextUI != null)
            {
                hintTextUI.enabled = !hintTextUI.enabled;
            }
        }

        private void StartGame()
        {

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("[TitleScreenController] nextSceneName not found！");
            }
        }
    }
}
