using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;

// Needed for compression types of incoming image data
using RosMessageTypes.Sensor;
using TMPro;

public class CameraSubscriber : MonoBehaviour
{
    public string rosTopicName = "/usb_cam/image_raw";
    private RawImage targetUiCanvas;
    private Texture2D texture2D;
    private TextMeshProUGUI noFeedWarning;
    void Start()
    {
        targetUiCanvas = this.transform.Find("LiveCameraFeed").GetComponent<RawImage>();
        noFeedWarning = this.transform.Find("NoFeedText").GetComponent<TextMeshProUGUI>();
        //Transform test = transform.Find("NoFeedText");

        //foreach (Component c in test.GetComponents<Component>())
        //{
        //    if (c != null)
        //    {
        //        Debug.Log($" -> Component: {c.GetType().Name}");
        //        Debug.Log($" -> Component: {c.GetType().Name}");
        //    }
        //}

        // Initialize an empty texture texture (matching your camera's 640x480 resolution)
        texture2D = new Texture2D(1280, 960, TextureFormat.RGB24, false);
        targetUiCanvas.texture = texture2D;

        // Register the subscriber directly with the ROS-TCP network manager
        ROSConnection.GetOrCreateInstance().Subscribe<ImageMsg>(rosTopicName, RenderImageMessage);

        // Use instead to handle compressed image type. Must be subscribed with the same data type
        //
        //ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>(rosTopicName, RenderImageMessage);
    }

    void RenderImageMessage(ImageMsg imageMessage)
    {
        // Disable "no camera" warning
        if (noFeedWarning.enabled)
        {
            noFeedWarning.enabled = false;
        }

        // Load the raw byte array coming from the ROS topic directly into the Unity texture
        texture2D.LoadRawTextureData(imageMessage.data);
        texture2D.Apply(); // Apply changes to render it on screen

        // Use instead to handle compressed image types
        // Change ImageMsg to CompressedImageMsg
        //
        //texture2D.LoadImage(imageMessage.data);
        //texture2D.Apply();
    }
}
