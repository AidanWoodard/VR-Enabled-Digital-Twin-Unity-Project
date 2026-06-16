using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Dashboard
{
    [Serializable]
    public class DashboardPlaybackResponse : Message
    {
        public const string k_RosMessageName = "unity_vr_control/DashboardPlayback";
        public override string RosMessageName => k_RosMessageName;

        public bool success;
        public string message;

        public DashboardPlaybackResponse()
        {
            this.success = false;
            this.message = string.Empty;
        }

        public DashboardPlaybackResponse(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        public static DashboardPlaybackResponse Deserialize(MessageDeserializer deserializer) => new DashboardPlaybackResponse(deserializer);

        private DashboardPlaybackResponse(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.success);
            deserializer.Read(out this.message);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.success);
            serializer.Write(this.message);
        }

        public override string ToString()
        {
            return $"DashboardPlaybackResponse: success={success} message={message}";
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize, MessageSubtopic.Response);
        }
    }
}
