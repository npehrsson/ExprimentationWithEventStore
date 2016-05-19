using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Tables {
    public class EventWriter {
        private readonly IEventStoreConnection _connection;
        private readonly ISerializer _serializer;

        public EventWriter(IEventStoreConnection connection, ISerializer serializer) {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            _connection = connection;
            _serializer = serializer;
        }

        public async Task Write(string stream, IEvent @event) {
            await Write(stream, new List<IEvent>() { @event });
        }

        public async Task Write(string stream, params IEvent[] events) {
            await Write(stream, events.ToList());
        }

        public async Task Write(string stream, IEnumerable<IEvent> events) {
            var convertedEvents =
                events.Select(
                    x =>
                        new EventData(x.EventId, x.EventType, true,
                            Encoding.UTF8.GetBytes(_serializer.Serialize(x)), null));
            // add changeset information
            await _connection.AppendToStreamAsync(stream, ExpectedVersion.Any, convertedEvents);
        }
    }
}