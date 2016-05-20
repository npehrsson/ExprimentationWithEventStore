using System;
using System.Collections.Generic;

namespace Tables {
    public static class EventSourcingDiagnostics {
        private static readonly IList<IEventDiagnostics> Listeners = new List<IEventDiagnostics>();
        public static void RegisterListener(IEventDiagnostics listener) {
            Listeners.Add(listener);
        }

        public static void DeserializationDiagnostics(TimeSpan elapsedJsonDeserialization, TimeSpan elapsedBinaryDeserialization) {
            ApplyToAll(x => { x.AddDeserializationDiagnostics(elapsedJsonDeserialization, elapsedBinaryDeserialization); });
        }

        public static void ApplyDiagnostics(string eventType, TimeSpan timespan) {
            ApplyToAll(x => { x.ApplyDiagnostics(eventType, timespan); });
        }

        public static void ReadFromStreamDiagnostics(TimeSpan elapsedTime, int events) {
            ApplyToAll(x => x.ReadFromStreamDiagnostics(elapsedTime, events));
        }

        private static void ApplyToAll(Action<IEventDiagnostics> diagnosticsActions) {
            foreach (var listener in Listeners) {
                diagnosticsActions(listener);
            }
        }
    }
}