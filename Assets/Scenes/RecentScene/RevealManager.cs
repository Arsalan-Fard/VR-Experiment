using UnityEngine;

public class RevealManager : MonoBehaviour
{
    [Header("Door at this location")]
    public DoorManager doorManager;

    [Header("XR Rig / XR Origin")]
    public Transform xrOrigin;

    [Header("State Machine")]
    public ExperimentStateManager stateManager;

    [Header("Reveal UI (World Space Canvases)")]
    public GameObject failUI;      // disabled by default
    public GameObject successUI;   // disabled by default

    [Header("Behavior")]
    public bool triggerOncePerCondition = true;
    public bool closeDoorOnConditionStart = true;

    private bool _isActiveRevealTrigger = false;
    private bool _showSuccessThisCondition = false;
    private bool _triggeredThisCondition = false;

    private void OnTriggerEnter(Collider other) => TryTrigger(other);
    private void OnTriggerStay(Collider other)  => TryTrigger(other); // fixes “already inside” issue

    private void TryTrigger(Collider other)
    {
        if (!_isActiveRevealTrigger) return;
        if (triggerOncePerCondition && _triggeredThisCondition) return;
        if (!xrOrigin || !stateManager) return;

        // Only react to XR rig (or its children like camera/hands)
        if (other.transform != xrOrigin && !other.transform.IsChildOf(xrOrigin))
            return;

        _triggeredThisCondition = true;

        if (doorManager != null)
            doorManager.Open();
        else
            Debug.LogWarning($"[RevealManager] doorManager is not assigned on {name}.");

        // UI toggles (no text updates)
        if (failUI) failUI.SetActive(!_showSuccessThisCondition);
        if (successUI) successUI.SetActive(_showSuccessThisCondition);

        stateManager.OnRevealReached();

        Debug.Log($"[RevealManager] Triggered at {name}. Success={_showSuccessThisCondition}");
    }

    /// <summary>
    /// Called by the state machine when a condition starts.
    /// </summary>
    public void ConfigureForCondition(bool activeRevealTrigger, bool showSuccess)
    {
        _isActiveRevealTrigger = activeRevealTrigger;
        _showSuccessThisCondition = showSuccess;
        _triggeredThisCondition = false;

        // Hide both UIs whenever condition changes / arms
        if (failUI) failUI.SetActive(false);
        if (successUI) successUI.SetActive(false);

        // Force a known door state each condition (prevents “sometimes” issues)
        if (closeDoorOnConditionStart && doorManager != null)
            doorManager.SetOpenImmediate(false);
    }
}
