using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a rating slider at a configurable mid-experiment event.
/// Attach this to an always-active scene object, not to the disabled rating panel.
/// </summary>
public class MiddleRatingTrigger : MonoBehaviour
{
    public enum TriggerSource
    {
        BarrierOpened,
        RoomTimerFinished
    }

    [Header("Trigger")]
    public TriggerSource triggerSource = TriggerSource.BarrierOpened;

    [Tooltip("Used when Trigger Source is Barrier Opened.")]
    public BarrierManager barrierManager;

    [Tooltip("1 = first turnstile, 2 = second, 3 = third, etc.")]
    [Min(1)]
    public int barrierNumber = 3;

    [Tooltip("Used when Trigger Source is Room Timer Finished.")]
    public RoomManager roomManager;

    [Header("Rating UI")]
    [Tooltip("Assign the Rate slider middle GameObject. Its RatingSliderUI should reference the matching grid.")]
    public GameObject ratingPanel;

    [Tooltip("Reset the RatingSliderUI so it starts from the grid before showing the slider.")]
    public bool resetRatingForReuse = true;

    [Tooltip("Keep ON if this prompt may be shown after visual-isolation logic disabled UI graphics.")]
    public bool forceEnableRatingVisuals = true;

    [Tooltip("If ON, this prompt can only be shown once per play session.")]
    public bool triggerOnce = true;

    private bool _hasTriggered;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (barrierManager != null)
            barrierManager.OnBarrierOpened += OnBarrierOpened;

        if (roomManager != null)
            roomManager.OnTimerFinished += OnRoomTimerFinished;
    }

    private void Unsubscribe()
    {
        if (barrierManager != null)
            barrierManager.OnBarrierOpened -= OnBarrierOpened;

        if (roomManager != null)
            roomManager.OnTimerFinished -= OnRoomTimerFinished;
    }

    private void OnBarrierOpened(int openedIndex, string triggerName)
    {
        if (triggerSource != TriggerSource.BarrierOpened)
            return;

        int wantedIndex = Mathf.Max(1, barrierNumber) - 1;
        if (openedIndex != wantedIndex)
            return;

        ShowRating($"barrier_{barrierNumber}:{triggerName}");
    }

    private void OnRoomTimerFinished(RoomManager finishedRoom)
    {
        if (triggerSource != TriggerSource.RoomTimerFinished)
            return;

        if (roomManager != null && finishedRoom != roomManager)
            return;

        string roomName = finishedRoom != null ? finishedRoom.name : "room";
        ShowRating($"room_timer_finished:{roomName}");
    }

    private void ShowRating(string reason)
    {
        if (triggerOnce && _hasTriggered)
            return;

        if (ratingPanel == null)
        {
            Debug.LogWarning($"[MiddleRatingTrigger] No rating panel assigned for {reason}.", this);
            return;
        }

        _hasTriggered = true;

        var slider = ratingPanel.GetComponentInChildren<RatingSliderUI>(true);
        if (resetRatingForReuse && slider != null)
            slider.ResetForReuse();

        ratingPanel.SetActive(true);

        if (forceEnableRatingVisuals)
        {
            ForceEnableVisuals(ratingPanel);
            if (slider != null && slider.valenceArousalGrid != null)
                ForceEnableVisuals(slider.valenceArousalGrid.gameObject);
        }

        QuestEventOutlet.Send($"middle_rating_shown:{reason}");
        Debug.Log($"[MiddleRatingTrigger] Rating shown after {reason}.", this);
    }

    private static void ForceEnableVisuals(GameObject root)
    {
        if (root == null)
            return;

        var graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].enabled = true;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;
    }
}
