
$(document).ready(function () {
    google.setOnLoadCallback(drawCharts);

    function drawCharts() {
        drawChart();
        drawDailyDurationChart();
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

    function drawChart() {
        var data = google.visualization.arrayToDataTable([
            ['Date', 'Duration'],
            ['2010-10-1', 1000],
            ['2010-10-2', 2000],
            ['2010-10-3', 1500],
        ]);

        var options = {
            title: 'Company Performance',
            curveType: 'function',
            legend: { position: 'bottom' }
        };

        var elem = $('#curve_chart').get(0);
        var chart = new google.visualization.LineChart(elem);

        chart.draw(data, options);
    }
});
