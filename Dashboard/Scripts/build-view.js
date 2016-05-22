$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawBuildSummary);

    function drawBuildSummary() {
        var elem = $('#build_summary_chart');
        var data = [['Build Result', 'Count']]

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1])]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Build Result',
            curveType: 'function',
            bar: { groupWidth: '75%'},
            isStacked: true
        };

        var chart = new google.visualization.BarChart(elem.get(0));
        chart.draw(dataTable, options);
    }
});
