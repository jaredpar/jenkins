$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawBuildSummary);

    function drawBuildSummary() {
        var elem = $('#repo_ET_chart');
        var data = [['Repo Names', 'Total Elapsed Time (in minutes)']];
        var categories = [];

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1])]);
            categories.push(all[0]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'Repo ET List',
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
