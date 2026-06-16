using System;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Dashboard
{
    [Serializable]
    public class DashboardQuerySlotsResponse : Message
    {
        public const string k_RosMessageName = "unity_vr_control/DashboardQuerySlots";
        public override string RosMessageName => k_RosMessageName;

        // Fixed-size array: one entry per slot (indices 0-4)
        public bool[] has_recording;

        public DashboardQuerySlotsResponse()
        {
            this.has_recording = new bool[5];
        }

        public DashboardQuerySlotsResponse(bool[] has_recording)
        {
            this.has_recording = has_recording;
        }

        public static DashboardQuerySlotsResponse Deserialize(MessageDeserializer deserializer) => new DashboardQuerySlotsResponse(deserializer);

        private DashboardQuerySlotsResponse(MessageDeserializer deserializer)
        {
            this.has_recording = new bool[5];
            for (int i = 0; i < 5; i++)
                deserializer.Read(out this.has_recording[i]);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            foreach (var v in this.has_recording)
                serializer.Write(v);
        }

        public override string ToString()
        {
            return $"DashboardQuerySlotsResponse: has_recording=[{string.Join(",", has_recording)}]";
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
