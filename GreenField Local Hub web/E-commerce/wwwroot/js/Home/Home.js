// ── Dark / Light Mode Toggle ──────────────────────────────────────────────────
const themeToggleBtn = document.getElementById('theme-toggle');
const htmlEl = document.documentElement;

// Load saved theme from localStorage, default to light if nothing is saved
const savedTheme = localStorage.getItem('glh-theme') || 'light';
htmlEl.setAttribute('data-theme', savedTheme);
updateToggleIcon(savedTheme);

// Switch between light and dark theme on button click, then save the choice
themeToggleBtn.addEventListener('click', () => {
    const current = htmlEl.getAttribute('data-theme');
    const next = current === 'light' ? 'dark' : 'light';
    htmlEl.setAttribute('data-theme', next);
    localStorage.setItem('glh-theme', next);
    updateToggleIcon(next);
});

// Show moon icon in light mode, sun icon in dark mode
function updateToggleIcon(theme) {
    themeToggleBtn.textContent = theme === 'dark' ? '☀️' : '🌙';
}

// ── Active Nav Link ───────────────────────────────────────────────────────────
// Highlight the nav link that matches the current page URL
const currentPath = window.location.pathname.toLowerCase();
document.querySelectorAll('.navbar-links a').forEach(link => {
    if (link.getAttribute('href').toLowerCase() === currentPath) {
        link.classList.add('active');
    }
});

// ── Smooth Scroll for "Meet Our Producers" button ────────────────────────────
// Clicking the button scrolls down to the producers section instead of jumping
const meetProducersBtn = document.getElementById('meet-producers-btn');
if (meetProducersBtn) {
    meetProducersBtn.addEventListener('click', () => {
        const section = document.getElementById('producers-section');
        if (section) {
            section.scrollIntoView({ behavior: 'smooth' });
        }
    });
}

// ── Navbar shadow on scroll ───────────────────────────────────────────────────
// Add a stronger shadow once the user scrolls down a bit so the nav stands out
const navbar = document.querySelector('.navbar');
window.addEventListener('scroll', () => {
    if (window.scrollY > 10) {
        navbar.style.boxShadow = '0 4px 20px rgba(47, 111, 78, 0.18)';
    } else {
        navbar.style.boxShadow = '0 4px 16px rgba(47, 111, 78, 0.10)';
    }
});

// ── Fade-in on scroll (Intersection Observer) ────────────────────────────────
// Trigger a CSS fade-in animation when an element enters the viewport
const observerOptions = {
    threshold: 0.1,           // Fire when 10% of the element is visible
    rootMargin: '0px 0px -50px 0px'  // Slightly before the element hits the bottom edge
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            observer.unobserve(entry.target); // Only animate once, then stop watching
        }
    });
}, observerOptions);

// Attach the observer to every element that should fade in on scroll
document.querySelectorAll('.fade-in').forEach(el => observer.observe(el));
