using System;
using System.Collections.Generic;
using System.Linq;

namespace Tables {
    public class Table {
        public Table(Guid id) {
            Id = id;
            Rows = new Dictionary<Guid, Row>();
            Columns = new List<Column>();
        }

        public string Name { get; set; }
        public Guid Id { get; private set; }
        public IDictionary<Guid, Row> Rows { get; private set; }
        public IList<Column> Columns { get; private set; }
        public int CurrentVersion { get; set; }

        public void AddRow(Guid id) {
            var row = new Row(id);
            foreach (var column in Columns) {
                row.Cells.Add(new Cell(id, column.Id));
            }

            Rows.Add(id, row);
        }

        public void AddColumn(Guid columnId, string name) {
            Columns.Add(new Column(columnId) { Name = name });

            foreach (var row in Rows.Values) {
                row.Cells.Add(new Cell(row.Id, columnId));
            }
        }

        public void EditCell(Guid rowId, Guid columnId, object value) {

            var row = Rows[rowId];
            var cell = row.Cells.Single(x => x.ColumnId == columnId);

            cell.Value = value;
        }
    }

    public class Column {
        public Column(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
        public string Name { get; set; }

    }

    public class Row {
        public Row(Guid id) {
            Id = id;
            Cells = new List<Cell>();
        }

        public Guid Id { get; private set; }
        public IList<Cell> Cells { get; private set; }
    }

    public class Cell {
        public Cell(Guid rowId, Guid columnId) {
            ColumnId = columnId;
            RowId = rowId;
        }

        public Guid RowId { get; private set; }
        public Guid ColumnId { get; private set; }
        public object Value { get; set; }
    }
}
