using System;
using System.Collections.Generic;
using System.Threading;
using EventStore.ClientAPI;

namespace Tables
{
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
}