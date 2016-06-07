$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawBuildSummary);

    function drawBuildSummary() {
        var elem = $('#build_summary_chart');
        var data = [['View Name', 'Count']];
        var categories = [];

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1])]);
            categories.push(all[0]);
        });

        var dataTable = google.visualization.arrayToDataTable(data);
        var options = {
            title: 'View Name (Project)',
            curveType: 'function',
            bar: { groupWidth: '75%' },
            isStacked: true
        };

        var chart = new google.visualization.BarChart(elem.get(0));
        chart.draw(dataTable, options);

        google.visualization.events.addListener(chart, 'select', function () {
            var selectedItem = chart.getSelection()[0];
            if (selectedItem) {
                var viewName = categories[selectedItem.row];
                $('#viewname_form_kind').attr('value', viewName);
                var form = $('#viewname_form').submit()
            }
        });
    }
});

$(document).ready(function () {
    var all_results_link = $('#submit_form_all');
    var all_viewname = all_results_link.attr('data-viewname');
    all_results_link.click(function (e) {
        e.preventDefault();
        $('#viewname_form_kind').attr('value', all_viewname);
        $('#viewname_form').submit();
    });
});
