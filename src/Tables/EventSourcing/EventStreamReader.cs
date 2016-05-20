using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace Tables {
    public class EventStreamReader {
        private readonly IEventStoreConnection _connection;
        private const int BatchSize = 4096;

        public EventStreamReader(IEventStoreConnection connection) {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            _connection = connection;
        }

        public async Task ReadAsync(string stream, int fromPosition,
            Action<IEnumerable<ResolvedEvent>> resolvedEventAction) {
            StreamEventsSlice slice;
            do {
                var stopWatch = Stopwatch.StartNew();

                slice = await _connection.ReadStreamEventsForwardAsync(stream, fromPosition, BatchSize, false);

                EventSourcingDiagnostics.ReadFromStreamDiagnostics(stopWatch.Elapsed, slice.Events.Length);
                resolvedEventAction(slice.Events);
                fromPosition = slice.NextEventNumber;

            } while (!slice.IsEndOfStream);
        }

        public async Task<IList<ResolvedEvent>> ToList(string stream, int fromPosition) {
            var list = new List<ResolvedEvent>();
            await ReadAsync(stream, fromPosition, x => list.AddRange(x));
            return list;
        }

        public async Task<IList<ResolvedEvent>> ToList(string stream) {
            return await ToList(stream, 0);
        }
    }
}