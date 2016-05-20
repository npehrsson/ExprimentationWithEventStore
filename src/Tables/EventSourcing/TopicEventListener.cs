using System;
using EventStore.ClientAPI;

namespace Tables
{
    public class TopicEventListener<T> : IDisposable {
        private readonly EventEnvolopeFactory _eventEnvolopeFactory;
        private readonly IEventApplier<T> _applier;
        private EventStoreAllCatchUpSubscription _subscription;

        public TopicEventListener(EventEnvolopeFactory eventEnvolopeFactory, IEventApplier<T> applier) {
            if (eventEnvolopeFactory == null) throw new ArgumentNullException(nameof(eventEnvolopeFactory));
            _eventEnvolopeFactory = eventEnvolopeFactory;
            _applier = applier;
        }

        public bool IsRunning => _subscription != null;
        public bool HasLiveStreamStarted { get; private set; }
        public T State { get; private set; }

        public void StartAsync(Position position, IEventStoreConnection connection) {
            if (IsRunning) {
                throw new InvalidOperationException("Has already been started");
            }

            _subscription = connection.SubscribeToAllFrom(
                position,
                CatchUpSubscriptionSettings.Default,
                EventAppeared,
                LiveProcessingStarted,
                SubscriptionDropped);
        }

        public void Stop() {
            if (!IsRunning) {
                throw new InvalidOperationException("Is not running");
            }

            _subscription.Stop();
            _subscription = null;
        }

        private void SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, SubscriptionDropReason subscriptionDropReason, Exception exception) {
            _subscription = null;
            _applier.StreamingStopped();
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription eventStoreCatchUpSubscription) {
            HasLiveStreamStarted = true;
            _applier.LiveStreamStarted();
        }

        private void EventAppeared(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, ResolvedEvent resolvedEvent) {
            EventEnvolope @event;
            if (!_eventEnvolopeFactory.TryCreate(resolvedEvent, out @event)) {
                return;
            }

            State = _applier.Apply(State, @event);
        }

        public void Dispose() {
            _subscription?.Stop();
        }
    }
}