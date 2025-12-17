using System.Collections;
using UnityEngine;

public class RevealManager : MonoBehaviour
{
    [Header("Door")]
    public DoorManager doorManager;

    [Header("XR Rig / XR Origin")]
    public Transform xrOrigin; // drag your XR Origin here

    [Header("Reset Pose (from your screenshot)")]
    public Vector3 resetPosition = new Vector3(2.782f, 0f, -3.672f);
    public Vector3 resetEulerAngles = new Vector3(0f, 0f, 0f);

    [Header("Timing")]
    public float secondsBeforeReset = 20f;

    [Header("Behavior")]
    public bool triggerOnce = true;

    bool _triggered;
    Coroutine _resetRoutine;

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && _triggered) return;
        if (!doorManager || !xrOrigin) return;

        // Accept either the XR Origin itself or any child collider (hands/camera, etc.)
        if (other.transform != xrOrigin && !other.transform.IsChildOf(xrOrigin)) return;

        _triggered = true;

        // Open door (reveal)
        doorManager.Open();

        // Start/reset timer to reposition user
        if (_resetRoutine != null) StopCoroutine(_resetRoutine);
        _resetRoutine = StartCoroutine(ResetAfterDelay());
    }

    IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(secondsBeforeReset);

        // Reset XR Origin pose
        xrOrigin.SetPositionAndRotation(
            resetPosition,
            Quaternion.Euler(resetEulerAngles)
        );

        // Optional: if you want to re-trigger again after reset, set triggerOnce = false
        // or manually set _triggered = false here.
    }
}
