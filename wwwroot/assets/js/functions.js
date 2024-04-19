
document.addEventListener("DOMContentLoaded", _ => {
    const topNav = document.getElementsByClassName("topnav")[0];
    if (topNav) {
        window.onscroll = () => {
            if (window.scrollY > 50) {
                topNav.classList.add('scrollednav', 'py-0')
            }
            else {
                topNav.classList.remove('scrollednav', 'py-0')
            }
        };
    }
});

function togglePasswordVisibility() {
    const passwordInput = document.getElementById("password");
    const passwordIcon = document.getElementById("password-icon");
    if (passwordInput.type === "password") {
        passwordInput.type = "text";
        passwordIcon.classList.remove("bi-eye-slash");
        passwordIcon.classList.add("bi-eye");
    } else {
        passwordInput.type = "password";
        passwordIcon.classList.remove("bi-eye");
        passwordIcon.classList.add("bi-eye-slash");
    }
}

let storedURL = window.location.href;

// Function to check for URL changes and reset scroll position
function checkURLChange() {
    if (window.location.href !== storedURL) {
        // URL has changed, reset scroll position
        document.documentElement.scrollTop = 0;
        document.body.scrollTop = 0;

        // Update storedURL with the new URL
        storedURL = window.location.href;
    }
}

// Check for URL changes every 500 milliseconds (adjust as needed)
setInterval(checkURLChange, 10);


