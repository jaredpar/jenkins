$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawBuildSummary);

    function drawBuildSummary() {
        var elem = $('#elapsed_time_chart');
        var data = [['Elapsed Time', 'Count']];
        var categories = [];

        //Categorize elapsed time based on their range
        var values = elem.attr('data-values').split(';');
        //Array elements are counts of jobs whose ETs fall into categories of 0~10ms, 10~100ms, 100~1000ms, 1000~10000ms, 10000~100000ms, 100000~1000000ms.
        var eTime = ["0", "0", "0", "0", "0", "0"];
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            var ETIndex = all[1].length - 1;
            eTime[ETIndex] = eTime[ETIndex] + 1;
        });

        var digits = 1;
        var lowerRange = '0 ~ '
        for (var i in eTime) {
            var upperRange = Math.pow(10, digits);
            var strRange = lowerRange + upperRange + 'ms'
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
