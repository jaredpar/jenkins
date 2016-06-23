$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawBuildSummary);

    function drawBuildSummary() {
        var elem = $('#elapsed_time_chart');
        var data = [['Elapsed Time', 'Count']];
        var categories = [];

        //Categorize elapsed time based on their range
        var values = elem.attr('data-values').split(';');
        var eTime = ["0", "0", "0", "0", "0", "0"];
        var i = 0;
        values.forEach(function (str, _, _) {
            eTime[i] = str;
            i = i + 1;
        });

        var digits = 1;
        var lowerRange = '0 ~ '
        for (var i in eTime) {
            var upperRange = Math.pow(10, digits);
            var strRange = lowerRange + upperRange + 's'
            data.push([strRange, parseInt(eTime[i])]);
            categories.push(strRange);
            lowerRange = upperRange + ' ~ ';
            digits = digits + 1;
        }

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Elapsed Time',
            curveType: 'function',
            bar: { groupWidth: '75%' },
            isStacked: true
        };

        var chart = new google.visualization.BarChart(elem.get(0));
        chart.draw(dataTable, options);

        google.visualization.events.addListener(chart, 'select', function () {
            var selectedItem = chart.getSelection()[0];
            if (selectedItem) {
                var category = categories[selectedItem.row];
                $('#category_form_kind').attr('value', category);
                var form = $('#category_form').submit()
            }
        });
    }
});
