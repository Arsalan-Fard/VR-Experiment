using System.Collections;
using UnityEngine;

public class ExperimentStateManager : MonoBehaviour
{
    [Header("Two reveal triggers (assign once, common scene objects)")]
    public RevealManager triggerAtStartSide;  // near where user starts in Condition 1
    public RevealManager triggerAtOtherSide;  // opposite end

    [Header("Start-room choice elements to hide after Condition 1")]
    public GameObject[] startChoiceElements; // boxes + choice UI, etc.

    [Header("Condition groups (leave empty to auto-fill from children)")]
    public GameObject[] conditionGroups;     // Condition1..Condition4

    [Header("Timing")]
    public float waitAfterRevealSeconds = 20f;

    private int _current = 0;
    private bool _advancing = false;

    private void Awake()
    {
        if (conditionGroups == null || conditionGroups.Length == 0)
        {
            int n = transform.childCount;
            conditionGroups = new GameObject[n];
            for (int i = 0; i < n; i++)
                conditionGroups[i] = transform.GetChild(i).gameObject;
        }
    }

    private void Start()
    {
        _current = 0; // Condition 1 by default
        ApplyCondition(_current);
    }

    public void OnRevealReached()
    {
        if (_advancing) return;
        StartCoroutine(AdvanceRoutine());
    }

    private IEnumerator AdvanceRoutine()
    {
        _advancing = true;

        if (waitAfterRevealSeconds > 0f)
            yield return new WaitForSeconds(waitAfterRevealSeconds);

        _current++;

        if (_current >= conditionGroups.Length)
        {
            Debug.Log("[ExperimentStateManager] Experiment finished.");
            // Disable both as reveal triggers
            ConfigureTriggers(activeReveal: null, showSuccess: false);
            yield break;
        }

        ApplyCondition(_current);

        _advancing = false;
    }

    private void ApplyCondition(int index)
    {
        // Enable only the active condition layout
        for (int i = 0; i < conditionGroups.Length; i++)
        {
            if (conditionGroups[i] != null)
                conditionGroups[i].SetActive(i == index);
        }

        // Boxes/choice only in Condition 1
        bool showChoice = (index == 0);
        if (startChoiceElements != null)
        {
            foreach (var go in startChoiceElements)
                if (go != null) go.SetActive(showChoice);
        }

        // Swap reveal side each condition:
        // C1: other side reveal
        // C2: start side reveal
        // C3: other side reveal
        // C4: start side reveal
        bool revealAtOtherSide = (index % 2 == 0);
        RevealManager activeReveal = revealAtOtherSide ? triggerAtOtherSide : triggerAtStartSide;

        // Success only in Condition 4
        bool showSuccess = (index == 3);

        ConfigureTriggers(activeReveal, showSuccess);

        Debug.Log($"[ExperimentStateManager] Condition {index + 1} active. Reveal={(activeReveal ? activeReveal.name : "None")} Success={showSuccess}");
    }

    private void ConfigureTriggers(RevealManager activeReveal, bool showSuccess)
    {
        if (triggerAtStartSide)
            triggerAtStartSide.ConfigureForCondition(activeReveal == triggerAtStartSide, showSuccess);

        if (triggerAtOtherSide)
            triggerAtOtherSide.ConfigureForCondition(activeReveal == triggerAtOtherSide, showSuccess);
    }
}
