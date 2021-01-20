
// Configuration Variables
var MATRIX_MAX_COLS = 7;
var MATRIX_MAX_ROWS = 7;
var SEQUENCE_MAX_COLS = 4;
var SEQUENCE_MAX_ROWS = 4;

// Maximum number of characters in a cell
var MAX_CELL_VALUE = 2;

// Valid values for the matrix (and sequence)
var VALID_CELLS = ["BD", "1C", "E9", "55", "7A", "FF"];

// Useful defines
var LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
var ICON_CAPTURE = "fa-camera";
var ICON_CLEAR = "fa-trash";
var ICON_SOLVE = "fa-calculator";

// Local storage
var defaultBufferSize = localStorage.getItem("defaultBufferSize");

if (defaultBufferSize === null || defaultBufferSize === "")
    defaultBufferSize = "4";

// Theme references
// #f3e600 - Bright yellow
// #c9be00 - Darker yellow
// #9e9700 - Darker yet yellow
// #0a0a00 - Black

function hideLoader() {
    $("#loader").css("display", "none");
}

function showLoader() {
    $("#loader").css("display", "flex");
}

function setupCodeMatrix() {
    // Header Row
    for (var col = 0; col < MATRIX_MAX_COLS; col++) {
        var letter = LETTERS.charAt(col);
        //var input = '<input type="text" class="cell" id="code' + + '/>';
        $(".code table thead tr").append("<th>" + letter + "</th>");
    }

    // Data rows
    for (var row = 0; row < MATRIX_MAX_ROWS; row++) {
        var tbody = $(".code table tbody");

        // Row start
        var tr = "<tr>";

        // Row header
        tr += "<th>" + (row + 1) + "</th>";

        // Data cells
        for (col = 0; col < MATRIX_MAX_COLS; col++) {
            letter = LETTERS.charAt(col);
            var td = '<td><input type="text" class="cell" id="code' + (row + 1) + letter + '" /></td>';
            tr += td;
        }

        // End row
        tr += "</tr>";
        tbody.append(tr);
    }
}

function setupSequenceMatrix() {
    // Header Row
    for (var col = 0; col < SEQUENCE_MAX_COLS; col++) {
        var letter = LETTERS.charAt(col);
        $(".sequence table thead tr").append("<th>" + letter + "</th>");
    }
    $(".sequence table thead tr").append("<th>$</th>");

    // Data rows
    var tbody = $(".sequence table tbody");
    for (var row = 0; row < SEQUENCE_MAX_ROWS; row++) {
        // Row start
        var tr = "<tr>";

        // Row header
        tr += "<th>" + (row + 1) + "</th>";
        for (col = 0; col < SEQUENCE_MAX_COLS; col++) {
            letter = LETTERS.charAt(col);
            var td = '<td><input type="text" class="cell" id="sequence' + (row + 1) + letter + '" /></td>';
            tr += td;
        }

        // Value column
        tr += '<td><input type="text" class="cell" id="sequence' + (row + 1) + 'v" /></td>';

        // Close row
        tr += "</tr>";
        tbody.append(tr);
    }

    // Now add the memory count box
    tbody.append('<tr><th>M</th><td colspan="5" class="memory-row"><input type="text" class="memory" id="memory" /></td></tr>');

    // Restore the sequence count value
    // Restore the default buffer size
    $(document.getElementById("memory")).val(defaultBufferSize);
}

function setupClickHandlers() {
    $("#capture").click(doCapture);
    $("#clear").click(doClear);
    $("#solve").click(doSolve);
}

function setupInputHandlers() {
    // Cell input and focus changes
    for (var row = 0; row < MATRIX_MAX_ROWS; row++) {
        for (var col = 0; col < MATRIX_MAX_COLS; col++) {
            var id = "code" + (row + 1) + LETTERS[col];
            var cell = document.getElementById(id);

            $(cell).on('input', cellUpdate);
            $(cell).on('focus', cellFocus);
        }
    }

    // Sequence input and focus handlers
    for (row = 0; row < SEQUENCE_MAX_ROWS; row++) {
        for (col = 0; col < SEQUENCE_MAX_COLS; col++) {
            id = "sequence" + (row + 1) + LETTERS[col];
            cell = document.getElementById(id);

            $(cell).on('input', sequenceUpdate);
            $(cell).on('focus', cellFocus);

        }
        id = "sequence" + (row + 1) + "v";
        cell = document.getElementById(id);
        $(cell).on('focus', cellFocus);
    }
}

function cellFocus(event) {
    $(event.target).select();
}

function cellUpdate(event) {
    var cell = $(event.target);

    // Uppercase the cell
    var val = cell.val();
    val = val.toUpperCase();
    cell.val(val);

    // Apply color based on the value
    if (VALID_CELLS.indexOf(val) === -1 && val.length !== 0) {
        cell.addClass("cell-bad");
    } else {
        cell.removeClass("cell-bad");
    }

    // Move to the next cell
    if (val.length === MAX_CELL_VALUE) {
        // Figure out which cell we're on
        var id = cell.attr('id').substring("code".length);
        var row = id.substring(0, 1);
        var colStr = id.substring(1, 2);
        var col = LETTERS.indexOf(colStr);

        // Go to the next column
        col++;

        // Check for increement and wrap
        if (col === MATRIX_MAX_COLS) {
            row++;
            col = 0;
        }

        // If we're out of bounds, do nothing
        if (row === MATRIX_MAX_ROWS + 1) {
            return;
        }

        // Otherwise, set focus
        id = "code" + row + LETTERS[col];
        $("#" + id).focus();
    }
}


function sequenceUpdate(event) {
    var cell = $(event.target);

    // Uppercase it
    var val = cell.val();
    val = val.toUpperCase();
    cell.val(val);

    // Apply color based on the value
    if (VALID_CELLS.indexOf(val) === -1 && val.length !== 0) {
        cell.addClass("cell-bad");
    } else {
        cell.removeClass("cell-bad");
    }

    // Move on to the next cell if we're done with this one
    if (val.length === MAX_CELL_VALUE) {
        // Figure out which cell we're on
        var id = cell.attr('id').substring("sequence".length);
        var row = id.substring(0, 1);
        var colStr = id.substring(1, 2);
        var col = LETTERS.indexOf(colStr);

        // Go to the next column
        col++;

        // Check for increement and wrap
        if (col === SEQUENCE_MAX_COLS) {
            row++;
            col = 0;
        }

        // If we're out of bounds, do nothing
        if (row === SEQUENCE_MAX_ROWS + 1) {
            return;
        }

        // Otherwise, set focus
        id = "sequence" + row + LETTERS[col];
        $("#" + id).focus();
    }
}

function doCapture() {
    // Turn capture button into processing
    cogifyButton("capture", ICON_CAPTURE, true);

    // Disable all buttons
    disableButtons(true);
    doClear();

    // Submit AJAX request
    $.ajax({
        url: 'api/Capture',
        type: 'GET',
        cache: false,
        async: true,
        contentType: false,
        processData: false,
        dataType: 'json'
    }).done(function (data) {
        fillMatrix(data);
    }).fail(function (xhr, status, err) {
        console.log(err, xhr.responseJSON, xhr.responseText);
        showError(err, xhr.responseJSON, xhr.responseText);
    }).always(function () {
        disableButtons(false);
        cogifyButton("capture", ICON_CAPTURE, false);
    });

}

function fillMatrix(data) {
    var cells = data.matrix;
    var sequences = data.sequences;

    doClear();
    // Fill in the matrix
    for (var row = 0; row < MATRIX_MAX_ROWS; row++) {
        for (var col = 0; col < MATRIX_MAX_COLS; col++) {
            var id = "code" + (row + 1) + LETTERS[col];
            var cell = document.getElementById(id);

            if (row >= cells.length || col >= cells[row].length) {
                $(cell).val("");
                $(cell).removeClass("cell-bad");
                continue;
            }

            var val = cells[row][col].value;
            $(cell).val(val);

            if (VALID_CELLS.indexOf(val) === -1) {
                $(cell).addClass("cell-bad");
            } else {
                $(cell).removeClass("cell-bad");
            }
        }
    }

    for (row = 0; row < SEQUENCE_MAX_ROWS; row++) {
        for (col = 0; col < SEQUENCE_MAX_COLS; col++) {
            id = "sequence" + (row + 1) + LETTERS[col];
            cell = document.getElementById(id);

            if (row >= sequences.length || col >= sequences[row].cells.length) {
                $(cell).val("");
                $(cell).removeClass("cell-bad");
                continue;
            }

            val = sequences[row].cells[col].value;
            $(cell).val(val);

            if (VALID_CELLS.indexOf(val) === -1) {
                $(cell).addClass("cell-bad");
            } else {
                $(cell).removeClass("cell-bad");
            }
        }
    }
}

function doClear() {
    for (var row = 0; row < SEQUENCE_MAX_ROWS; row++) {
        for (var col = 0; col < SEQUENCE_MAX_COLS; col++) {
            var id = "sequence" + (row + 1) + LETTERS[col];
            var cell = document.getElementById(id);
            $(cell).val("");
            $(cell).removeClass("cell-bad");
        }
        id = "sequence" + (row + 1) + "v";
        cell = document.getElementById(id);
        $(cell).val("");
    }

    for (row = 0; row < MATRIX_MAX_ROWS; row++) {
        for (col = 0; col < MATRIX_MAX_COLS; col++) {
            id = "code" + (row + 1) + LETTERS[col];
            cell = document.getElementById(id);
            $(cell).val("");
            $(cell).removeClass("cell-bad");
        }
    }

    $("#solution").text("");
}

function doSolve() {
    // Turn solve button into processing (fa-cog fa-spin)
    cogifyButton("solve", ICON_SOLVE, true);

    // Disable all buttons
    disableButtons(true);

    var matrix = "";

    for (var row = 0; row < MATRIX_MAX_ROWS; row++) {
        var matrixRow = "";
        for (var col = 0; col < MATRIX_MAX_COLS; col++) {
            var id = "code" + (row + 1) + LETTERS[col];
            var cell = document.getElementById(id);
            var val = $(cell).val();
            matrixRow += val;
        }

        // Stop on the first empty row
        if (matrixRow.length === 0)
            break;

        matrix += matrixRow + ",";
    }

    matrix = matrix.slice(0, -1);

    var sequences = "";

    for (row = 0; row < SEQUENCE_MAX_ROWS; row++) {
        var sequence = "";
        for (col = 0; col < SEQUENCE_MAX_COLS; col++) {
            id = "sequence" + (row + 1) + LETTERS[col];
            cell = document.getElementById(id);
            val = $(cell).val();
            sequence += val;
        }

        // Stop on the first empty sequence
        if (sequence.length === 0)
            break;

        var value = $(document.getElementById("sequence" + (row + 1) + "v")).val();
        if (value !== null && value.length > 0)
            sequence += "=" + value;

        sequences += sequence + ",";
    }

    sequences = sequences.slice(0, -1);

    var memory = $("#memory").val();

    localStorage.setItem("defaultBufferSize", memory);

    var formData = new FormData();
    formData.append("matrixString", matrix);
    formData.append("sequenceString", sequences);
    formData.append("bufferSize", memory);

    // Post it to the backend
    $.ajax({
        url: 'api/Solve',
        type: 'POST',
        data: formData,
        cache: false,
        async: true,
        contentType: false,
        processData: false,
        dataType: 'json'
    }).done(function (data) {
        handleSolution(data);
    }).fail(function (xhr, status, err) {
        console.log(err, xhr.responseJSON, xhr.responseText);
        showError(err, xhr.responseJSON, xhr.responseText);
    }).always(function () {
        disableButtons(false);
        cogifyButton("solve", ICON_SOLVE, false);
    });
}

function setDisabled(button, disabled) {
    $("#" + button).prop("disabled", disabled);
}

function disableButtons(disabled) {
    var buttons = ["capture", "clear", "solve"];
    buttons.forEach(button => setDisabled(button, disabled));
}

function cogifyButton(button, btnClass, cogged) {
    if (cogged) {
        $("#" + button + " i").removeClass(btnClass);
        $("#" + button + " i").addClass("fa-cog fa-spin");
    } else {
        $("#" + button + " i").removeClass("fa-cog");
        $("#" + button + " i").removeClass("fa-spin");
        $("#" + button + " i").addClass(btnClass);
    }
}

function showError(status, err, errStr) {
    var message = status + "\n";

    // Try to find out what type the error is, default to errStr if not a json format
    if (typeof(err) === "string")
        message += err + "\n";
    else if (Array.isArray(err))
        message += err.join("\n");
    else if (typeof (err) === "object" && 'error' in err && typeof (err.error) === "string")
        message += err.error + "\n";
    else if (typeof (err) === "object" && 'message' in err && typeof (err.message) === "string")
        message += err.message + "\n";
    else
        message += errStr + "\n";

    $("#solution").addClass("bad-cell");
    $("#solution").text(message);
}

function handleSolution(sol) {
    var solution = "";
    for (var i = 0; i < sol.length; i++) {
        solution += sol[i] + "\n";
    }

    $("#solution").removeClass("bad-cell");
    $("#solution").text(solution);
}



setupCodeMatrix();
setupSequenceMatrix();
setupClickHandlers();
setupInputHandlers();