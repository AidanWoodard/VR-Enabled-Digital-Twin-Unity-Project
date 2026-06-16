using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Dashboard
{
    [Serializable]
    public class DashboardRecordResponse : Message
    {
        public const string k_RosMessageName = "unity_vr_control/DashboardRecord";
        public override string RosMessageName => k_RosMessageName;

        public bool success;
        public string message;

        public DashboardRecordResponse()
        {
            this.success = false;
            this.message = string.Empty;
        }

        public DashboardRecordResponse(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        public static DashboardRecordResponse Deserialize(MessageDeserializer deserializer) => new DashboardRecordResponse(deserializer);

        private DashboardRecordResponse(MessageDeserializer deserializer)
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
            return $"DashboardRecordResponse: success={success} message={message}";
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
