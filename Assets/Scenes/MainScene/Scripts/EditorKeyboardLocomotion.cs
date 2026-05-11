#if UNITY_EDITOR
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public sealed class EditorKeyboardLocomotion : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2.5f;
    [SerializeField] float sprintMultiplier = 2f;
    [SerializeField] bool useCameraForward = true;

    CharacterController controller;
    Transform head;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!Application.isEditor || !Application.isPlaying)
            return;

#if UNITY_2023_1_OR_NEWER
        var origin = Object.FindFirstObjectByType<XROrigin>();
#else
        var origin = Object.FindObjectOfType<XROrigin>();
#endif
        if (origin == null || origin.GetComponent<EditorKeyboardLocomotion>() != null)
            return;

        origin.gameObject.AddComponent<EditorKeyboardLocomotion>();
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        var origin = GetComponent<XROrigin>();
        if (origin != null && origin.Camera != null)
            head = origin.Camera.transform;
        else if (Camera.main != null)
            head = Camera.main.transform;

        if (controller == null)
            Debug.LogWarning("[EditorKeyboardLocomotion] No CharacterController found on XR Origin; keyboard movement is disabled.", this);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (controller == null || keyboard == null)
            return;

        var input = Vector2.zero;

        if (keyboard.iKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            input.y += 1f;
        if (keyboard.kKey.isPressed || keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            input.y -= 1f;
        if (keyboard.lKey.isPressed || keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            input.x += 1f;
        if (keyboard.jKey.isPressed || keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            input.x -= 1f;

        if (input.sqrMagnitude < 0.0001f)
            return;

        input = Vector2.ClampMagnitude(input, 1f);

        var reference = useCameraForward && head != null ? head : transform;
        var forward = reference.forward;
        var right = reference.right;
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;
        if (right.sqrMagnitude < 0.001f)
            right = transform.right;

        forward.Normalize();
        right.Normalize();

        var speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        controller.Move((forward * input.y + right * input.x) * speed * Time.deltaTime);
    }
}
#endif
