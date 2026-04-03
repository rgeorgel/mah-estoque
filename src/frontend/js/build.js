const BUILD_VERSION = '202604031200';

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('link[rel="stylesheet"]').forEach(el => {
        el.href = el.href.split('?v=')[0] + '?v=' + BUILD_VERSION;
    });
    document.querySelectorAll('script[src]').forEach(el => {
        el.src = el.src.split('?v=')[0] + '?v=' + BUILD_VERSION;
    });
});