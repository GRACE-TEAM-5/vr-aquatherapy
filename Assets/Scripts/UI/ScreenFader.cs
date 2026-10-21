using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fades the headset view to a colour and back (e.g. while the player is relocated).
/// Uses a world-space UI image right in front of the camera, so it needs no special
/// shader and works in the Editor, over Link and in builds. Created on first use.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private const float Distance = 0.25f;

    private static ScreenFader instance;

    private Canvas canvas;
    private Image image;
    private Transform viewCamera;
    private float alpha;

    public static float Alpha => instance != null ? instance.alpha : 0f;

    /// <summary>Use inside a coroutine: <c>yield return ScreenFader.FadeTo(1f, 0.5f);</c></summary>
    public static IEnumerator FadeTo(float targetAlpha, float duration, Color? color = null)
    {
        ScreenFader fader = GetOrCreate();
        if (color.HasValue)
        {
            Color c = color.Value;
            c.a = fader.alpha;
            fader.image.color = c;
        }

        float start = fader.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fader.SetAlpha(Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration)));
            yield return null;
        }

        fader.SetAlpha(targetAlpha);
    }

    private static ScreenFader GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject root = new GameObject("Screen Fader");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<ScreenFader>();
        instance.Build();
        return instance;
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("Fade Canvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localScale = Vector3.one * 0.001f;

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = short.MaxValue;
        canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(4000f, 4000f); // 4 x 4 m at 25 cm

        GameObject imageObject = new GameObject("Fade", typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(0f, 0f, 0f, 0f);

        SetAlpha(0f);
        Application.onBeforeRender += FollowCamera;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
        Application.onBeforeRender -= FollowCamera;
    }

    private void LateUpdate()
    {
        FollowCamera();
    }

    private void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);
        Color c = image.color;
        c.a = alpha;
        image.color = c;
        canvas.enabled = alpha > 0.001f;
        if (canvas.enabled)
            FollowCamera();
    }

    private void FollowCamera()
    {
        if (canvas == null || !canvas.enabled)
            return;

        if (viewCamera == null || !viewCamera.gameObject.activeInHierarchy)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            viewCamera = cam.transform;
        }

        canvas.transform.SetPositionAndRotation(
            viewCamera.position + viewCamera.forward * Distance,
            viewCamera.rotation);
    }
}
