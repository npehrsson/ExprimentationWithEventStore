using System;
using System.Collections.Generic;

namespace Tables {
    public class Table {
        public Table(Guid id) {
            Id = id;
            Rows = new List<Row>();
        }

        public string Name { get; set; }
        public Guid Id { get; private set; }
        public IList<Row> Rows { get; private set; }
        public int CurrentVersion { get; set; }
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
        public Cell(Guid rowId, Guid id) {
            Id = id;
            RowId = rowId;
        }

        public Guid RowId { get; private set; }
        public Guid Id { get; private set; }
        public string Value { get; set; }
    }
}
