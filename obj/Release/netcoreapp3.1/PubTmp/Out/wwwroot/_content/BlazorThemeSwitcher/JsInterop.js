window.darkThemeSwitcherFunctions = {
    changeVariable: function (variable, value) {
        let root = document.documentElement;
        root.style.setProperty(variable, value);
    }
}