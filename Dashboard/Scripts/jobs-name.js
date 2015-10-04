
$(document).ready(function () {
    google.load("visualization", "1", { packages: ["corechart"] });
    google.setOnLoadCallback(drawCharts);

    function drawCharts() {
        drawDailySummaryChart();
        drawDailyDurationChart();
    }

    function drawDailySummaryChart() {
        var elem = $('#daily_summary_chart');
        var data = [['Date', 'Passed', 'Failed']];

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1]), parseInt(all[2])]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Daily Job Summary',
            curveType: 'function',
            bar: { groupWidth: '75%'},
            isStacked: true
        };

        var chart = new google.visualization.BarChart(elem.get(0));
        chart.draw(dataTable, options);
    }

    function drawDailyDurationChart() {
        var elem = $('#daily_duration_chart');
        var dates = elem.attr('data-dates').split(';');
        var times = elem.attr('data-times').split(';');

        var data = [['Date', 'Duration']];
        dates.forEach(function (date, index, _) {
            var time = parseInt(times[index]);
            data.push([date, time]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Average Daily Duration',
            curveType: 'function',
            legend: { position: 'bottom' }
        };

        var chart = new google.visualization.LineChart(elem.get(0));
        chart.draw(dataTable, options);
    }
});
