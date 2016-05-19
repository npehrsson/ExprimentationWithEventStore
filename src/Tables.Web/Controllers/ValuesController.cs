using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;

namespace Tables.Controllers {
    [Route("")]
    public class ValuesController : Controller {

        // GET: api/values
        [HttpGet("create")]
        public async Task<string> Create() {
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();
                var writer = new EventWriter(connection, new CustomSerializer());
                var createdEvent = new TableCreatedEvent(Guid.NewGuid(), "Table rule 1");
                await writer.Write("tables-" + createdEvent.TableId.ToString("N"), createdEvent);

                return createdEvent.TableId.ToString("N");
            }
        }

        // GET api/values/5
        [HttpGet("/get/{id}")]
        public async Task<JsonResult> Get(Guid id) {
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();

                var query = new TableQuery(connection);

                var result = await query.Single(id, 0);

                return new JsonResult(result);
            }
        }

        [HttpGet("/import")]
        public async Task<string> Import(int rows, int columns) {
            using (var connection = new ConnectionFactory().Create()) {
                await connection.ConnectAsync();
                var writer = new EventWriter(connection, new CustomSerializer());

                var events = new List<IEvent>();

                var createdEvent = new TableCreatedEvent(Guid.NewGuid(), "Table rule 1");
                events.Add(createdEvent);

                var columnList = new List<Guid>();

                for (var column = 0; column < columns; column++) {
                    var columnId = Guid.NewGuid();
                    events.Add(new CreatedColumnEvent(columnId, "Column " + column));
                    columnList.Add(columnId);
                }

                for (var row = 0; row < rows; row++) {
                    var rowEvent = new CreatedRowEvent(Guid.NewGuid());
                    events.Add(rowEvent);

                    foreach (var column in columnList) {
                        events.Add(new CellEditedEvent(rowEvent.RowId, column, "Data"));
                    }
                }

                await writer.Write("tables-" + createdEvent.TableId.ToString("N"), events);

                return createdEvent.TableId.ToString("N");
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value) {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value) {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id) {
        }
    }


    public class CustomSerializer : ISerializer {
        public string Serialize(IEvent @event) {
            return JsonConvert.SerializeObject(@event);
        }
    }

    public class ConnectionFactory {
     

        public IEventStoreConnection Create() {
            var connectionSettings = ConnectionSettings
                .Create()
                .UseConsoleLogger()
                .KeepReconnecting()
                .SetDefaultUserCredentials(new UserCredentials(UserName, Password))
                //.UseSslConnection("", true)
                .Build();

            return EventStoreConnection.Create(connectionSettings, new Uri(Uri));
        }
    }

    public class TableQuery {
        private readonly EventStreamReader<Table> _reader;

        public TableQuery(IEventStoreConnection connection) {
            var factory = new EventEnvolopeFactory();
            factory.AddDeserializer(typeof(TableCreatedEvent));
            factory.AddDeserializer(typeof(CreatedColumnEvent));
            factory.AddDeserializer(typeof(CreatedRowEvent));
            factory.AddDeserializer(typeof(CellEditedEvent));

            _reader = new EventStreamReader<Table>(connection, factory, new TableRuleEventApplier());
        }

        public async Task<Table> Single(Guid id, double changesetVersion) {
            return await _reader.Read("tables-" + id.ToString("N"), null);
        }
    }

    public class TableRuleEventApplier : IEventApplier<Table> {
        public Table Apply(Table state, EventEnvolope eventEnvolope) {
            switch (eventEnvolope.EventType) {
                case nameof(TableCreatedEvent):
                    var createdEvent = (TableCreatedEvent)eventEnvolope.EventData;
                    var rule = new Table(createdEvent.TableId) {
                        Name = createdEvent.Name,
                        CurrentVersion = eventEnvolope.EventNumber
                    };
                    return rule;
                case nameof(CreatedRowEvent):
                    var rowEvent = (CreatedRowEvent)eventEnvolope.EventData;
                    state.AddRow(rowEvent.RowId);
                    return state;
                case nameof(CreatedColumnEvent):
                    var column = (CreatedColumnEvent)eventEnvolope.EventData;
                    state.AddColumn(column.ColumnId, column.Name);
                    return state;
                case nameof(CellEditedEvent):
                    var cellEditEvent = (CellEditedEvent)eventEnvolope.EventData;
                    state.EditCell(cellEditEvent.RowId, cellEditEvent.ColumnId, cellEditEvent.Value);
                    return state;
                default:
                    return state;
            }
        }
    }

    public class EventStreamReader<TState> {
        private readonly IEventStoreConnection _connection;
        private readonly EventEnvolopeFactory _eventFactory;
        private readonly IEventApplier<TState> _applier;
        private const int BatchSize = 1000;

        public EventStreamReader(IEventStoreConnection connection, EventEnvolopeFactory eventFactory, IEventApplier<TState> applier) {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            _connection = connection;
            _eventFactory = eventFactory;
            _applier = applier;
        }

        public async Task<TState> Read(string stream, IEventFilter filter, int fromPosition, TState state) {
            StreamEventsSlice slice;
            do {
                slice = await _connection.ReadStreamEventsForwardAsync(stream, fromPosition, BatchSize, false);
                foreach (var @event in slice.Events) {
                    EventEnvolope deserializedEvent;
                    if (_eventFactory.TryCreate(@event, out deserializedEvent) &&
                        (filter == null || filter.Filter(deserializedEvent))) {
                        state = _applier.Apply(state, deserializedEvent);
                    }
                }
                slice = await _connection.ReadStreamEventsForwardAsync(stream, slice.NextEventNumber, BatchSize, false);
            } while (!slice.IsEndOfStream);

            return state;
        }

        public async Task<TState> Read(string stream, IEventFilter filter) {
            return await Read(stream, filter, StreamPosition.Start, default(TState));
        }
    }

    public interface IEventApplier<TState> {
        TState Apply(TState state, EventEnvolope eventEnvolope);
    }

    public interface IEventFilter {
        bool Filter(EventEnvolope @event);
    }
}
