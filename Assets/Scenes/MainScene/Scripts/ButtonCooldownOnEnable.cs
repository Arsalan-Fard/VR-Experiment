using UnityEngine;

/// <summary>
/// Attach to any UI panel that appears as a result of another button being
/// clicked. When the panel is enabled, its CanvasGroup is briefly made
/// non-interactable so a still-pressed trigger / lingering poke from the
/// previous button can't carry through and click whatever button happens to
/// appear under it.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ButtonCooldownOnEnable : MonoBehaviour
{
    [Tooltip("Seconds the panel ignores clicks after it becomes active.")]
    public float cooldownSeconds = 0.25f;

    private CanvasGroup _cg;
    private float _enableUntilRealtime;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (_cg == null) _cg = GetComponent<CanvasGroup>();
        _cg.interactable = false;
        _enableUntilRealtime = Time.realtimeSinceStartup + cooldownSeconds;
    }

    void Update()
    {
        if (!_cg.interactable && Time.realtimeSinceStartup >= _enableUntilRealtime)
            _cg.interactable = true;
    }
}
