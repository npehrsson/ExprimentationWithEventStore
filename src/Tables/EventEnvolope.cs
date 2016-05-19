using EventStore.ClientAPI;

namespace Tables
{
    public class EventEnvolope {
        public EventEnvolope(int eventNumber, string eventType, string streamId, Position? position, object eventData) {
            EventType = eventType;
            StreamId = streamId;
            EventNumber = eventNumber;
            EventType = EventType;
            EventData = eventData;
            Position = position;
        }

        public object EventData { get; }
        public string StreamId { get; }
        public int EventNumber { get; }
        public string EventType { get; }
        public Position? Position { get; }
        // ChangesetAffectedList?
    }
}