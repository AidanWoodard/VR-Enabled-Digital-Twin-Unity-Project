using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Dashboard
{
    [Serializable]
    public class DashboardClearResponse : Message
    {
        public const string k_RosMessageName = "unity_vr_control/DashboardClear";
        public override string RosMessageName => k_RosMessageName;

        public bool success;
        public string message;

        public DashboardClearResponse()
        {
            this.success = false;
            this.message = string.Empty;
        }

        public DashboardClearResponse(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        public static DashboardClearResponse Deserialize(MessageDeserializer deserializer) => new DashboardClearResponse(deserializer);

        private DashboardClearResponse(MessageDeserializer deserializer)
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
            return $"DashboardClearResponse: success={success} message={message}";
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
