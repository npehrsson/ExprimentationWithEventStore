using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Tables {
    public class TableRuleCreatedEvent : IEvent {
        public TableRuleCreatedEvent(Guid tableId, string name) {
            TableId = tableId;
            Name = name;
            EventId = Guid.NewGuid();
        }

        public Guid TableId { get; private set; }
        public Guid EventId { get; private set; }
        public string Name { get; private set; }
        public string EventType => nameof(TableRuleCreatedEvent);
    }

    public interface IEventHandler {
        string EventType { get; }
        object Handle(object stateContext, EventEnvolope @event);
    }

    public class EventEnvolope {
        public EventEnvolope(int eventNumber, string eventType, string streamId, Position position, object eventData) {
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
        public Position Position { get; }
        // ChangesetAffectedList?
        // Revision
    }

    public interface IEventApplier {
        void Apply(EventEnvolope eventEnvolope);
    }

    public interface IDeserializer {
        string EventType { get; }
        object Deserialize(object data);
    }

    public interface IEvent {
        Guid EventId { get; }
        string EventType { get; }
    }

    public abstract class EventApplierTask<T> where T : IEventApplier {
        private readonly IDictionary<string, IDeserializer> _eventSerializers;

        protected EventApplierTask(IDictionary<string, IDeserializer> eventSerializers) {
            if (eventSerializers == null) throw new ArgumentNullException(nameof(eventSerializers));
            _eventSerializers = eventSerializers;
        }

        public string Stream { get; private set; }
        public T Applier { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool Failed { get; private set; }
        public Exception Exception { get; private set; }
        public string FailedReason { get; private set; }
        public bool IsStarted => CancellationTokenSource != null;
        private EventStoreCatchUpSubscription Subscription { get; set; }
        private CancellationTokenSource CancellationTokenSource { get; set; }

        public void Start(string stream, IEventStoreConnection connection, T applier) {
            if (CancellationTokenSource != null) {
                throw new InvalidOperationException("Task has already been started");
            }

            CancellationTokenSource = new CancellationTokenSource();
            Stream = stream;
            Applier = applier;
            Subscription = connection.SubscribeToStreamFrom(Stream, null, CatchUpSubscriptionSettings.Default, OnEvent, CatchupComplete, OnSubscriptionDropped);
        }

        public void Cancel() {
            if (IsStarted) {
                throw new InvalidOperationException("The task must have been started first");
            }
            if (IsCompleted || Failed) {
                return;
            }

            Failed = true;
            FailedReason = "Cancelled by user";
            CancellationTokenSource.Cancel();
            Dispose();
        }

        public void Wait() {
            if (CancellationTokenSource.IsCancellationRequested) {
                return;
            }

            CancellationTokenSource.Token.WaitHandle.WaitOne();
        }

        internal void Complete() {
            IsCompleted = true;
            CancellationTokenSource.Cancel();
            Dispose();
        }

        internal void OnEvent(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent) {
            IDeserializer eventSerializer;
            if (!_eventSerializers.TryGetValue(resolvedEvent.Event.EventType, out eventSerializer)) {
                // Ignore events we can't handle
                return;
            }

            var deserializedEvent = eventSerializer.Deserialize(resolvedEvent);
            Applier.Apply(new EventEnvolope(
                resolvedEvent.OriginalEventNumber,
                resolvedEvent.OriginalEvent.EventType,
                resolvedEvent.OriginalEvent.EventStreamId,
                resolvedEvent.OriginalPosition.Value,
                deserializedEvent));
        }

        internal void OnSubscriptionDropped(EventStoreCatchUpSubscription subscription, SubscriptionDropReason reason, Exception exception) {
            Failed = true;
            FailedReason = reason.ToString();
            Exception = exception;
            Dispose();
        }

        internal void CatchupComplete(EventStoreCatchUpSubscription subscription) {
            Complete();
        }

        private void Dispose() {
            CancellationTokenSource.Dispose();
            Subscription.Stop();
        }
    }

    public class EventListener : IDisposable {
        private readonly IEventStoreConnection _connection;
        private EventStoreSubscription _subscription;
        private readonly IDictionary<string, IEventHandler> _handlers;

        public EventListener(IEventStoreConnection connection) {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            _connection = connection;
            _handlers = new Dictionary<string, IEventHandler>();
        }

        public void Register(IEventHandler handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers.Add(handler.EventType, handler);
        }

        public async Task StartAsync() {
            await _connection.ConnectAsync();
        }

        public async Task Subscribe() {
         

            _subscription = await _connection.SubscribeToAllAsync(true, OnEvent, OnSubscriptionDropped);
        }

        public void OnEvent(EventStoreSubscription subscription, ResolvedEvent resolvedEvent) {
            var data = resolvedEvent.Event.Data;

        }

        public void OnSubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exception) {

        }

        public void Dispose() {
            _subscription?.Dispose();
        }
    }