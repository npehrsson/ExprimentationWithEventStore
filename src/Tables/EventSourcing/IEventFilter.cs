namespace Tables
{
    public interface IEventFilter {
        bool Filter(EventEnvolope @event);
    }
}