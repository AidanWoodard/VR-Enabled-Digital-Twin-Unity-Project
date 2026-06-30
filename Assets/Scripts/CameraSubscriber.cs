using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using System.Diagnostics;

// Needed for compression types of incoming image data
using RosMessageTypes.Sensor;
using TMPro;

public class CameraSubscriber : MonoBehaviour
{
    public string rosTopicName = "/usb_cam/image_raw";
    private RawImage targetUiCanvas;
    private Texture2D texture2D;
    private TextMeshProUGUI noFeedWarning;
    [SerializeField] private int targetWidth = 640;
    [SerializeField] private int targetHeight = 480;

    // volatile: written from ROS background thread, read from main thread in Update()
    private volatile CompressedImageMsg _pendingMessage = null;

    // Diagnostics
    private int _messagesReceived = 0;
    private int _messagesApplied = 0;
    private int _messagesDropped = 0;
    private float _diagWindowStart = -1f;
    private long _decodeMaxMs = 0;
    private long _decodeTotalMs = 0;
    private readonly Stopwatch _decodeTimer = new Stopwatch();

    void Start()
    {
        targetUiCanvas = this.transform.Find("LiveCameraFeed").GetComponent<RawImage>();
        noFeedWarning = this.transform.Find("NoFeedText").GetComponent<TextMeshProUGUI>();

        texture2D = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        targetUiCanvas.texture = texture2D;

        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>(rosTopicName, RenderImageMessage);
    }

    void Update()
    {
        // Diagnostics window
        if (_diagWindowStart < 0f)
        {
            _diagWindowStart = Time.realtimeSinceStartup;
        }
        else
        {
            float elapsed = Time.realtimeSinceStartup - _diagWindowStart;
            if (elapsed >= 5f)
            {
                float decodeAvgMs = _messagesApplied > 0 ? (float)_decodeTotalMs / _messagesApplied : 0f;
                UnityEngine.Debug.Log($"[{gameObject.name}] Over {elapsed:F1}s — received: {_messagesReceived} ({_messagesReceived / elapsed:F1}/s), dropped(overwritten): {_messagesDropped} ({_messagesDropped / elapsed:F1}/s), applied: {_messagesApplied} ({_messagesApplied / elapsed:F1}/s), decode: avg {decodeAvgMs:F2}ms max {_decodeMaxMs}ms, Unity FPS: {1f / Time.deltaTime:F1}");
                _messagesReceived = 0;
                _messagesApplied = 0;
                _messagesDropped = 0;
                _decodeMaxMs = 0;
                _decodeTotalMs = 0;
                _diagWindowStart = Time.realtimeSinceStartup;
            }
        }

        if (_pendingMessage == null)
            return;

        CompressedImageMsg msg = _pendingMessage;
        _pendingMessage = null;

        if (noFeedWarning.enabled)
            noFeedWarning.enabled = false;

        _decodeTimer.Restart();
        texture2D.LoadImage(msg.data);
        texture2D.Apply(false);
        _decodeTimer.Stop();

        long elapsedMs = _decodeTimer.ElapsedMilliseconds;
        _decodeTotalMs += elapsedMs;
        if (elapsedMs > _decodeMaxMs) _decodeMaxMs = elapsedMs;

        _messagesApplied++;
    }

    void RenderImageMessage(CompressedImageMsg imageMessage)
    {
        _messagesReceived++;
        if (_pendingMessage != null) _messagesDropped++;
        _pendingMessage = imageMessage;
    }
}
