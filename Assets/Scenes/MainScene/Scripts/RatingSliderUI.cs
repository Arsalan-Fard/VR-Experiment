using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to any rating slider canvas. Auto-wires the Slider, value label,
/// and Done button. On submit: logs to RatingManager, closes the popup,
/// and optionally notifies BoxChoiceManager.
/// </summary>
public class RatingSliderUI : MonoBehaviour
{
    [Header("Auto-found if left empty")]
    public Slider slider;
    public Button doneButton;

    [Header("Value display (auto-found child named 'ValueLabel', or created)")]
    public TMP_Text valueLabel;

    [Header("Context tag for LSL marker (box_choice, reveal)")]
    public string ratingContext = "box_choice";

    [Header("Optional: notify BoxChoiceManager when done")]
    public BoxChoiceManager boxChoiceManager;

    [Header("Optional: 2D valence/arousal grid shown BEFORE the slider")]
    [Tooltip("If assigned, the grid is shown first. The slider stays hidden " +
             "until the grid is confirmed, then the slider's Done completes " +
             "the whole rating.")]
    public ValenceArousalGridUI valenceArousalGrid;

    private bool _gridSubscribed;
    // True once the grid has been confirmed, so re-enabling the slider
    // doesn't bounce back to the grid.
    private bool _gridConfirmed;

    void OnEnable()
    {
        // Auto-find components if not assigned
        if (slider == null)
            slider = GetComponentInChildren<Slider>(true);

        if (doneButton == null)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
            {
                if (b.gameObject.name == "Done")
                {
                    doneButton = b;
                    break;
                }
            }
        }

        if (valueLabel == null)
        {
            // Look for a child named "ValueLabel"
            var labels = GetComponentsInChildren<TMP_Text>(true);
            foreach (var lbl in labels)
            {
                if (lbl.gameObject.name == "ValueLabel")
                {
                    valueLabel = lbl;
                    break;
                }
            }
        }

        // Wire listeners
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderChanged);
            slider.onValueChanged.AddListener(OnSliderChanged);
            OnSliderChanged(slider.value); // update label immediately
        }

        if (doneButton != null)
        {
            doneButton.onClick.RemoveListener(OnDoneClicked);
            doneButton.onClick.AddListener(OnDoneClicked);
        }

        // If a grid is assigned and not yet confirmed, show it first and
        // keep the slider hidden until the grid is confirmed.
        if (valenceArousalGrid != null && !_gridConfirmed)
        {
            if (!_gridSubscribed)
            {
                valenceArousalGrid.onGridDone += OnGridConfirmed;
                _gridSubscribed = true;
            }
            valenceArousalGrid.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderChanged);
        if (doneButton != null)
            doneButton.onClick.RemoveListener(OnDoneClicked);
        // Note: the grid subscription is intentionally NOT removed here.
        // The slider disables itself to hand off to the grid, and we still
        // need OnGridConfirmed to fire while the slider is disabled.
    }

    void OnDestroy()
    {
        if (valenceArousalGrid != null && _gridSubscribed)
            valenceArousalGrid.onGridDone -= OnGridConfirmed;
    }

    private void OnSliderChanged(float value)
    {
        if (valueLabel != null)
            valueLabel.text = Mathf.RoundToInt(value).ToString();
    }

    /// <summary>
    /// Called by ValenceArousalGridUI when its Confirm is pressed.
    /// The grid has already closed itself; now reveal the slider.
    /// </summary>
    private void OnGridConfirmed()
    {
        _gridConfirmed = true;
        gameObject.SetActive(true);   // re-enables the slider; OnEnable skips
                                      // the grid because _gridConfirmed is set
    }

    private void OnDoneClicked()
    {
        int rating = slider != null ? Mathf.RoundToInt(slider.value) : 1;

        // Send rating via LSL
        if (RatingManager.Instance != null)
            RatingManager.Instance.LogRating(ratingContext, rating);

        // Slider is the final step — complete the rating now.
        CompleteRating(rating);
    }

    /// <summary>
    /// Finishes the rating: notifies BoxChoiceManager and listeners.
    /// </summary>
    private void CompleteRating(int rating)
    {
        // Notify BoxChoiceManager so it can show nextPanel
        if (boxChoiceManager != null)
            boxChoiceManager.SubmitRating(rating);

        onRatingDone?.Invoke();

        // Reset so the NEXT time this slider is shown (e.g. the next
        // condition) the grid appears first again.
        _gridConfirmed = false;

        // Slider popup is already closed if the grid path was taken;
        // calling again is harmless.
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Forces the next show to start from the grid again. Call this if the
    /// rating UI is reset/re-shown without a full CompleteRating cycle.
    /// </summary>
    public void ResetForReuse()
    {
        _gridConfirmed = false;
    }

    public System.Action onRatingDone;

    public void SetInteractable(bool interactable)
    {
        if (slider != null)
            slider.interactable = interactable;
        if (doneButton != null)
            doneButton.interactable = interactable;
    }
}
