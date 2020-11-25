function showLoading() {
    $('#loadingProcess').show();
}
function hideLoading() {
    $('#loadingProcess').hide();
}

/* نمایش نوتیفیکیشن */
function dispAlert(msg, color = 'red') {
    try {
        new jBox('Notice', {
            position: { x: 'left', y: 'center' },
            content: msg,
            color: color
        });
    } catch (e) { alert(msg); }
}