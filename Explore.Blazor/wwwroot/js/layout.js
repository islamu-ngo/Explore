window.ExploreLayout = {
    setAnnouncementBarHeight: function (element) {
        var height = element && element.offsetHeight ? element.offsetHeight : 0;
        document.documentElement.style.setProperty("--announcement-bar-height", height + "px");
    },
    clearAnnouncementBarHeight: function () {
        document.documentElement.style.setProperty("--announcement-bar-height", "0px");
    }
};
