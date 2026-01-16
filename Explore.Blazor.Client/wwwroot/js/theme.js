window.ExploreTheme = {
    setThemeCookie: function (value) {
        document.cookie = "theme=" + value + "; path=/; max-age=31536000";
    },
    getStoredTheme: function () {
        return localStorage.getItem("theme");
    },
    setStoredTheme: function (value) {
        localStorage.setItem("theme", value);
    }
};
