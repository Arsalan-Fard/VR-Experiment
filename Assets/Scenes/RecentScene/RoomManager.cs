using System.Collections;
using UnityEngine;
using TMPro;

public class RoomManager : MonoBehaviour
{
    [Header("Door to lock")]
    public DoorManager doorManager;

    [Header("Player collider (recommended)")]
    [Tooltip("Assign the XR Origin's CharacterController object collider (or a dedicated PlayerBody collider).")]
    public Collider playerBodyCollider; // best: a single collider, not hands

    [Header("Timing")]
    public float closeDelaySeconds = 0.2f;
    public float lockSeconds = 5f;

    [Header("Timer UI (always visible)")]
    public TMP_Text timerText;

    [Header("Behaviour")]
    public bool openDoorWhenDone = true;
    public bool resetDoorOnDisable = true;

    private Collider _triggerCollider;
    private bool _running;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        if (_triggerCollider == null || !_triggerCollider.isTrigger)
            Debug.LogWarning("[RoomManager] This object should have a Collider set as Is Trigger.", this);
    }

    private void Start()
    {
        SetTimerText(0f); // show 00:00 initially
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_running) return;
        if (!doorManager || !timerText) return;
        if (!IsPlayerBody(other)) return;

        StartCoroutine(LockCycle());
    }

    private void OnTriggerExit(Collider other)
    {
        // Rearm only after the player body leaves the trigger volume
        if (!IsPlayerBody(other)) return;

        if (_triggerCollider != null)
            _triggerCollider.enabled = true;
    }

    private bool IsPlayerBody(Collider other)
    {
        // Strict match: only the collider you assigned can trigger the room lock
        return playerBodyCollider != null && other == playerBodyCollider;
    }

    private IEnumerator LockCycle()
    {
        _running = true;

        // Prevent re-trigger while the player is still overlapping the slab
        if (_triggerCollider != null)
            _triggerCollider.enabled = false;

        // Start countdown immediately
        float remaining = lockSeconds;
        SetTimerText(remaining);

        // Close behind user after a short delay (so it closes behind them, not onto them)
        if (closeDelaySeconds > 0f)
            yield return new WaitForSeconds(closeDelaySeconds);

        doorManager.Close();

        // Update once per frame (simple); you can throttle if you want
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            if (remaining < 0f) remaining = 0f;
            SetTimerText(remaining);
            yield return null;
        }

        // Freeze at 00:00
        SetTimerText(0f);

        if (openDoorWhenDone)
            doorManager.Open();

        // Keep trigger disabled until the player exits (OnTriggerExit will re-enable it)
        _running = false;
    }

    private void SetTimerText(float secondsRemaining)
    {
        if (!timerText) return;

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _running = false;

        SetTimerText(0f);

        // Ensure trigger is usable when re-enabled
        if (_triggerCollider != null)
            _triggerCollider.enabled = true;

        if (resetDoorOnDisable && doorManager != null)
            doorManager.SetOpenImmediate(true);
    }
}
