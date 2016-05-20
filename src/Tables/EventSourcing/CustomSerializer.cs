using Newtonsoft.Json;

namespace Tables {
    public class CustomSerializer : ISerializer {
        public string Serialize(IEvent @event) {
            return JsonConvert.SerializeObject(@event);
        }
    }
}