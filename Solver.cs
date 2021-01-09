using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SmartLink.Models;

namespace SmartLink
{
    public class Solver
    {
        public List<Sequence> Sequences { get; set; } = new List<Sequence>();
        public int BufferSize { get; set; } = 4;
        public List<List<Cell>> Matrix { get; set; } = new List<List<Cell>>();
        public List<Sequence> Solves { get; set; } = new List<Sequence>();

        public int Height { get { return Matrix.Count; } }
        public int Width { get { return Matrix[0].Count; } }

        public Solver(int height, int width)
        {
            for (int h = 0; h < height; h++)
            {
                Matrix.Add(new List<Cell>());
                for (int w = 0; w < width; w++)
                {
                    Matrix[h].Add(new Cell() { Row = h, Column = w });
                }
            }
        }

        public void SetCell(int x, int y, string cell)
        {
            Matrix[y][x] = new Cell() { Value = cell, Row = y, Column = x };
        }

        public void SetRow(int row, string values)
        {
            if (String.IsNullOrEmpty(values))
                return;

            if (values.Length % 2 != 0)
                throw new ArgumentException("Row has incorrect string length");

            if (values.Length / 2 > Width)
                throw new ArgumentException("Row length is too long");

            int pos = 0;
            for (int sPos = 0; sPos < values.Length; sPos += 2)
            {
                Matrix[row][pos] = new Cell() { Value = values.Substring(sPos, 2), Row = row, Column = pos };
                pos++;
            }
        }

        public void AddSequence(string sequence)
        {
            AddSequence(sequence, 0);
        }

        public void AddSequence(string sequence, int value)
        {
            if (String.IsNullOrEmpty(sequence))
                return;

            if (sequence.Length % 2 != 0)
                throw new ArgumentException("Sequence has incorrect string length");

            Sequence s = new Sequence() { Value = value };
            for (int sPos = 0; sPos < sequence.Length; sPos += 2)
            {
                string val = sequence.Substring(sPos, 2);
                Cell c = new Cell() { Value = val };
                s.Cells.Add(c);
            }
            Sequences.Add(s);
        }

        public Sequence Solve()
        {
            // It's already solved if there's nothing to solve for
            if (Sequences.Count == 0)
                return null;

            GenerateSolves();
            CompareSolves();
            return Solves
                .OrderByDescending(s => s.Value)
                .ThenByDescending(s => s.SolvedValues.Count)
                .ThenByDescending(s => s.Used)
                .First();
        }

        public void CompareSolves()
        {
            for (int s = 0; s < Solves.Count; s++)
            {
                var solve = Solves[s];

                // Reset the sequences so we can match fresh
                Sequences.ForEach(s => s.Reset());

                // Compare this solve to each sequence and consume an entry if matched
                // Reset the sequence if the cells don't match
                // Skip the sequence if it's already solved
                for (int sc = 0; sc < solve.Cells.Count; sc++)
                {
                    var solveCell = solve.Cells[sc];

                    foreach (Sequence seq in Sequences)
                    {
                        if (seq.Solved)
                            continue;

                        bool recheck = true;
                        while (recheck)
                        {
                            recheck = false;
                            foreach (Cell seqCel in seq.Cells)
                            {
                                if (seqCel.Visited)
                                    continue;

                                // First unmatched cell is now seqCel
                                if (solveCell.Value == seqCel.Value)
                                {
                                    seq.Used++;
                                    seqCel.Visited = true;

                                    // If we've visited all cells, mark it solved
                                    if (seqCel == seq.Cells.Last())
                                    {
                                        seq.Solved = true;
                                        solve.Value += seq.Value;
                                        solve.SolvedValues.Add(seq.Value);
                                        if (seq.Started)
                                            solve.Used += seq.Used;
                                    }
                                    break;
                                }
                                else
                                {
                                    // If the cell values don't match, reset the sequence
                                    seq.Reset();
                                    seq.Started = false;
                                    if (seqCel != seq.Cells.First())
                                    {
                                        recheck = true;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void GenerateSolves()
        {
            Solves.Clear();
            for (int col = 0; col < Width; col++)
            {
                Visit(true, 0, col, 0, new Sequence());
            }
        }

        public void Visit(bool visitRow, int row, int col, int depth, Sequence path)
        {
            var cell = Matrix[row][col];
            cell.Visited = true;
            path.Cells.Add(cell);

            // If we're visiting a cell at our buffer depth, stop processing and this 'solve'
            if (depth == BufferSize - 1)
            {
                Solves.Add(new Sequence(path));
                goto done;
            }

            // Recurse down into the solving sequence
            int limit = (visitRow ? Height : Width);
            for (int i = 0; i < limit; i++)
            {
                if (visitRow)
                {
                    // Don't visit previously visited cells
                    if (Matrix[i][col].Visited)
                        continue;

                    Visit(false, i, col, depth + 1, path);
                } 
                else
                {
                    // Don't visit previously visited cells
                    if (Matrix[row][i].Visited)
                        continue;

                    Visit(true, row, i, depth + 1, path);
                }
            }

            done:
                // Reset our visit after we're done for the next iteration
                cell.Visited = false;
                // Also remove us from the list (pass by reference)
                path.Cells.RemoveAt(path.Cells.Count - 1);
        }

        public Sequence FlattenSequences()
        {
            List<Sequence> Run = Sequences.ToList();
            Sequence best = new Sequence();

            Console.WriteLine("Sequences to solve for: ");
            Run.ForEach(row => Console.WriteLine(row));
            Console.WriteLine();

            // Check if a sequence is wholly contained in another
            for (int seqNumber = 0; seqNumber < Run.Count; seqNumber++)
            {
                Sequence s = Run[seqNumber];
                for (int checkNumber = seqNumber + 1; checkNumber < Run.Count; checkNumber++)
                {
                    Sequence c = Run[checkNumber];

                    // Check them to see if either is contained in either
                    if (s.ToString().Contains(c.ToString()) || c.ToString().Contains(s.ToString())) 
                    {
                        // One contains the other, elminate the shorter one (or first one)
                        if (s.Cells.Count > c.Cells.Count)
                            Run.Remove(c);
                        else
                            Run.Remove(s);
                    }
                }
            }

            Console.WriteLine("After removing whole matches:");
            Run.ForEach(row => Console.WriteLine(row));
            Console.WriteLine();

            // Check if this sequence starts/ends with another
            for (int seqNumber = 0; seqNumber < Run.Count; seqNumber++)
            {
                Sequence s = Run[seqNumber];
                for (int checkNumber = seqNumber + 1; checkNumber < Run.Count; checkNumber++)
                {
                    Sequence c = Run[checkNumber];

                    // Check them to see if either is contained in either
                    if (s.ToString().Contains(c.ToString()) || c.ToString().Contains(s.ToString()))
                    {
                        // One contains the other, elminate the shorter one (or first one)
                        if (s.Cells.Count > c.Cells.Count)
                            Run.Remove(c);
                        else
                            Run.Remove(s);
                    }
                }
            }



            return best;
        }

        public override string ToString()
        {
            string value = "";
            foreach (var row in Matrix)
            {
                value += String.Join(" ", row.Select(cell => cell.Value));
                value += "\n";
            }
            return value;
        }
    }
}
