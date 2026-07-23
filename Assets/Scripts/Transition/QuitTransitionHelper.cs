using UnityEngine;

public class QuitTransitionTrigger : MonoBehaviour
{
    [Tooltip("The empty RectTransform (inside the Canvas) to use as the visual center of the wipe.")]
    public Transform focusPoint;

    // The Quit Button will call this method
    public void TriggerQuit()
    {
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.QuitGame(focusPoint);
        }
        else
        {
            // Fallback just in case testing without the manager
            Application.Quit();
        }
    }
}