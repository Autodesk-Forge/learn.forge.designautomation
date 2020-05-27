$(document).ready(function () {
    prepareLists();

    $('#clearAccount').click(clearAccount);
    $('#defineActivityShow').click(defineActivityModal);
    $('#createAppBundleActivity').click(createAppBundleActivity);
    $('#startWorkitem').click(startWorkitem);

    startConnection();
});

function prepareLists() {
    list('activity', '/api/forge/designautomation/activities');
    list('engines', '/api/forge/designautomation/engines');
    list('localBundles', '/api/appbundles');
}

function list(control, endpoint) {
    $('#' + control).find('option').remove().end();
    jQuery.ajax({
        url: endpoint,
        success: function (list) {
            if (list.length === 0)
                $('#' + control).append($('<option>', {
                    disabled: true,
                    text: 'Nothing found'
                }));
            else
                list.forEach(function (item) {
                    $('#' + control).append($('<option>', {
                        value: item,
                        text: item
                    }));
                });
        }
    });
}

function clearAccount() {
    if (!confirm('Clear existing activities & appbundles before start. ' +
            'This is useful if you believe there are wrong settings on your account.' +
            '\n\nYou cannot undo this operation. Proceed?'))
        return;

    jQuery.ajax({
        url: 'api/forge/designautomation/account',
        method: 'DELETE',
        success: function () {
            prepareLists();
            writeLog('Account cleared, all appbundles & activities deleted');
        }
    });
}

function defineActivityModal() {
    $("#defineActivityModal").modal();
}

function createAppBundleActivity() {
    startConnection(function () {
        writeLog("Defining appbundle and activity for " + $('#engines').val());
        $("#defineActivityModal").modal('toggle');
        createAppBundle(function () {
            createActivity(function () {
                prepareLists();
            });
        });
    });
}

function createAppBundle(cb) {
    jQuery.ajax({
        url: 'api/forge/designautomation/appbundles',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            zipFileName: $('#localBundles').val(),
            engine: $('#engines').val()
        }),
        success: function (res) {
            writeLog('AppBundle: ' + res.appBundle + ', v' + res.version);
            if (cb)
                cb();
        },
        error: function (xhr, ajaxOptions, thrownError) {
            writeLog(' -> ' + (xhr.responseJSON && xhr.responseJSON.diagnostic ? xhr.responseJSON.diagnostic : thrownError));
        }
    });
}

function createActivity(cb) {
    jQuery.ajax({
        url: 'api/forge/designautomation/activities',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            zipFileName: $('#localBundles').val(),
            engine: $('#engines').val()
        }),
        success: function (res) {
            writeLog('Activity: ' + res.activity);
            if (cb)
                cb();
        },
        error: function (xhr, ajaxOptions, thrownError) {
            writeLog(' -> ' + (xhr.responseJSON && xhr.responseJSON.diagnostic ? xhr.responseJSON.diagnostic : thrownError));
        }
    });
}

function startWorkitem() {
    var inputFileField = document.getElementById('inputFile');
    if (inputFileField.files.length === 0) {
        alert('Please select an input file');
        return;
    }
    if ($('#activity').val() === null)
        return (alert('Please select an activity'));
    var file = inputFileField.files[0];
    startConnection(function () {
        var formData = new FormData();
        formData.append('inputFile', file);
        formData.append('data', JSON.stringify({
            width: $('#width').val(),
            height: $('#height').val(),
            activityName: $('#activity').val(),
            browerConnectionId: connectionId
        }));
        writeLog('Uploading input file...');
        $.ajax({
            url: 'api/forge/designautomation/workitems',
            data: formData,
            processData: false,
            contentType: false,
            //contentType: 'multipart/form-data',
            //dataType: 'json',
            type: 'POST',
            success: function (res) {
                writeLog('Workitem started: ' + res.workItemId);
            },
            error: function (xhr, ajaxOptions, thrownError) {
                writeLog(' -> ' + (xhr.responseJSON && xhr.responseJSON.diagnostic ? xhr.responseJSON.diagnostic : thrownError));
            }
        });
    });
}

function writeLog(text) {
    $('#outputlog').append('<div style="border-top: 1px dashed #C0C0C0">' + text + '</div>');
    var elem = document.getElementById('outputlog');
    elem.scrollTop = elem.scrollHeight;
}

var connection;
var connectionId;

function startConnection(onReady) {
    if (connection && connection.connected) {
        if (onReady)
            onReady();
        return;
    }
    connection = io();
    connection.on('connect', function() {
        connectionId = connection.id;
        if (onReady)
            onReady();
    });

    connection.on('downloadResult', function (url) {
        writeLog('<a href="' + url + '">Download result file here</a>');
    });

    connection.on('downloadReport', function (url) {
        writeLog('<a href="' + url + '">Download report file here</a>');
    });

    connection.on('onComplete', function (message) {
        if (typeof message === 'object')
            message =JSON.stringify(message, null, 2);
        writeLog(message);
    });
}
