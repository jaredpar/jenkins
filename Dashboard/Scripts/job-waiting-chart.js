$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawChart);

    function drawChart() {
        var elem = $('#jobs_waiting_chart');
        var data = [];
        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1])]);
        });

        var table = new google.visualization.DataTable();
        table.addColumn('string', 'Job Name');
        table.addColumn('number', 'Count');
        table.addRows(data);

        var options = {
            title: 'Builds in Queue',
            hAxis: {
                title: 'Job Name'
            },
            vAxis: {
                title: 'Count'
            },
            width: 600,
            height: 400,
        };

        var chart = new google.visualization.ColumnChart(elem.get(0));
        chart.draw(table, options);
    }
});
