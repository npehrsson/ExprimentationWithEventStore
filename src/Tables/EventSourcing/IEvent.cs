using System;

namespace Tables {
    public interface IEvent {
        Guid EventId { get; }
        string EventType { get; }
    }
}