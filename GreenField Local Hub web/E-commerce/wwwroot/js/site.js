/**
 * GreenField Local Hub — site.js
 * Shared: Favourites/Wishlist functionality
 */
(function () {
    'use strict';

    const FAVS_KEY = 'glh-favourites';

    // ── Favourites store ──────────────────────────────────
    function getFavourites() {
        try {
            return JSON.parse(localStorage.getItem(FAVS_KEY) || '[]');
        } catch (e) {
            return [];
        }
    }

    function saveFavourites(favs) {
        localStorage.setItem(FAVS_KEY, JSON.stringify(favs));
    }

    function isFavourite(productId) {
        return getFavourites().includes(String(productId));
    }

    function toggleFavourite(productId) {
        let favs = getFavourites();
        const id = String(productId);
        if (favs.includes(id)) {
            favs = favs.filter(f => f !== id);
            return false;
        } else {
            favs.push(id);
            saveFavourites(favs);
            return true;
        }
        saveFavourites(favs);
    }

    // ── Sync wishlist button state ─────────────────────────
    function syncWishlistButtons() {
        document.querySelectorAll('.card-wishlist[data-product-id]').forEach(btn => {
            const id = btn.dataset.productId;
            const active = isFavourite(id);
            btn.classList.toggle('card-wishlist--active', active);
            btn.setAttribute('aria-label', active
                ? `Remove ${btn.dataset.productName || 'product'} from favourites`
                : `Save ${btn.dataset.productName || 'product'} for later`);
            const svg = btn.querySelector('svg');
            if (svg) svg.style.fill = active ? 'currentColor' : 'none';
        });
    }

    // ── Toast helper (used across pages) ──────────────────
    function showToast(message, type = 'success', icon = '✓') {
        const container = document.getElementById('toastContainer');
        if (!container) return;
        const toast = document.createElement('div');
        toast.className = `toast toast--${type}`;
        toast.innerHTML = `<span class="toast__icon" aria-hidden="true">${icon}</span><span>${message}</span>`;
        container.appendChild(toast);
        setTimeout(() => {
            toast.classList.add('removing');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // ── Wishlist click handler ─────────────────────────────
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
    document.addEventListener('DOMContentLoaded', syncWishlistButtons);

    // Expose for other scripts
    window.GreenHub = window.GreenHub || {};
    window.GreenHub.showToast = showToast;
    window.GreenHub.getFavourites = getFavourites;
    window.GreenHub.isFavourite = isFavourite;
})();
