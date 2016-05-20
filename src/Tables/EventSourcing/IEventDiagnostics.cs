using System;

namespace Tables {
    public interface IEventDiagnostics {
        void AddDeserializationDiagnostics(TimeSpan elapsedJsonDeserialization, TimeSpan elapsedBinaryDeserialization);
        void ApplyDiagnostics(string eventType, TimeSpan timespan);
        void ReadFromStreamDiagnostics(TimeSpan elapsedTime, int events);
    }
}