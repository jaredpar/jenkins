$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawSummary);

    function drawSummary() {
        var elem = $('#testrun_comparison_chart');
        var data = [['Date', 'Legacy', 'Chunk', 'Full']];

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1]), parseInt(all[2]), parseInt(all[3])]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Daily Job Summary',
            format: 'MM/DD'
        };

        var chart = new google.visualization.ColumnChart(elem.get(0));
        chart.draw(dataTable, options);
    }
});
