using UnityEngine;

public class ControllerReference : MonoBehaviour
{
    [SerializeField] private GameObject referencePanel;

    [Tooltip("Local axis on this controller that points away from the palm. Tune if the gesture angle feels off.")]
    [SerializeField] private Vector3 palmLocalAxis = Vector3.up;

    [Tooltip("Degrees from world-up within which the palm must face to show the panel.")]
    [SerializeField] private float activationAngle = 45f;

    void Update()
    {
        if (referencePanel == null) return;

        Vector3 palmWorldDir = transform.TransformDirection(palmLocalAxis);
        bool isWatchGesture = Vector3.Dot(palmWorldDir, Vector3.up) >= Mathf.Cos(activationAngle * Mathf.Deg2Rad);

        referencePanel.SetActive(isWatchGesture);
    }
}
