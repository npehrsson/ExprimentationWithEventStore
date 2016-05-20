using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Tables {
    public class EventStreamReaderApplier<TState> {
        private readonly EventEnvolopeFactory _eventFactory;
        private readonly IEventApplier<TState> _applier;
        private readonly EventStreamReader _streamReader;

        public EventStreamReaderApplier(EventStreamReader streamReader, EventEnvolopeFactory eventFactory, IEventApplier<TState> applier) {
            if (streamReader == null) throw new ArgumentNullException(nameof(streamReader));
            if (eventFactory == null) throw new ArgumentNullException(nameof(eventFactory));
            if (applier == null) throw new ArgumentNullException(nameof(applier));

            _eventFactory = eventFactory;
            _applier = applier;
            _streamReader = streamReader;
        }

        public async Task<TState> Read(string stream, IEventFilter filter, int fromPosition, TState state) {
            await _streamReader.ReadAsync(stream, fromPosition, events => {
                foreach (var @event in events) {
                    state = ProcessEvent(@event, state, filter);
                }
            });

            return state;
        }

        private TState ProcessEvent(ResolvedEvent @event, TState state, IEventFilter filter) {
            EventEnvolope deserializedEvent;
            if (!_eventFactory.TryCreate(@event, out deserializedEvent) || (filter != null && !filter.Filter(deserializedEvent))) {
                return state;
            }

            var watch = Stopwatch.StartNew();
            state = _applier.Apply(state, deserializedEvent);
            EventSourcingDiagnostics.ApplyDiagnostics(deserializedEvent.EventType, watch.Elapsed);

            return state;
        }

        public async Task<TState> Read(string stream, IEventFilter filter) {
            return await Read(stream, filter, StreamPosition.Start, default(TState));
        }
    }
}