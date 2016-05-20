namespace Tables {
    public interface ISerializer {
        string Serialize(IEvent @event);
    }
}