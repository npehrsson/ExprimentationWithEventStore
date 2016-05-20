using System;

namespace Tables {
    public class TableCreatedEvent : IEvent {
        public TableCreatedEvent(Guid tableId, string name) {
            TableId = tableId;
            Name = name;
            EventId = Guid.NewGuid();
        }

        public Guid TableId { get; private set; }
        public Guid EventId { get; private set; }
        public string Name { get; private set; }
        public string EventType => nameof(TableCreatedEvent);
    }

    public class CellEditedEvent : IEvent {
        public CellEditedEvent(Guid rowId, Guid columnId, object value) {
            RowId = rowId;
            ColumnId = columnId;
            EventId = Guid.NewGuid();
            Value = value;
        }

        public Guid ColumnId { get; private set; }

        public Guid RowId { get; private set; }
        public Guid EventId { get; }
        public string EventType => nameof(CellEditedEvent);
        public object Value { get; private set; }
    }

    public class CreatedRowEvent : IEvent {
        public CreatedRowEvent(Guid rowId) {
            EventId = Guid.NewGuid();
            RowId = rowId;
        }

        public Guid RowId { get; private set; }
        public Guid EventId { get; }
        public string EventType => nameof(CreatedRowEvent);
    }

    public class CreatedColumnEvent : IEvent {
        public CreatedColumnEvent(Guid columnId, string name) {
            ColumnId = columnId;
            EventId = Guid.NewGuid();
            Name = name;
        }

        public Guid ColumnId { get; private set; }
        public string Name { get; private set; }
        public Guid EventId { get; }
        public string EventType => nameof(CreatedColumnEvent);
    }
}