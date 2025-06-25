// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/eventshub")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveEvent", function (eventType, timestamp, payload) {
        const tableBody = document.querySelector('#eventsTable tbody');
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${eventType}</td>
            <td>${new Date(timestamp).toLocaleString()}</td>
            <td>${payload}</td>
        `;
        tableBody.prepend(row);
    });

    connection.start()
        .catch(function (err) {
            return console.error(err.toString());
        });
});
