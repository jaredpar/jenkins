$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawCharts);

    function drawCharts() {
        drawTime();
        drawCount();
    }

    function drawTime() {
        var elem = $('#testrun_comparison_time_chart');
        var data = [['Date', 'Legacy', 'Chunk', 'Full']];

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1]), parseInt(all[2]), parseInt(all[3])]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Test Run Time Comparison',
            format: 'MM/DD'
        };

        var chart = new google.visualization.ColumnChart(elem.get(0));
        chart.draw(dataTable, options);
    }

    function drawCount() {
        var elem = $('#testrun_comparison_count_chart');
        var data = [['Date', 'None', 'Cache Hit', 'High Cache Hit']];

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1]), parseInt(all[2]), parseInt(all[3])]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Test Run Cache Comparison',
            format: 'MM/DD'
        };

        var chart = new google.visualization.ColumnChart(elem.get(0));
        chart.draw(dataTable, options);
    }
});
