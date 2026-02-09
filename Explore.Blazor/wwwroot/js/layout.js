window.ExploreLayout = {
    setAnnouncementBarHeight: function (element) {
        var height = element && element.offsetHeight ? element.offsetHeight : 0;
        document.documentElement.style.setProperty("--announcement-bar-height", height + "px");
    },
    clearAnnouncementBarHeight: function () {
        document.documentElement.style.setProperty("--announcement-bar-height", "0px");
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
