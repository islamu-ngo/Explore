// ABOUTME: JS interop functions for localization: document direction (RTL/LTR) and language attribute.
// ABOUTME: Called from LanguageProvider.razor when the active language changes.

window.localization = {
    setDirection: function (dir, lang) {
        document.documentElement.setAttribute('dir', dir);
        document.documentElement.setAttribute('lang', lang);
    },
    getDirection: function () {
        return document.documentElement.getAttribute('dir') || 'ltr';
    },
    getLanguage: function () {
        return document.documentElement.getAttribute('lang') || 'en';
    },
    getLanguageCookie: function () {
        var match = document.cookie.match(new RegExp('(^| )lang=([^;]+)'));
        return match ? match[2] : null;
    },
    getDirectionCookie: function () {
        var match = document.cookie.match(new RegExp('(^| )direction=([^;]+)'));
        return match ? match[2] : null;
    }
};
