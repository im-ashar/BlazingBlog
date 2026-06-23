(function () {
    function writeCookie(theme) {
        try {
            document.cookie = 'theme=' + theme + ';path=/;max-age=31536000;samesite=lax';
        } catch (_) { }
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        try { localStorage.setItem('theme', theme); } catch (_) { }
        writeCookie(theme);
        var checks = document.querySelectorAll('input.theme-controller');
        checks.forEach(function (el) { el.checked = theme === 'blazing-dark'; });
    }

    window.BlazingBlogTheme = {
        get: function () {
            return document.documentElement.getAttribute('data-theme') || 'blazing-light';
        },
        set: applyTheme,
        toggle: function () {
            var current = document.documentElement.getAttribute('data-theme') || 'blazing-light';
            applyTheme(current === 'blazing-dark' ? 'blazing-light' : 'blazing-dark');
        }
    };

    window.BlazingBlogModal = {
        show: function (id) { var d = document.getElementById(id); if (d && d.showModal) d.showModal(); },
        close: function (id) { var d = document.getElementById(id); if (d && d.close) d.close(); }
    };

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-password-toggle]');
        if (!btn) return;
        var targetId = btn.getAttribute('data-password-toggle');
        var input = document.getElementById(targetId);
        if (!input) return;
        input.type = input.type === 'password' ? 'text' : 'password';
        btn.setAttribute('data-shown', input.type === 'text' ? 'true' : 'false');
    });
})();
