using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Dashboard
{
    [Serializable]
    public class DashboardQuerySlotsRequest : Message
    {
        public const string k_RosMessageName = "unity_vr_control/DashboardQuerySlots";
        public override string RosMessageName => k_RosMessageName;

        public DashboardQuerySlotsRequest() { }

        public static DashboardQuerySlotsRequest Deserialize(MessageDeserializer deserializer) => new DashboardQuerySlotsRequest(deserializer);

        private DashboardQuerySlotsRequest(MessageDeserializer deserializer) { }

        public override void SerializeTo(MessageSerializer serializer) { }

        public override string ToString() => "DashboardQuerySlotsRequest";

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
