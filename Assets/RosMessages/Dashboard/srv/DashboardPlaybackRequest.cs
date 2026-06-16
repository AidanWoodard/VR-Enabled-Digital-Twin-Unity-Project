using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Dashboard
{
    [Serializable]
    public class DashboardPlaybackRequest : Message
    {
        public const string k_RosMessageName = "unity_vr_control/DashboardPlayback";
        public override string RosMessageName => k_RosMessageName;

        public int slot_id;
        public bool start;

        public DashboardPlaybackRequest()
        {
            this.slot_id = 0;
            this.start = false;
        }

        public DashboardPlaybackRequest(int slot_id, bool start)
        {
            this.slot_id = slot_id;
            this.start = start;
        }

        public static DashboardPlaybackRequest Deserialize(MessageDeserializer deserializer) => new DashboardPlaybackRequest(deserializer);

        private DashboardPlaybackRequest(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.slot_id);
            deserializer.Read(out this.start);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.slot_id);
            serializer.Write(this.start);
        }

        public override string ToString()
        {
            return $"DashboardPlaybackRequest: slot_id={slot_id} start={start}";
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
