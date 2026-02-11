window.ExploreLayout = {
    setAnnouncementBarHeight: function (element) {
        var height = element && element.offsetHeight ? element.offsetHeight : 0;
        document.documentElement.style.setProperty("--announcement-bar-height", height + "px");
    },
    clearAnnouncementBarHeight: function () {
        document.documentElement.style.setProperty("--announcement-bar-height", "0px");
    }
};

window.ExploreTheme = {
    getStoredTheme: function () {
        try {
            return localStorage.getItem("explore-theme") || "";
        } catch (e) {
            return "";
        }
    },
    setStoredTheme: function (theme) {
        try {
            localStorage.setItem("explore-theme", theme);
        } catch (e) {
            // localStorage not available (e.g. private browsing)
        }
    },
    setThemeCookie: function (theme) {
        var maxAge = 365 * 24 * 60 * 60;
        document.cookie = "theme=" + theme + ";path=/;max-age=" + maxAge + ";SameSite=Lax";
    }
};

(function () {
    var path = (window.location.pathname || "").toLowerCase();

    // Normalize optional trailing slash while keeping root intact.
    if (path.length > 1 && path.endsWith("/")) {
        path = path.slice(0, -1);
    }

    if (path === "/login") {
        window.location.replace("/auth/challenge" + (window.location.search || ""));
        return;
    }

    if (path === "/logout") {
        window.location.replace("/auth/signout" + (window.location.search || ""));
    }
})();
