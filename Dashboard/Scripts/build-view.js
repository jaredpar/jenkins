$(document).ready(function () {
    google.charts.load('current', { packages: ['corechart', 'bar'] });
    google.charts.setOnLoadCallback(drawBuildSummary);

    function drawBuildSummary() {
        var elem = $('#build_summary_chart');
        var data = [['Build Result', 'Count']];
        var categories = [];

        var values = elem.attr('data-values').split(';');
        values.forEach(function (str, _, _) {
            var all = str.split(',');
            data.push([all[0], parseInt(all[1])]);
            categories.push(all[0]);
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

        google.visualization.events.addListener(chart, 'select', function() {
            var selectedItem = chart.getSelection()[0];
            if (selectedItem) {
                var category = categories[selectedItem.row];
                $('#category_form_kind').attr('value', category);
                var form = $('#category_form').submit()
            }
        });
    }
});

$(document).ready(function () {
    var startDate = $('#start_date_field')[0].value;
    var selectedViewName = $('#view_name_div').attr('data-selected-viewname');
    $.ajax({
        dataType: 'json',
        url: '/api/builds/viewNames',
        data: {
            startDate: startDate
        },
        success: function (list) {
            for (var i = 0; i < list.length; i++) {
                $('#view_name_div select').append('<option>' + list[i] + '</option>');
            }
            $('#view_name_div img').css('display', 'none');
            $('#view_name_div select').val(selectedViewName).css('display', '');
        },
        error: function () {
            $('#view_name_div img').css('display', 'none');
            $('#view_name_div').append('<span class="error_message">[[ Failed to fetch data ]]</span>');
        }
    });
});
