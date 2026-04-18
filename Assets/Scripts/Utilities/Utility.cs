using UnityEngine;

public static class Utility
{
    private static MonoBehaviour monoBehaviour;

    public static void Setup(MonoBehaviour monoBehaviour)
    {
        Utility.monoBehaviour = monoBehaviour;
    }

    public static bool IsEditor()
    {
#if UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    public static bool IsWebGL()
    {
#if UNITY_WEBGL
        return true;
#else
        return false;
#endif
    }

    public static void WaitForSeconds(float seconds, System.Action onComplete)
    {
        monoBehaviour.StartCoroutine(WaitCoroutine(seconds, onComplete));
    }

    private static System.Collections.IEnumerator WaitCoroutine(float seconds, System.Action onComplete)
    {
        yield return new WaitForSeconds(seconds);
        onComplete?.Invoke();
    }
}