using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

public class StayOnPlaneToAdvance : MonoBehaviour
{
    [Header("Player body collider (strict match recommended)")]
    public Collider playerBodyCollider;

    [Header("Headset fallback for room-scale walking")]
    public Transform playerHead;
    public bool useHeadPositionFallback = true;
    public float horizontalMargin = 0.15f;

    [Header("State machine")]
    public ExperimentStateManager stateManager;

    private Collider _triggerCollider;
    private bool _armed;
    private bool _playerOnPlane;
    private bool _advanced;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        ResolvePlayerHead();
    }

    public void SetArmed(bool armed)
    {
        _armed = armed;
        _playerOnPlane = false;
        _advanced = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_armed) return;
        if (!IsPlayerBody(other)) return;

        _playerOnPlane = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerBody(other)) return;

        _playerOnPlane = false;
    }

    private void Update()
    {
        if (!_armed || _advanced) return;
        // Ignore Y while familiarity mode is active — advancing the condition
        // mid-familiarity invalidates the renderer/collider snapshot that gets
        // restored on the next X press, leaving walls in mixed visibility.
        if (FamilarityManager.IsFamiliarityActive) return;
        if (!_playerOnPlane && !IsHeadOverPlane()) return;

        if (IsYButtonPressed())
        {
            _advanced = true;
            QuestEventOutlet.Send("return_plane_held");
            if (stateManager != null)
                stateManager.OnReturnPlaneHeld();
        }
    }

    private bool IsYButtonPressed()
    {
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid &&
            leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed) &&
            pressed)
        {
            return true;
        }
        return false;
    }

    private bool IsPlayerBody(Collider other)
    {
        return playerBodyCollider != null && other == playerBodyCollider;
    }

    private bool IsHeadOverPlane()
    {
        if (!useHeadPositionFallback || playerHead == null || _triggerCollider == null)
            return false;
        if (!_triggerCollider.enabled || !_triggerCollider.gameObject.activeInHierarchy)
            return false;

        Bounds bounds = _triggerCollider.bounds;
        Vector3 point = playerHead.position;
        float margin = Mathf.Max(0f, horizontalMargin);

        return point.x >= bounds.min.x - margin &&
               point.x <= bounds.max.x + margin &&
               point.z >= bounds.min.z - margin &&
               point.z <= bounds.max.z + margin;
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
}
