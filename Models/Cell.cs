using System;
using System.Collections.Generic;
using System.Text;

namespace SmartLink.Models
{
    public class Cell
    {
        public string Value { get; set; } = String.Empty;
        public bool Visited { get; set; } = false;

        public int Row { get; set; } = 0;
        public int Column { get; set; } = 0;

        public Cell()
        {

        }
        
        public Cell(Cell other)
        {
            Value = other.Value;
            Visited = other.Visited;
            Row = other.Row;
            Column = other.Column;
        }
        
        public void Reset()
        {
            Visited = false;
        }

        public override string ToString()
        {
            return $"{Value}({Row + 1},{Column + 1})";
        }

    }
}
