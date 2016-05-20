namespace Tables
{
    public interface IEventHandler<TState> : IEventHandler {
        TState Handle(TState stateContext, EventEnvolope @event);
    }

    public interface IEventHandler {
        string EventType { get; }
        object Handle(object stateContext, EventEnvolope @event);
    }
}