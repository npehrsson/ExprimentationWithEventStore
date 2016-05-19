namespace Tables
{
    public interface IEventApplier {
        void Apply(EventEnvolope eventEnvolope);
        void LiveStreamStarted();
        void StreamingStopped();
    }
}