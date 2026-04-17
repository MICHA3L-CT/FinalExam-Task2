// ── Dark / Light Mode Toggle ──────────────────────────────────────────────────
const themeToggleBtn = document.getElementById('theme-toggle');
const htmlEl = document.documentElement;

// Load saved theme on page load
const savedTheme = localStorage.getItem('glh-theme') || 'light';
htmlEl.setAttribute('data-theme', savedTheme);
updateToggleIcon(savedTheme);

themeToggleBtn.addEventListener('click', () => {
    const current = htmlEl.getAttribute('data-theme');
    const next = current === 'light' ? 'dark' : 'light';
    htmlEl.setAttribute('data-theme', next);
    localStorage.setItem('glh-theme', next);
    updateToggleIcon(next);
});

function updateToggleIcon(theme) {
    themeToggleBtn.textContent = theme === 'dark' ? '☀️' : '🌙';
}

// ── Active Nav Link ───────────────────────────────────────────────────────────
const currentPath = window.location.pathname.toLowerCase();
document.querySelectorAll('.navbar-links a').forEach(link => {
    if (link.getAttribute('href').toLowerCase() === currentPath) {
        link.classList.add('active');
    }
});

// ── Smooth Scroll for "Meet Our Producers" button ────────────────────────────
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
const navbar = document.querySelector('.navbar');
window.addEventListener('scroll', () => {
    if (window.scrollY > 10) {
        navbar.style.boxShadow = '0 4px 20px rgba(47, 111, 78, 0.18)';
    } else {
        navbar.style.boxShadow = '0 4px 16px rgba(47, 111, 78, 0.10)';
    }
});

// ── Fade-in on scroll (Intersection Observer) ────────────────────────────────
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -50px 0px'
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add('visible');
            observer.unobserve(entry.target);
        }
    });
}, observerOptions);

document.querySelectorAll('.fade-in').forEach(el => observer.observe(el));