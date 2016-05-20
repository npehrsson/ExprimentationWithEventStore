using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Tables {
    public class ConnectionFactory {
        private const string Uri = "tcp://localhost:1113";

        public IEventStoreConnection Create() {
            var connectionSettings = ConnectionSettings
                .Create()
                .UseConsoleLogger()
                .KeepReconnecting()
            //    .SetDefaultUserCredentials(new UserCredentials(UserName, Password))
                //.UseSslConnection("", true)
                .Build();

            return EventStoreConnection.Create(connectionSettings, new Uri(Uri));
        }
    }
}