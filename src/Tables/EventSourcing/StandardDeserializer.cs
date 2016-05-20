using System;
using Newtonsoft.Json;

namespace Tables {
    public class StandardDeserializer : IDeserializer {
        private readonly Type _clrType;

        public StandardDeserializer(string eventType, Type clrType) {
            EventType = eventType;
            _clrType = clrType;
        }

        public string EventType { get; }

        public object Deserialize(object data) {
            return JsonConvert.DeserializeObject(data.ToString(), _clrType);
        }
    }
}