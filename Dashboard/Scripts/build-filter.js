$(document).ready(function () {
    if ($('#view_name_div select').length > 0) {
        var startDate = $('input[name=startDate]').val();
        var selectedViewName = $('input[name=viewName]').val();
        $('#view_name_div select').change(function (e) {
            $('input[name=viewName]').val(e.target.value)
        });
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
    }
});
