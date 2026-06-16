using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Dashboard
{
    [Serializable]
    public class DashboardClearRequest : Message
    {
        public const string k_RosMessageName = "unity_vr_control/DashboardClear";
        public override string RosMessageName => k_RosMessageName;

        public int slot_id;

        public DashboardClearRequest()
        {
            this.slot_id = 0;
        }

        public DashboardClearRequest(int slot_id)
        {
            this.slot_id = slot_id;
        }

        public static DashboardClearRequest Deserialize(MessageDeserializer deserializer) => new DashboardClearRequest(deserializer);

        private DashboardClearRequest(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.slot_id);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.slot_id);
        }

        public override string ToString()
        {
            return $"DashboardClearRequest: slot_id={slot_id}";
        }

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
