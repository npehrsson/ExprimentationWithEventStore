using System;

namespace Tables.Controllers
{
    public class TableRuleEventApplier : IEventApplier<Table> {
        public Table Apply(Table state, EventEnvolope eventEnvolope) {
            switch (eventEnvolope.EventType) {
                case nameof(TableCreatedEvent):
                    var createdEvent = (TableCreatedEvent)eventEnvolope.EventData;
                    var rule = new Table(createdEvent.TableId) {
                        Name = createdEvent.Name,
                        CurrentVersion = eventEnvolope.EventNumber
                    };
                    return rule;
                case nameof(CreatedRowEvent):
                    var rowEvent = (CreatedRowEvent)eventEnvolope.EventData;
                    state.AddRow(rowEvent.RowId);
                    return state;
                case nameof(CreatedColumnEvent):
                    var column = (CreatedColumnEvent)eventEnvolope.EventData;
                    state.AddColumn(column.ColumnId, column.Name);
                    return state;
                case nameof(CellEditedEvent):
                    var cellEditEvent = (CellEditedEvent)eventEnvolope.EventData;
                    state.EditCell(cellEditEvent.RowId, cellEditEvent.ColumnId, cellEditEvent.Value);
                    return state;
                default:
                    return state;
            }
        }

        public void LiveStreamStarted() {
        }

        public void StreamingStopped() {
        }
    }
}