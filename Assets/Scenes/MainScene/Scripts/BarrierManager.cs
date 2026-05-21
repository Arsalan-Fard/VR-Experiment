using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Opens turnstile barriers after a configurable delay when the player
/// enters corresponding trigger zones.
///
/// Place on each condition's Barriers parent (or any object in the condition).
/// Assign trigger→turnstile pairs in the Inspector.
///
/// Each turnstile must have HingeDoor / HingeDoor1 children (empty pivots
/// positioned at the pipe hinge points) with GlassDoor / GlassDoor1
/// already parented underneath them in the scene hierarchy.
/// </summary>
public class BarrierManager : MonoBehaviour
{
    public enum ControllerButton
    {
        Primary,
        Secondary,
        Grip,
        Trigger
    }

    [Serializable]
    public struct BarrierEntry
    {
        [Tooltip("Trigger collider (BoxCollider, isTrigger) that the player walks through")]
        public Collider triggerZone;

        [Tooltip("Turnstile root transform (parent of HingeDoor, HingeDoor1, etc.)")]
        public Transform turnstile;
    }

    [Header("Barrier Pairs (trigger → turnstile, in order)")]
    public BarrierEntry[] barriers;

    [Header("Player")]
    [Tooltip("Assign the XR Origin body collider so only the player triggers barriers")]
    public Collider playerBodyCollider;

    [Tooltip("Optional headset/camera transform. Used as a fallback for physical room-scale walking.")]
    public Transform playerHead;

    [Tooltip("Also open barriers when the headset position enters the trigger volume.")]
    public bool useHeadPositionFallback = true;

    [Tooltip("Small radius around the headset position for fallback trigger checks.")]
    public float headTriggerRadius = 0.15f;

    [Header("Timing")]
    [Tooltip("Per-barrier delays in seconds, mapped by index (e.g. [20,30,60,120,90])")]
    public float[] openDelays;

    [Tooltip("Fallback delay if openDelays is null, empty, or shorter than the barrier count")]
    public float defaultOpenDelay = 10f;

    [Header("Glass Room Lock Addition")]
    [Tooltip("Reference to the glass room RoomManager whose lockSeconds will be added to specific barriers")]
    public RoomManager glassRoom;

    [Tooltip("Barrier indices whose delay = openDelays[i] + glassRoom.lockSeconds (e.g. [4] for Condition 1, [1,3] for Condition 2)")]
    public int[] addGlassRoomLockIndices;

    /// <summary>Raised once when every barrier in the array has opened.</summary>
    public event System.Action OnAllBarriersOpen;

    [Header("Manual Barrier Control")]
    [Tooltip("Allows an experimenter to open/close barriers without waiting for trigger/timer logic.")]
    public bool allowManualBarrierControl = true;

    [Tooltip("Controller used to manually open the next closed barrier.")]
    public XRNode manualOpenController = XRNode.LeftHand;

    [Tooltip("Button used to manually open the next closed barrier.")]
    public ControllerButton manualOpenButton = ControllerButton.Trigger;

    [Tooltip("Controller used to manually close the last opened barrier.")]
    public XRNode manualCloseController = XRNode.LeftHand;

    [Tooltip("Button used to manually close the last opened barrier.")]
    public ControllerButton manualCloseButton = ControllerButton.Grip;

    [Header("Open Animation")]
    [Tooltip("Angle (degrees) each glass panel swings open")]
    public float openAngle = 90f;

    [Tooltip("Duration of the swing animation (seconds)")]
    public float animationDuration = 0.8f;

    // --- runtime state ---
    private Transform[] _hingeA;          // HingeDoor (swings GlassDoor)
    private Transform[] _hingeB;          // HingeDoor1 (swings GlassDoor1)
    private Quaternion[] _closedA, _openA;
    private Quaternion[] _closedB, _openB;
    private bool[] _isOpen;
    private bool[] _timerActive;
    private Coroutine[] _timerRoutines;
    private bool _manualOpenWasPressed;
    private bool _manualCloseWasPressed;

    private void Awake()
    {
        ResolvePlayerHead();

        int n = barriers.Length;
        _hingeA     = new Transform[n];
        _hingeB     = new Transform[n];
        _closedA    = new Quaternion[n];
        _openA      = new Quaternion[n];
        _closedB    = new Quaternion[n];
        _openB      = new Quaternion[n];
        _isOpen     = new bool[n];
        _timerActive = new bool[n];
        _timerRoutines = new Coroutine[n];

        for (int i = 0; i < n; i++)
        {
            var ts = barriers[i].turnstile;
            if (ts == null) continue;

            // Find the pre-placed hinge pivots in the scene
            var hingeDoor  = ts.Find("HingeDoor");
            var hingeDoor1 = ts.Find("HingeDoor1");

            if (hingeDoor != null)
            {
                _hingeA[i]  = hingeDoor;
                _closedA[i] = hingeDoor.localRotation;
                _openA[i]   = _closedA[i] * Quaternion.Euler(0f, openAngle, 0f);
            }

            if (hingeDoor1 != null)
            {
                _hingeB[i]  = hingeDoor1;
                _closedB[i] = hingeDoor1.localRotation;
                _openB[i]   = _closedB[i] * Quaternion.Euler(0f, -openAngle, 0f);
            }

            // Attach a tiny forwarder to each trigger so OnTriggerEnter routes back here
            var zone = barriers[i].triggerZone;
            if (zone != null)
            {
                var fwd = zone.gameObject.AddComponent<BarrierTriggerForwarder>();
                fwd.Init(this, i);
            }
        }
    }

    private void Update()
    {
        HandleManualBarrierInput();

        if (!useHeadPositionFallback || playerHead == null || barriers == null)
            return;

        for (int i = 0; i < barriers.Length; i++)
        {
            if (_isOpen[i] || _timerActive[i])
                continue;

            if (IsHeadInsideTrigger(barriers[i].triggerZone))
                StartBarrierTimer(i);
        }
    }

    /// <summary>True when the last barrier in the array has opened.</summary>
    public bool LastBarrierOpen =>
        _isOpen != null && _isOpen.Length > 0 && _isOpen[_isOpen.Length - 1];

    private float GetDelay(int index)
    {
        float delay = (openDelays != null && index < openDelays.Length && openDelays[index] > 0f)
            ? openDelays[index]
            : defaultOpenDelay;

        if (glassRoom != null && addGlassRoomLockIndices != null)
        {
            for (int i = 0; i < addGlassRoomLockIndices.Length; i++)
            {
                if (addGlassRoomLockIndices[i] == index)
                {
                    delay += glassRoom.lockSeconds;
                    break;
                }
            }
        }

        return delay;
    }

    /// <summary>Called by BarrierTriggerForwarder when something enters a trigger zone.</summary>
    public void OnPlayerEnteredTrigger(int index, Collider other)
    {
        if (playerBodyCollider != null && other != playerBodyCollider) return;

        StartBarrierTimer(index);
    }

    private void StartBarrierTimer(int index)
    {
        if (barriers == null || index < 0 || index >= barriers.Length)
            return;

        if (_isOpen[index] || _timerActive[index]) return;

        _timerRoutines[index] = StartCoroutine(DelayThenOpen(index));
    }

    private void HandleManualBarrierInput()
    {
        if (!allowManualBarrierControl || barriers == null || barriers.Length == 0)
            return;

        bool openPressed = IsManualButtonPressed(manualOpenController, manualOpenButton);
        if (openPressed && !_manualOpenWasPressed)
            OpenNextClosedBarrierManually();

        bool closePressed = IsManualButtonPressed(manualCloseController, manualCloseButton);
        if (closePressed && !_manualCloseWasPressed)
            CloseLastOpenBarrierManually();

        _manualOpenWasPressed = openPressed;
        _manualCloseWasPressed = closePressed;
    }

    private bool IsManualButtonPressed(XRNode controllerNode, ControllerButton button)
    {
        var controller = InputDevices.GetDeviceAtXRNode(controllerNode);
        if (!controller.isValid)
            return false;

        var usage = GetButtonUsage(button);

        return controller.TryGetFeatureValue(usage, out bool pressed) && pressed;
    }

    private static InputFeatureUsage<bool> GetButtonUsage(ControllerButton button)
    {
        switch (button)
        {
            case ControllerButton.Primary:
                return CommonUsages.primaryButton;
            case ControllerButton.Secondary:
                return CommonUsages.secondaryButton;
            case ControllerButton.Grip:
                return CommonUsages.gripButton;
            case ControllerButton.Trigger:
                return CommonUsages.triggerButton;
            default:
                return CommonUsages.primaryButton;
        }
    }

    private void OpenNextClosedBarrierManually()
    {
        for (int i = 0; i < barriers.Length; i++)
        {
            if (_isOpen[i])
                continue;

            if (_timerRoutines != null && _timerRoutines[i] != null)
            {
                StopCoroutine(_timerRoutines[i]);
                _timerRoutines[i] = null;
            }

            _timerActive[i] = false;

            string triggerName = barriers[i].triggerZone != null
                ? barriers[i].triggerZone.name : i.ToString();

            QuestEventOutlet.Send($"barrier_manual_open:{triggerName}");
            StartCoroutine(OpenBarrier(i, triggerName));
            return;
        }
    }

    private void CloseLastOpenBarrierManually()
    {
        for (int i = barriers.Length - 1; i >= 0; i--)
        {
            if (!_isOpen[i] || _timerActive[i])
                continue;

            string triggerName = barriers[i].triggerZone != null
                ? barriers[i].triggerZone.name : i.ToString();

            QuestEventOutlet.Send($"barrier_manual_close:{triggerName}");
            StartCoroutine(CloseBarrier(i, triggerName));
            return;
        }
    }

    private bool IsHeadInsideTrigger(Collider triggerZone)
    {
        if (triggerZone == null || !triggerZone.enabled || !triggerZone.gameObject.activeInHierarchy)
            return false;

        Vector3 point = playerHead.position;
        Vector3 closest = triggerZone.ClosestPoint(point);
        float radius = Mathf.Max(0.001f, headTriggerRadius);
        return (closest - point).sqrMagnitude <= radius * radius;
    }

    private void ResolvePlayerHead()
    {
        if (playerHead != null)
            return;

#if UNITY_2023_1_OR_NEWER
        var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
#else
        var origin = UnityEngine.Object.FindObjectOfType<XROrigin>();
#endif
        if (origin != null && origin.Camera != null)
        {
            playerHead = origin.Camera.transform;
            return;
        }

        if (Camera.main != null)
            playerHead = Camera.main.transform;
    }

    private IEnumerator DelayThenOpen(int index)
    {
        _timerActive[index] = true;

        float delay = GetDelay(index);

        string triggerName = barriers[index].triggerZone != null
            ? barriers[index].triggerZone.name : index.ToString();

        QuestEventOutlet.Send($"barrier_trigger:{triggerName}");
        QuestEventOutlet.Send($"barrier_timer_start:{triggerName}:{delay}s");

        yield return new WaitForSeconds(delay);

        QuestEventOutlet.Send($"barrier_timer_end:{triggerName}");
        _timerRoutines[index] = null;

        yield return OpenBarrier(index, triggerName);
    }

    private IEnumerator OpenBarrier(int index, string triggerName)
    {
        if (_isOpen[index])
            yield break;

        _isOpen[index] = true;
        _timerActive[index] = true;

        // Animate the glass panels swinging open
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));

            if (_hingeA[index])
                _hingeA[index].localRotation = Quaternion.Slerp(_closedA[index], _openA[index], t);
            if (_hingeB[index])
                _hingeB[index].localRotation = Quaternion.Slerp(_closedB[index], _openB[index], t);

            yield return null;
        }

        // Snap to exact final rotation
        if (_hingeA[index]) _hingeA[index].localRotation = _openA[index];
        if (_hingeB[index]) _hingeB[index].localRotation = _openB[index];

        _timerActive[index] = false;

        QuestEventOutlet.Send($"barrier_open:{triggerName}");

        if (index == barriers.Length - 1)
        {
            QuestEventOutlet.Send("last_barrier_open");
            OnAllBarriersOpen?.Invoke();
        }
    }

    private IEnumerator CloseBarrier(int index, string triggerName)
    {
        if (!_isOpen[index])
            yield break;

        _timerActive[index] = true;

        Quaternion startA = _hingeA[index] ? _hingeA[index].localRotation : Quaternion.identity;
        Quaternion startB = _hingeB[index] ? _hingeB[index].localRotation : Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));

            if (_hingeA[index])
                _hingeA[index].localRotation = Quaternion.Slerp(startA, _closedA[index], t);
            if (_hingeB[index])
                _hingeB[index].localRotation = Quaternion.Slerp(startB, _closedB[index], t);

            yield return null;
        }

        if (_hingeA[index]) _hingeA[index].localRotation = _closedA[index];
        if (_hingeB[index]) _hingeB[index].localRotation = _closedB[index];

        _isOpen[index] = false;
        _timerActive[index] = false;

        QuestEventOutlet.Send($"barrier_close:{triggerName}");
    }

    /// <summary>Reset all barriers to closed (useful when switching conditions).</summary>
    public void ResetAll()
    {
        StopAllCoroutines();

        _manualOpenWasPressed = false;
        _manualCloseWasPressed = false;

        if (_hingeA == null) return;

        for (int i = 0; i < barriers.Length; i++)
        {
            if (_hingeA[i]) _hingeA[i].localRotation = _closedA[i];
            if (_hingeB[i]) _hingeB[i].localRotation = _closedB[i];
            _isOpen[i] = false;
            _timerActive[i] = false;
            if (_timerRoutines != null)
                _timerRoutines[i] = null;
        }
    }

    private void OnDisable()
    {
        ResetAll();
    }
}
