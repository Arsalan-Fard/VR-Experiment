using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 2D affect grid (valence = X, arousal = Y). The player clicks anywhere
/// inside the square plot to place a marker, then presses Confirm.
/// Axes report -1..+1 with (0,0) at the center.
///
/// Hierarchy expected (assign in inspector, or auto-found by name):
///   Panel (this script)
///    ├─ Plot        (Image, RectTransform) — the clickable square
///    │   └─ Marker  (Image)                — the dot, moved on click
///    ├─ Confirm     (Button)
///    └─ ValueLabel  (TMP_Text, optional)   — shows current (v, a)
/// </summary>
public class ValenceArousalGridUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Auto-found if left empty")]
    [Tooltip("The square RectTransform the player clicks inside.")]
    public RectTransform plot;
    [Tooltip("The dot moved to the clicked position.")]
    public RectTransform marker;
    public Button confirmButton;

    [Header("Value display (optional, child named 'ValueLabel')")]
    public TMP_Text valueLabel;

    [Header("Context tag for LSL marker")]
    public string ratingContext = "box_choice";

    [Header("Require a selection before Confirm is enabled")]
    public bool requireSelection = true;

    /// <summary>Fired after Confirm. Lets RatingSliderUI/RevealManager continue.</summary>
    public System.Action onGridDone;

    // Normalized -1..+1
    private float _valence;
    private float _arousal;
    private bool _hasSelection;

    void OnEnable()
    {
        AutoFind();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        // Reset for a fresh selection each time the panel is shown
        _hasSelection = false;
        _valence = 0f;
        _arousal = 0f;

        if (marker != null)
        {
            marker.anchoredPosition = Vector2.zero;
            marker.gameObject.SetActive(!requireSelection);
        }

        UpdateLabel();
        if (confirmButton != null && requireSelection)
            confirmButton.interactable = false;
    }

    void OnDisable()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }

    private void AutoFind()
    {
        if (plot == null)
        {
            var t = transform.Find("Plot") as RectTransform;
            if (t != null) plot = t;
        }
        if (marker == null && plot != null)
        {
            var t = plot.Find("Marker") as RectTransform;
            if (t != null) marker = t;
        }
        if (confirmButton == null)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
                if (b.gameObject.name == "Confirm")
                {
                    confirmButton = b;
                    break;
                }
        }
        if (valueLabel == null)
        {
            foreach (var lbl in GetComponentsInChildren<TMP_Text>(true))
                if (lbl.gameObject.name == "ValueLabel")
                {
                    valueLabel = lbl;
                    break;
                }
        }
    }

    /// <summary>
    /// Fired when the player clicks anywhere inside the plot (the plot's
    /// Image receives the click; this handler is reached via event bubbling
    /// because the script also lives on the plot — see note in OnEnable).
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (plot == null) return;

        // Convert the click into the plot's local coordinates.
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                plot, eventData.position, eventData.pressEventCamera, out local))
            return;

        Rect r = plot.rect;

        // Local point relative to the plot's bottom-left, clamped to the square.
        float nx = Mathf.Clamp01((local.x - r.xMin) / r.width);
        float ny = Mathf.Clamp01((local.y - r.yMin) / r.height);

        // Map 0..1 to -1..+1
        _valence = nx * 2f - 1f;
        _arousal = ny * 2f - 1f;
        _hasSelection = true;

        if (marker != null)
        {
            marker.gameObject.SetActive(true);
            // anchoredPosition is relative to the marker's anchor; with a
            // centered anchor, 0,0 is the plot center.
            marker.anchoredPosition = new Vector2(
                (nx - 0.5f) * r.width,
                (ny - 0.5f) * r.height);
        }

        UpdateLabel();

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void UpdateLabel()
    {
        if (valueLabel == null) return;
        valueLabel.text = _hasSelection
            ? $"V {_valence:+0.00;-0.00;0.00}   A {_arousal:+0.00;-0.00;0.00}"
            : "Tap the grid";
    }

    private void OnConfirmClicked()
    {
        if (requireSelection && !_hasSelection) return;

        // LSL markers: one per axis, two-decimal precision.
        if (RatingManager.Instance != null)
        {
            RatingManager.Instance.LogRating($"{ratingContext}_valence",
                Mathf.RoundToInt(_valence * 100f));
            RatingManager.Instance.LogRating($"{ratingContext}_arousal",
                Mathf.RoundToInt(_arousal * 100f));
        }

        Debug.Log($"[ValenceArousalGridUI] Confirmed V={_valence:0.00} A={_arousal:0.00}");

        onGridDone?.Invoke();
        gameObject.SetActive(false);
    }

    /// <summary>Latest selection, -1..+1. X = valence, Y = arousal.</summary>
    public Vector2 SelectedPoint => new Vector2(_valence, _arousal);
}
