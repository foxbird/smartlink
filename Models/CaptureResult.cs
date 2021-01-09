using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartLink.Models
{
    public class CaptureResult
    {
        public List<List<Cell>> Matrix { get; set; } = new List<List<Cell>>();
        public List<Sequence> Sequences { get; set; } = new List<Sequence>();

        public CaptureResult()
        {

        }

        public void Initialize(int matrixSize = 0, int sequenceCount = 0)
        {
            if (matrixSize > 0)
            {
                Matrix = new List<List<Cell>>();
                for (int row = 0; row < matrixSize; row++)
                {
                    Matrix.Add(new List<Cell>());
                    for (int col = 0; col < matrixSize; col++)
                    {
                        Matrix[row].Add(new Cell() { Row = row, Column = col });
                    }
                }
            }

            if (sequenceCount > 0)
            {
                Sequences = new List<Sequence>();
                for (int count = 0; count < sequenceCount; count++)
                {
                    Sequences.Add(new Sequence());
                }
            }
        }

        public void SetMatrixCell(int row, int col, string value)
        {
            Matrix[row][col].Value = value;
        }

        public void SetSequenceCell(int sequence, int col, string value)
        {
            Sequences[sequence].Cells[col].Value = value;
        }

        public void SetSequenceValue(int sequence, int value)
        {
            Sequences[sequence].Value = value;
        }
    }
}
