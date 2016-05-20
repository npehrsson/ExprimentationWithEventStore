using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.AspNet.Http.Internal;

namespace Tables.Controllers {
    public class TableQuery {
        private readonly IEventStoreConnection _connection;
        private readonly EventEnvolopeFactory _factory;
        private readonly TableRuleEventApplier _eventApplier;
        private readonly EventStreamReader _streamReader;

        public TableQuery(IEventStoreConnection connection) {
            _connection = connection;
            _factory = new EventEnvolopeFactory();
            _factory.AddDefaultSerializer<TableCreatedEvent>();
            _factory.AddDefaultSerializer<CreatedColumnEvent>();
            _factory.AddDefaultSerializer<CreatedRowEvent>();
            _factory.AddDefaultSerializer<CellEditedEvent>();

            _streamReader = new EventStreamReader(_connection);
            _eventApplier = new TableRuleEventApplier();
        }

        public Table SingleWithSubscription(Guid id, double changesetVersion) {
            using (var task = new EventApplierTask<Table>(_factory)) {
                task.Start(GetStreamId(id), _connection, _eventApplier);
                task.Wait();

                return task.Result;
            }
        }

        public async Task<Table> SingleWithBatchLoop(Guid id, double changesetVersion) {
            var reader = new EventStreamReaderApplier<Table>(_streamReader, _factory, _eventApplier);
            return await reader.Read(GetStreamId(id), null);
        }

        public async Task<Table> SingleReadAllEventsAndThenAppned(Guid id, double changesetVersion) {
            Table state = null;
            var stream = GetStreamId(id);
            var events = await _streamReader.ToList(stream);

            foreach (var resolvedEvent in events) {
                EventEnvolope deserializedEvent;
                if (_factory.TryCreate(resolvedEvent, out deserializedEvent))
                    state = _eventApplier.Apply(state, deserializedEvent);
            }

            return state;
        }

        private static string GetStreamId(Guid id) {
            return "tables-" + id.ToString("N");
        }
    }

    public class CurrentRequestEventDiagnostics : IEventDiagnostics {
        private TimeSpan _totalElapsedJson = new TimeSpan(0);
        private TimeSpan _totalElapsedBinary = new TimeSpan(0);

        public void AddDeserializationDiagnostics(TimeSpan elapsedJsonDeserialization, TimeSpan elapsedBinaryDeserialization) {
            _totalElapsedJson = _totalElapsedJson.Add(elapsedJsonDeserialization);
            _totalElapsedBinary = _totalElapsedBinary.Add(elapsedBinaryDeserialization);

            var accessor = new HttpContextAccessor();
            accessor.HttpContext.Items["TotalJson"] = _totalElapsedJson;
            accessor.HttpContext.Items["TotalBinary"] = _totalElapsedBinary;
        }

        public void ApplyDiagnostics(string eventType, TimeSpan timespan) {
            throw new NotImplementedException();
        }

        public void ReadFromStreamDiagnostics(TimeSpan elapsedTime, int events) {
            throw new NotImplementedException();
        }
    }
}