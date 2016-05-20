namespace Tables {
    public interface IDeserializer {
        string EventType { get; }
        object Deserialize(object data);
    }
}