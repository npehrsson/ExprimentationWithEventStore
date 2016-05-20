using System.Collections.Generic;

namespace Tables {
    public class EventApplier<TState> : IEventApplier<TState> {
        private readonly IDictionary<string, IEventHandler<TState>> _handlers;

        public EventApplier() {
            _handlers = new Dictionary<string, IEventHandler<TState>>();
        }

        public void RegisterHandler(IEventHandler<TState> handler) {
            _handlers.Add(handler.EventType, handler);
        }

        public TState Apply(TState state, EventEnvolope eventEnvolope) {
            IEventHandler<TState> handler;
            return _handlers.TryGetValue(eventEnvolope.EventType, out handler) ?
                handler.Handle(state, eventEnvolope) :
                state;
        }

        public void StreamingStopped() {
            // Batch write or do something else
        }

        public void LiveStreamStarted() {
            // Batch write or do something else
        }
    }
}