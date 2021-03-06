﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartLink.Models;
using Microsoft.Extensions.Logging;

namespace SmartLink.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolveController : ControllerBase
    {
        private readonly ILogger _logger;

        public SolveController(ILogger<SolveController> logger)
        {
            _logger = logger;
        }

        // POST api/<OcrController>
        [HttpPost]
        public ActionResult<List<string>> Post([FromForm] string matrixString, [FromForm] string sequenceString, [FromForm] int bufferSize)
        {
            _logger.LogInformation($"Performing solve over {matrixString} for {sequenceString} with length {bufferSize}");
            if (string.IsNullOrWhiteSpace(matrixString) || string.IsNullOrWhiteSpace(sequenceString) || bufferSize <= 0)
            {
                _logger.LogError("Matrix, sequence, or memory size missing or invalid");
                return BadRequest(new List<string>() { "Missing matrix, sequence, or memory size" });
            }

            List<string> matrix = matrixString.Split(",").ToList();
            List<string> sequences = sequenceString.Split(",").ToList();

            // Make sure that each row has the right number of cells and matches the count
            List<string> errors = new List<string>();
            matrix.ForEach(x =>
            {
                if (x.Length % 2 != 0)
                {
                    _logger.LogError($"Matrix row '{x}' is not a multiple of 2 characters");
                    errors.Add($"Matrix row '{x}' is not a multiple of 2 characters");
                    return;
                }

                if (Math.Floor((double)x.Length / 2) != matrix.Count)
                {
                    _logger.LogError($"Matrix row '{x}' does permit a square matrix");
                    errors.Add($"Matrix row '{x}' does permit a square matrix");
                    return;
                }
            });

            // Validate each of the sequences
            sequences.ForEach(x =>
            {
                var parts = x.Split("=").ToList();
                var seq = parts.First();
                var value = parts.ElementAtOrDefault(1);

                if (seq.Length % 2 != 0)
                {
                    _logger.LogError($"Sequence '{x}' is not a multiple of two characters");
                    errors.Add($"Sequence '{x}' is not a multiple of two characters");
                    return;
                }

                if (!String.IsNullOrWhiteSpace(value) && !int.TryParse(value, out int iValue))
                {
                    _logger.LogError($"Sequence value for '{x}' is not a valid integer");
                    errors.Add($"Sequence value for '{x}' is not a valid integer");
                    return;
                }
            });

            // Error out on validation if needed
            if (errors.Count > 0)
                return BadRequest(errors);

            // Since we got this far, we know we have a square matrix based on comma splices
            int height = matrix.Count;
            int width = height;

            _logger.LogInformation($"Creating a {width} wide, by {height} tall matrix with buffer size {bufferSize}");

            // Createa the solver and send in all the rows
            Solver s = new Solver(height, width) { BufferSize = bufferSize };
            for (int i = 0; i < matrix.Count; i++)
                s.SetRow(i, matrix[i]);

            // Figure out the sequences to solve and send those in
            for (int i = 0; i < sequences.Count; i++)
            {
                var parts = sequences[i].Split("=").ToList();
                var seq = parts.First();
                var value = parts.ElementAtOrDefault(1);
                int iValue = i + 1;
                if (!String.IsNullOrWhiteSpace(value))
                    iValue = int.Parse(value);

                s.AddSequence(seq, iValue);
            }

            Sequence result = null;
            try
            {
                _logger.LogInformation("Solving matrix");
                // Go ahead and solve it 
                result = s.Solve();
                _logger.LogInformation("Solution found");
            }
            catch (Exception e)
            {
                _logger.LogError("Exception occurred during the solve", e);
                return StatusCode(500, new { message = e.Message });
            }


            // No results means no solution
            if (result == null || result.SolvedValues.Count == 0)
            {
                _logger.LogWarning("No solution found");
                return NotFound(new List<string>() { "No solution found" });
            }

            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            List<string> resultStr = new List<string>
            {
                // Pretty print the solution
                $"Best solution:",
                $"  Solved: {String.Join(", ", result.SolvedValues)} for {result.Value} total",
                $"  Start Chain: {result.Used}",
                $"  Solution:"
            };

            Cell prev = null;
            for (int i = 0; i < result.Cells.Count; i++)
            {
                Cell c = result.Cells[i];
                if (i == 0)
                {
                    resultStr.Add($"    Start in column {letters[c.Column]} at {c.Value} ({c.Row + 1},{letters[c.Column]})");
                    prev = c;
                }
                else 
                {
                    string movement = "";
                    int horiz = c.Column - prev.Column;
                    int vert = c.Row - prev.Row;
                    if (horiz != 0)
                        movement = $"{Math.Abs(horiz)} {(horiz > 0 ? "right" : "left ")}";
                    else
                        movement = $"{Math.Abs(vert)} {(vert > 0 ? "down " : "up   ")}";

                    resultStr.Add($"    {movement} to {c.Value} ({c.Row + 1},{letters[c.Column]})");
                    prev = c;
                }
            }
            //resultStr.Add("  Old Solution:");
            //result.Cells.ForEach(c => resultStr.Add($"    {c.Value} ({c.Row + 1},{letters[c.Column]})"));

            return Ok(resultStr);
        }
    }
}
