$(document).ready(function () {
    $('#executeSqlForm').submit(function (event) {
        event.preventDefault();
        var sqlQuery = $('#sqlQuery').val().trim();
        if (sqlQuery === '') return;
        if (!isValidSelectQuery(sqlQuery)) {
            $('#validationMessage').text('Only SELECT queries are allowed.');
            return;
        }
        $.post('/OAI/ExecuteSqlQuery', { sqlQuery: sqlQuery }, function (data) {
            generateTable(data);
            $('.query-results-container').show();
        }).fail(function (xhr, status, error) {
            console.error('Error executing SQL query:', error);
            $('#validationMessage').text('Error executing SQL query. Please try again.');
        });
    });

    $('#toggleResultsBtn').click(function () {
        var container = $('.query-results-container');
        container.toggleClass('maximized');
        var isMaximized = container.hasClass('maximized');
        $('#toggleResultsBtn').text(isMaximized ? 'Minimize' : 'Maximize');

        if (isMaximized) {
            $('body').css('overflow', 'hidden'); // Prevent scrolling in background
            $('#queryResults').css('max-width', '100vw'); // Expand to full viewport width
            $('html, body').animate({ scrollTop: 0 }, 'fast'); // Scroll to top
            $(document).on('keydown', handleEscKey); // Listen for Esc key press
            alert('Press Esc key to minimize.');
            $('.maximized').resizable({
                handles: 's', // Only allow vertical resizing
                minHeight: 100 // Minimum height when resized
            });
        } else {
            $('body').css('overflow', 'auto'); // Restore scrolling in background
            $('#queryResults').css('max-width', '100%'); // Restore original width
            $(document).off('keydown', handleEscKey); // Remove Esc key listener
            $('.maximized').resizable('destroy'); // Destroy resizable when minimized
        }
    });

    function submitQuery() {
        var userQuery = $('#userQuery').val().trim();
        if (userQuery === '') return;
        addMessage(userQuery, true);
        $.post('/OAI/GetSqlQuery', { userQuery: userQuery }, function (data) {
            var botResponse = data.sqlQuery;
            addMessage(botResponse);
            $('#chatArea .bot-message code').each(function (i, block) {
                hljs.highlightBlock(block);
            });
        });
        $('#userQuery').val('');
    }

    function addMessage(message, isUser = false) {
        const chatBox = $('#chatArea');
        const messageDiv = $('<div class="chat-message">');
        if (isUser) {
            messageDiv.addClass('user-message').html('<strong>You:</strong> ' + escapeHtml(message));
        } else {
            messageDiv.addClass('bot-message').html('<strong>ChatGPT:</strong> ' + escapeHtml(message));
        }
        chatBox.append(messageDiv);
        scrollToBottom();
    }

    function generateTable(data) {
        var table = $('<table class="table table-bordered table-striped"></table>');
        var thead = $('<thead></thead>');
        var tbody = $('<tbody></tbody>');

        // Generate header row
        var headerRow = $('<tr></tr>');
        for (var columnName in data) {
            headerRow.append($('<th></th>').text(columnName));
        }
        thead.append(headerRow);

        // Generate body rows
        var numRows = data[Object.keys(data)[0]].length; // Assuming all columns have the same number of rows
        for (var i = 0; i < numRows; i++) {
            var bodyRow = $('<tr></tr>');
            for (var columnName in data) {
                bodyRow.append($('<td></td>').text(data[columnName][i]));
            }
            tbody.append(bodyRow);
        }

        table.append(thead).append(tbody);
        $('#queryResults').html(table);
    }

    function scrollToBottom() {
        $('#chatArea').scrollTop($('#chatArea')[0].scrollHeight);
    }

    function escapeHtml(text) {
        var map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, function (m) { return map[m]; });
    }

    function isValidSelectQuery(query) {
        var trimmedQuery = query.trim().toLowerCase();
        return trimmedQuery.startsWith('select');
    }

    function handleEscKey(event) {
        if (event.key === 'Escape') {
            $('#toggleResultsBtn').click(); // Toggle back to minimized view
        }
    }
});
