/**
 * Greenfield Local Hub – Products Index
 * Handles: search, category filtering, sort, basket add, toast notifications, cart badge
 * File: ~/js/Products/Products.js
 */

(function () {
    'use strict';

    // ── State ────────────────────────────────────────────────
    // Tracks the currently active filter/sort settings
    const state = {
        activeCategory: 'all',
        searchQuery: '',
        sortBy: 'newest',
    };

    // ── DOM refs ─────────────────────────────────────────────
    const grid = document.getElementById('productGrid');
    const getCards = () => grid ? Array.from(grid.querySelectorAll('.product-card')) : [];
    const searchInput  = document.getElementById('productSearch');
    const sortSelect   = document.getElementById('sortSelect');
    const filterPills  = document.querySelectorAll('.filter-pill');
    const countBadge   = document.getElementById('productCount');
    const clearBtn     = document.getElementById('clearFilters');
    const toastContainer = document.getElementById('toastContainer');

    // ── Count badge ──────────────────────────────────────────
    // Update the "X items found" label above the grid
    function updateCount(visibleCount) {
        if (!countBadge) return;
        const count = visibleCount !== undefined ? visibleCount : getCards().length;
        countBadge.textContent = `${count} item${count !== 1 ? 's' : ''} found`;
    }

    // ── Empty state ──────────────────────────────────────────
    // Show or hide the "No products found" placeholder inside the grid
    function toggleEmptyState(show) {
        if (!grid) return;
        let emptyState = document.getElementById('emptyState');
        if (show && !emptyState) {
            // Create the empty-state element the first time it is needed
            emptyState = document.createElement('div');
            emptyState.id = 'emptyState';
            emptyState.className = 'empty-state';
            emptyState.innerHTML = `
                <div class="empty-state__icon" aria-hidden="true">🌿</div>
                <div class="empty-state__title">No products found</div>
                <div class="empty-state__text">Try a different search or category.</div>
            `;
            grid.appendChild(emptyState);
        } else if (emptyState) {
            emptyState.style.display = show ? 'flex' : 'none';
        }
    }

    // ── Filtering ────────────────────────────────────────────
    // Show only cards that match the current category AND search text
    function applyFilters() {
        let visibleCount = 0;
        const searchLower = state.searchQuery.toLowerCase();

        getCards().forEach(card => {
            const category    = (card.dataset.category    || '').toLowerCase();
            const name        = (card.dataset.name        || '').toLowerCase();
            const description = (card.dataset.description || '').toLowerCase();
            const producer    = (card.dataset.producer    || '').toLowerCase();

            const matchesCategory = state.activeCategory === 'all' || category === state.activeCategory;
            const matchesSearch   = !state.searchQuery ||
                name.includes(searchLower) ||
                description.includes(searchLower) ||
                producer.includes(searchLower);

            const shouldShow = matchesCategory && matchesSearch;
            card.style.display = shouldShow ? '' : 'none';
            if (shouldShow) visibleCount++;
        });

        updateCount(visibleCount);
        toggleEmptyState(visibleCount === 0);
    }

    // Called each time the user types in the search box
    function onSearch(e) {
        state.searchQuery = e.target.value;
        applyFilters();
    }

    // Called when a category pill is clicked - make it active and re-filter
    function onFilter(pill) {
        filterPills.forEach(p => p.classList.remove('active'));
        pill.classList.add('active');
        state.activeCategory = pill.dataset.category || 'all';
        applyFilters();
    }

    // ── Sorting ──────────────────────────────────────────────
    // Re-order the product cards in the DOM based on the chosen sort option
    function sortCards() {
        if (!grid) return;
        const cards  = getCards();
        const sorted = [...cards].sort((a, b) => {
            const priceA = parseFloat(a.dataset.price || 0);
            const priceB = parseFloat(b.dataset.price || 0);
            const nameA  = (a.dataset.name || '').toLowerCase();
            const nameB  = (b.dataset.name || '').toLowerCase();
            const dateA  = new Date(a.dataset.date || 0);
            const dateB  = new Date(b.dataset.date || 0);

            switch (state.sortBy) {
                case 'price-asc':  return priceA - priceB;
                case 'price-desc': return priceB - priceA;
                case 'name-asc':   return nameA.localeCompare(nameB);
                case 'name-desc':  return nameB.localeCompare(nameA);
                case 'newest':     return dateB - dateA;
                case 'oldest':     return dateA - dateB;
                default:           return 0;
            }
        });
        // Move each card to its new position inside the grid
        sorted.forEach(card => grid.appendChild(card));
        applyFilters(); // Re-apply visibility after re-ordering
    }

    // Called when the sort dropdown changes
    function onSort(e) {
        state.sortBy = e.target.value;
        sortCards();
    }

    // ── Clear filters ────────────────────────────────────────
    // Reset search text, category, and sort back to defaults
    function clearFilters() {
        state.activeCategory = 'all';
        state.searchQuery    = '';
        if (searchInput) searchInput.value = '';
        filterPills.forEach(p => p.classList.remove('active'));
        const allPill = document.querySelector('.filter-pill[data-category="all"]');
        if (allPill) allPill.classList.add('active');
        applyFilters();
    }

    // ── Add to Basket (AJAX – no redirect) ──────────────────
    // Handle the "Add to Basket" form submit without reloading the page
    function onAddToBasket(e) {
        e.preventDefault();
        const form        = e.currentTarget;
        const productName = form.dataset.productName || 'Product';
        const submitBtn   = form.querySelector('.btn-add-basket');

        if (!submitBtn) return;

        // Temporarily change the button to a confirmation state while the request runs
        const originalHtml = submitBtn.innerHTML;
        submitBtn.innerHTML  = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" aria-hidden="true"><polyline points="20 6 9 17 4 12"/></svg> Added!`;
        submitBtn.disabled   = true;
        submitBtn.setAttribute('aria-label', productName + ' added to basket');

        const formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
        })
            .then(res => {
                if (!res.ok) throw new Error('Network error');
                return res.json();
            })
            .then(data => {
                if (data.success) {
                    showToast(`✓ ${productName} added to basket`, 'success');
                    // Update the cart icon badge in the navbar with the new total
                    if (typeof data.cartCount === 'number' && window.GreenHub) {
                        window.GreenHub.updateCartBadge(data.cartCount);
                    }
                } else {
                    showToast(data.message || 'Failed to add item', 'error');
                }
            })
            .catch(() => {
                showToast('Failed to add item to basket', 'error');
            })
            .finally(() => {
                // Restore the button after 1.5 seconds so the user can add again
                setTimeout(() => {
                    submitBtn.innerHTML = originalHtml;
                    submitBtn.disabled  = false;
                    submitBtn.setAttribute('aria-label', 'Add ' + productName + ' to basket');
                }, 1500);
            });
    }

    // ── Toast notifications ──────────────────────────────────
    // Create and show a temporary notification message at the bottom of the page
    function showToast(message, type = 'success') {
        const container = toastContainer || document.getElementById('toastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast toast--${type}`;
        toast.setAttribute('role', 'status');
        toast.setAttribute('aria-live', 'polite'); // Announce to screen readers
        toast.innerHTML = `
            <span class="toast__icon" aria-hidden="true">${type === 'success' ? '✓' : '✕'}</span>
            <span>${escapeHtml(message)}</span>
        `;

        container.appendChild(toast);

        // Start the removal animation after 3 seconds
        setTimeout(() => {
            toast.classList.add('removing');
            toast.addEventListener('animationend', () => toast.remove(), { once: true });
        }, 3000);
    }

    // Prevent XSS by escaping any HTML characters in the toast message text
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ── Debounce ─────────────────────────────────────────────
    // Delay calling fn until the user stops triggering the event for `delay` ms
    // Used for the search input so we don't filter on every single keystroke
    function debounce(fn, delay) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => fn.apply(this, args), delay);
        };
    }

    // ── Init ─────────────────────────────────────────────────
    function init() {
        if (!grid) return; // No product grid on this page, nothing to do

        if (searchInput) searchInput.addEventListener('input', debounce(onSearch, 300));
        if (sortSelect)  sortSelect.addEventListener('change', onSort);
        if (clearBtn)    clearBtn.addEventListener('click', clearFilters);

        filterPills.forEach(pill => pill.addEventListener('click', () => onFilter(pill)));

        // Attach the AJAX basket handler to every product's add-to-basket form
        document.querySelectorAll('.basket-form').forEach(form => {
            form.addEventListener('submit', onAddToBasket);
        });

        sortCards(); // Apply default sort order on page load
    }

    // ── Public API ───────────────────────────────────────────
    // Expose functions that other scripts (e.g. site.js) may call
    window.GreenHub = window.GreenHub || {};
    window.GreenHub.showToast      = showToast;
    window.GreenHub.refreshFilters = applyFilters;

    // Run init straight away if the DOM is already ready, otherwise wait
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
