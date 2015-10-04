
$(document).ready(function () {
    google.setOnLoadCallback(drawDailyDurationChart);

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
