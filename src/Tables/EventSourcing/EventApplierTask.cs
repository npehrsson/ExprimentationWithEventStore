using System;
using System.Diagnostics;
using System.Threading;
using EventStore.ClientAPI;

namespace Tables {
    public class EventApplierTask<T> : IDisposable {
        private readonly EventEnvolopeFactory _eventEnvolopeFactory;
        private T _result;

        public EventApplierTask(EventEnvolopeFactory eventEnvolopeFactory) {
            if (eventEnvolopeFactory == null) throw new ArgumentNullException(nameof(eventEnvolopeFactory));
            _eventEnvolopeFactory = eventEnvolopeFactory;
        }

        public string Stream { get; private set; }
        public IEventApplier<T> Applier { get; private set; }
        public T Result { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool Failed { get; private set; }
        public Exception Exception { get; private set; }
        public string FailedReason { get; private set; }
        public bool IsStarted => CancellationTokenSource != null;
        private EventStoreCatchUpSubscription Subscription { get; set; }
        private CancellationTokenSource CancellationTokenSource { get; set; }

        public void Start(string stream, IEventStoreConnection connection, IEventApplier<T> applier) {
            Start(stream, connection, applier, default(T), StreamPosition.Start);
        }

        public void Start(string stream, IEventStoreConnection connection, IEventApplier<T> applier, T state, int position) {
            if (IsStarted) {
                throw new InvalidOperationException("Task has already been started");
            }

            _result = state;
            CancellationTokenSource = new CancellationTokenSource();
            Stream = stream;
            Applier = applier;
            var settings = CatchUpSubscriptionSettings.Default;
            settings = new CatchUpSubscriptionSettings(settings.MaxLiveQueueSize, 4096, false, false);

            Subscription = connection.SubscribeToStreamFrom(Stream, position == 0 ? (int?)null : position, settings, OnEvent, CatchupComplete, OnSubscriptionDropped);
        }

        public void Cancel() {
            if (!IsStarted) {
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
            Result = _result;
            IsCompleted = true;
            CancellationTokenSource.Cancel();
            Dispose();
        }

        private void OnEvent(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent) {
            EventEnvolope eventEnvolope;
            if (!_eventEnvolopeFactory.TryCreate(resolvedEvent, out eventEnvolope)) {
                return;
            }

            var watch = Stopwatch.StartNew();
            _result = Applier.Apply(_result, eventEnvolope);
            EventSourcingDiagnostics.ApplyDiagnostics(eventEnvolope.EventType, watch.Elapsed);
        }

        private void OnSubscriptionDropped(EventStoreCatchUpSubscription subscription, SubscriptionDropReason reason, Exception exception) {
            if (IsCompleted) {
                return;
            }

            Failed = true;
            FailedReason = reason.ToString();
            Exception = exception;
            Dispose();
        }

        private void CatchupComplete(EventStoreCatchUpSubscription subscription) {
            Complete();
        }

        public void Dispose() {
            CancellationTokenSource?.Dispose();
            Subscription?.Stop();
        }
    }
}