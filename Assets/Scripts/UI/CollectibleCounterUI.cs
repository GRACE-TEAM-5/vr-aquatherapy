using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Small head-locked counter ("Collected: N") that is visible inside the headset.
/// Created automatically; it only shows in scenes that contain an InteractionManager.
/// (A Screen Space Overlay canvas is not rendered in VR, which is why the old
/// counter was invisible on the Quest.)
/// </summary>
public class CollectibleCounterUI : MonoBehaviour
{
    [Tooltip("Position of the counter relative to the player's head (metres).")]
    public Vector3 headOffset = new Vector3(0f, -0.28f, 1.2f);
    public string label = "Collected";

    private static CollectibleCounterUI instance;

    private Canvas canvas;
    private Text counterText;
    private Transform viewCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("Collectible Counter UI");
        root.AddComponent<CollectibleCounterUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Application.onBeforeRender += FollowCamera;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Application.onBeforeRender -= FollowCamera;
    }

    private void OnEnable()
    {
        CollectibleCounter.CountChanged += HandleCountChanged;
        HandleCountChanged(CollectibleCounter.Count);
    }

    private void OnDisable()
    {
        CollectibleCounter.CountChanged -= HandleCountChanged;
    }

    private void Start()
    {
        RefreshForScene();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForScene();
    }

    private void LateUpdate()
    {
        FollowCamera();
    }

    private void RefreshForScene()
    {
        if (canvas == null)
            return;

        viewCamera = null;
        canvas.enabled = FindAnyObjectByType<InteractionManager>() != null;
        HandleCountChanged(CollectibleCounter.Count);
    }

    private void FollowCamera()
    {
        if (canvas == null || !canvas.enabled)
            return;

        if (viewCamera == null)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            viewCamera = cam.transform;
        }

        Transform t = canvas.transform;
        t.SetPositionAndRotation(viewCamera.TransformPoint(headOffset), viewCamera.rotation);
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("Counter Canvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localScale = Vector3.one * 0.001f; // 1 canvas unit = 1 mm

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        canvas.enabled = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(500f, 80f);

        GameObject textObject = new GameObject("Text", typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        counterText = textObject.GetComponent<Text>();
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counterText.fontSize = 48;
        counterText.alignment = TextAnchor.MiddleCenter;
        counterText.color = Color.white;
        counterText.horizontalOverflow = HorizontalWrapMode.Overflow;
        counterText.verticalOverflow = VerticalWrapMode.Overflow;
        counterText.raycastTarget = false;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        HandleCountChanged(CollectibleCounter.Count);
    }

    private void HandleCountChanged(int count)
    {
        if (counterText != null)
            counterText.text = $"{label}: {count}";
    }
}
