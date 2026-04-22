using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private GameObject transitionCanvas;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    
    [Header("Loading Settings")]
    [SerializeField] private bool showLoadingProgress = false;
    [SerializeField] private float minimumLoadTime = 2.5f;
    [SerializeField] private Image[] loadingBar;
    [SerializeField] private TMPro.TextMeshProUGUI loadingText;

    private bool isLoading = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Setup initial state
            if (transitionCanvas != null)
            {
                transitionCanvas.SetActive(false);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load scene by name with fade transition
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }
    }

    /// <summary>
    /// Load scene by build index with fade transition
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex));
        }
    }

    /// <summary>
    /// Reload current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Load next scene in build settings
    /// </summary>
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = (currentIndex + 1) % SceneManager.sceneCountInBuildSettings;
        LoadScene(nextIndex);
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;

        // Show transition canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(true);
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Start loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float startTime = Time.time;

        // Wait for scene to load with minimum display time
        while (asyncLoad.progress < 0.9f || Time.time - startTime < minimumLoadTime)
        {
            float realProgress = asyncLoad.progress / 0.9f;
            float fakeProgress = Mathf.Clamp01((Time.time - startTime) / minimumLoadTime);
            UpdateLoadingProgress(Mathf.Max(realProgress, fakeProgress));
            yield return null;
        }
        UpdateLoadingProgress(1f);

        // Activate the scene
        asyncLoad.allowSceneActivation = true;

        // Wait for scene to fully activate
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Hide transition canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }

        isLoading = false;
    }

    private IEnumerator LoadSceneCoroutine(int sceneIndex)
    {
        isLoading = true;

        // Show transition canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(true);
        }

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Start loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        asyncLoad.allowSceneActivation = false;

        float startTime = Time.time;

        // Wait for scene to load with minimum display time
        while (asyncLoad.progress < 0.9f || Time.time - startTime < minimumLoadTime)
        {
            float realProgress = asyncLoad.progress / 0.9f;
            float fakeProgress = Mathf.Clamp01((Time.time - startTime) / minimumLoadTime);
            UpdateLoadingProgress(Mathf.Max(realProgress, fakeProgress));
            yield return null;
        }
        UpdateLoadingProgress(1f);

        // Activate the scene
        asyncLoad.allowSceneActivation = true;

        // Wait for scene to fully activate
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Hide transition canvas
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }

        isLoading = false;
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        if (loadingBar != null)
        {
            foreach (var bar in loadingBar)
            {
                bar.fillAmount = 0;
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (!showLoadingProgress) return;

        if (loadingBar != null)
        {
            foreach (var bar in loadingBar)
            {
                bar.fillAmount = Mathf.Lerp(bar.fillAmount, progress, Time.deltaTime * 10f);
            }
        }

        if (loadingText != null)
        {
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
        }
    }

    /// <summary>
    /// Simple fade to black and back without loading
    /// </summary>
    public IEnumerator FadeTransition()
    {
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(true);
        }

        yield return StartCoroutine(FadeOut());
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeIn());

        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }
    }
}
