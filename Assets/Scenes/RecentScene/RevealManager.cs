using UnityEngine;
using UnityEngine.UI;

public class RevealManager : MonoBehaviour
{
    [Header("Door before reveal room (optional)")]
    public DoorManager revealDoor;

    [Header("XR Rig / XR Origin")]
    public Transform xrOrigin;

    [Header("State Machine")]
    public ExperimentStateManager stateManager;

    [Header("Reveal UI (World Space Canvases)")]
    public GameObject failUI;      // disabled by default
    public GameObject successUI;   // disabled by default

    [Header("Behavior")]
    public bool triggerOncePerCondition = true;

    private bool _isActiveRevealTrigger = false;
    private bool _showSuccessThisCondition = false;
    private bool _triggeredThisCondition = false;

    private void OnTriggerEnter(Collider other) => TryTrigger(other);
    private void OnTriggerStay(Collider other)  => TryTrigger(other);

    private void TryTrigger(Collider other)
    {
        if (!_isActiveRevealTrigger) return;
        if (triggerOncePerCondition && _triggeredThisCondition) return;
        if (!xrOrigin || !stateManager) return;

        if (other.transform != xrOrigin && !other.transform.IsChildOf(xrOrigin))
            return;

        _triggeredThisCondition = true;

        // Open reveal door
        if (revealDoor != null)
            revealDoor.Open();

        // Show fail/success UI
        if (failUI) failUI.SetActive(!_showSuccessThisCondition);
        if (successUI) successUI.SetActive(_showSuccessThisCondition);

        // Middle isolation may have disabled Graphic.enabled; force it back on
        ForceEnableVisuals(_showSuccessThisCondition ? successUI : failUI);

        // Notify state machine
        stateManager.OnRevealReached();

        Debug.Log($"[RevealManager] Triggered at {name}. Success={_showSuccessThisCondition}");
    }

    /// <summary>
    /// Arms/disarms reveal logic for the current phase.
    /// IMPORTANT: In some phases (e.g., experiment end) you may want to disarm WITHOUT hiding UI.
    /// </summary>
    public void ConfigureForCondition(bool activeRevealTrigger, bool showSuccess, bool resetAndHideUI = true)
    {
        _isActiveRevealTrigger = activeRevealTrigger;
        _showSuccessThisCondition = showSuccess;

        if (resetAndHideUI)
        {
            ResetRevealRoom(closeDoor: false);
        }
        else
        {
            // Still ensure we won't retrigger within the same phase unless you want that.
            // Do NOT change UI visibility.
        }
    }

    public void ResetRevealRoom(bool closeDoor)
    {
        _triggeredThisCondition = false;

        if (failUI) failUI.SetActive(false);
        if (successUI) successUI.SetActive(false);

        if (closeDoor && revealDoor != null)
            revealDoor.SetOpenImmediate(false);
    }

    private static void ForceEnableVisuals(GameObject root)
    {
        if (root == null) return;

        var graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].enabled = true;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;
    }
}
