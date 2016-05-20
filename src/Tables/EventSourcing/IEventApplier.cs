namespace Tables
{
    public interface IEventApplier<TState> {
        TState Apply(TState state, EventEnvolope eventEnvolope);
        void StreamingStopped();
        void LiveStreamStarted();
    }
}