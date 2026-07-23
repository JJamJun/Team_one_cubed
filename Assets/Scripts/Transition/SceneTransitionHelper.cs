using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("The exact name of the scene to load.")]
    public string sceneToLoad;

    [Tooltip("Vanishing point")]
    public Transform focusPoint;

    public void TriggerTransition()
    {
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.ChangeScene(sceneToLoad, focusPoint);
        }
        else
        {
            Debug.LogWarning("No TransitionManager found in the scene!");
        }
    }
}