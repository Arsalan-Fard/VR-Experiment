using System.Collections;
using UnityEngine;

public class BoxChoiceManager : MonoBehaviour
{
    public GameObject choosePanel;
    public GameObject nextPanel;
    public DoorManager doorManager;

    [Header("Visibility Control")]
    public GameObject gatedObject; // hidden before choice, shown after

    [Header("Start Room Door Auto-Close")]
    public DoorManager startRoomDoorManager;  // optional; if null, uses doorManager
    public float closeAfterSeconds = 30f;

    private bool _chosen;
    private Coroutine _closeRoutine;

    void Start()
    {
        _chosen = false;

        if (choosePanel) choosePanel.SetActive(true);
        if (nextPanel) nextPanel.SetActive(false);

        if (gatedObject) gatedObject.SetActive(false);
    }

    public void ChooseBlack() => Choose();
    public void ChooseWhite() => Choose();

    private void Choose()
    {
        if (_chosen) return;
        _chosen = true;

        // UI transition
        if (choosePanel) choosePanel.SetActive(false);
        if (nextPanel) nextPanel.SetActive(true);

        // Reveal gated object
        if (gatedObject) gatedObject.SetActive(true);

        // Open the door now
        if (doorManager) doorManager.Open();

        // Auto-close the start room door after delay
        var doorToClose = startRoomDoorManager != null ? startRoomDoorManager : doorManager;
        if (doorToClose != null)
        {
            if (_closeRoutine != null) StopCoroutine(_closeRoutine);
            _closeRoutine = StartCoroutine(CloseDoorAfterDelay(doorToClose, closeAfterSeconds));
        }

        Debug.Log("[Experiment] Box chosen");
    }

    private IEnumerator CloseDoorAfterDelay(DoorManager dm, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dm != null) dm.Close();
    }
}
