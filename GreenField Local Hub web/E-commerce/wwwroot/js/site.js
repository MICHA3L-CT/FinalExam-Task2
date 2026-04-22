
(function () {
    'use strict';

    // Key used to store the favourites list in localStorage
    const FAVS_KEY = 'glh-favourites';

    // ── Favourites store ──────────────────────────────────
    // Read the saved list; return an empty array if nothing is stored or JSON is broken
    function getFavourites() {
        try {
            return JSON.parse(localStorage.getItem(FAVS_KEY) || '[]');
        } catch (e) {
            return [];
        }
    }

    // Write the updated favourites array back to localStorage
    function saveFavourites(favs) {
        localStorage.setItem(FAVS_KEY, JSON.stringify(favs));
    }

    // Check whether a product ID is already in the saved favourites list
    function isFavourite(productId) {
        return getFavourites().includes(String(productId));
    }

    // Add the product if it is not already saved, or remove it if it is
    // Returns true if it was added, false if it was removed
    function toggleFavourite(productId) {
        let favs = getFavourites();
        const id = String(productId);
        if (favs.includes(id)) {
            favs = favs.filter(f => f !== id);
            saveFavourites(favs);
            return false;
        } else {
            favs.push(id);
            saveFavourites(favs);
            return true;
        }
    }

    // ── Sync wishlist button state ─────────────────────────
    // Make all heart buttons reflect the current saved state (filled or outline)
    function syncWishlistButtons() {
        document.querySelectorAll('.card-wishlist[data-product-id]').forEach(btn => {
            const id = btn.dataset.productId;
            const active = isFavourite(id);
            btn.classList.toggle('card-wishlist--active', active);
            // Update the aria-label so screen readers say the correct action
            btn.setAttribute('aria-label', active
                ? `Remove ${btn.dataset.productName || 'product'} from favourites`
                : `Save ${btn.dataset.productName || 'product'} for later`);
            // Fill the SVG heart when saved, outline when not saved
            const svg = btn.querySelector('svg');
            if (svg) svg.style.fill = active ? 'currentColor' : 'none';
        });
    }

    // ── Toast helper (used across pages) ──────────────────
    // Show a small pop-up notification that disappears after 3 seconds
    function showToast(message, type = 'success', icon = '✓') {
        const container = document.getElementById('toastContainer');
        if (!container) return;
        const toast = document.createElement('div');
        toast.className = `toast toast--${type}`;
        toast.innerHTML = `<span class="toast__icon" aria-hidden="true">${icon}</span><span>${message}</span>`;
        container.appendChild(toast);
        setTimeout(() => {
            toast.classList.add('removing');
            setTimeout(() => toast.remove(), 300); // Let the CSS fade-out finish before removing the element
        }, 3000);
    }

    // ── Wishlist click handler ─────────────────────────────
    // Use event delegation so this works for cards added dynamically too
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.card-wishlist[data-product-id]');
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();
        const id = btn.dataset.productId;
        const name = btn.dataset.productName || 'Product';
        const added = toggleFavourite(id);
        syncWishlistButtons();
        if (added) {
            showToast(`❤️ ${name} added to favourites`, 'success', '❤️');
        } else {
            showToast(`${name} removed from favourites`, 'info', '♡');
        }
    });

    // ── Init on DOM ready ──────────────────────────────────
    // Sync button states once the page has finished loading
    document.addEventListener('DOMContentLoaded', syncWishlistButtons);

    // Expose useful functions on the global GreenHub namespace so other scripts can call them
    window.GreenHub = window.GreenHub || {};
    window.GreenHub.showToast = showToast;
    window.GreenHub.getFavourites = getFavourites;
    window.GreenHub.isFavourite = isFavourite;
})();
