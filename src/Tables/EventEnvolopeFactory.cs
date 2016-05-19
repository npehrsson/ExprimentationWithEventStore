using System;
using System.Collections.Generic;
using System.Text;
using EventStore.ClientAPI;

namespace Tables
{
    public class EventEnvolopeFactory {
        private readonly IDictionary<string, IDeserializer> _serializers;

        public EventEnvolopeFactory(IDictionary<string, IDeserializer> serializers) {
            if (serializers == null) throw new ArgumentNullException(nameof(serializers));
            _serializers = serializers;
        }

        public EventEnvolopeFactory() : this(new Dictionary<string, IDeserializer>()) {
        }

        public void AddDeserializer(string eventType, Type clrType) {
            _serializers.Add(eventType, new StandardDeserializer(eventType, clrType));
        }

        public void AddDeserializer(Type clrType) {
            _serializers.Add(clrType.Name, new StandardDeserializer(clrType.Name, clrType));
        }

        public bool TryCreate(ResolvedEvent @event, out EventEnvolope eventEnvolope) {
            IDeserializer serializer;
            if (!_serializers.TryGetValue(@event.Event.EventType, out serializer)) {
                eventEnvolope = null;
                return false;
            }

            var eventData = serializer.Deserialize(Encoding.UTF8.GetString(@event.Event.Data));
            eventEnvolope = new EventEnvolope(@event.OriginalEventNumber, @event.Event.EventType,
                @event.Event.EventStreamId, @event.OriginalPosition, eventData);

            return true;
        }
    }
}