using System;
using UnityEngine;
using UnityEngine.UI;

public static class CollectedPrefabCounter
{
    public static event Action<int> CountChanged;

    public static int Count { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Count = 0;
        CountChanged = null;
    }

    public static void Increment()
    {
        Count++;
        CountChanged?.Invoke(Count);
    }
}

public class CollectedPrefabCounterUI : MonoBehaviour
{
    private static CollectedPrefabCounterUI instance;
    private Text counterText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("Collected Prefab Counter UI");
        root.AddComponent<CollectedPrefabCounterUI>();
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
    }

    private void OnEnable()
    {
        CollectedPrefabCounter.CountChanged += HandleCountChanged;
        HandleCountChanged(CollectedPrefabCounter.Count);
    }

    private void OnDisable()
    {
        CollectedPrefabCounter.CountChanged -= HandleCountChanged;
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        GameObject textObject = new GameObject("Text", typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(20f, -20f);
        textRect.sizeDelta = new Vector2(420f, 40f);

        counterText = textObject.GetComponent<Text>();
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counterText.fontSize = 28;
        counterText.alignment = TextAnchor.MiddleLeft;
        counterText.color = Color.white;
        counterText.horizontalOverflow = HorizontalWrapMode.Overflow;
        counterText.verticalOverflow = VerticalWrapMode.Truncate;
        counterText.raycastTarget = false;

        HandleCountChanged(CollectedPrefabCounter.Count);
    }

    private void HandleCountChanged(int count)
    {
        if (counterText != null)
            counterText.text = $"Collected: {count}";
    }
}

