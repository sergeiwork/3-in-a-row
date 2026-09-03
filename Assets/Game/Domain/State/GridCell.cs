using System;

namespace ThreeInARow.Domain.State
{
    [Serializable]
    public struct GridCell : IEquatable<GridCell>, IComparable<GridCell>
    {
        public readonly int Column;
        public readonly int Row;

        public GridCell(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(GridCell other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => (Column * 397) ^ Row;
        public int CompareTo(GridCell other)
        {
            var rowComparison = Row.CompareTo(other.Row);
            return rowComparison != 0 ? rowComparison : Column.CompareTo(other.Column);
        }

        public override string ToString() => string.Format("{0},{1}", Column, Row);
    }
}
