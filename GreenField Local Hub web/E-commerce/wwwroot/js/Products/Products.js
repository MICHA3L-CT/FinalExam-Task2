/**
 * Greenfield Local Hub – Products Index
 * Handles: search, category filtering, sort, basket add, toast notifications
 * File: ~/js/Products/Products.js
 */

(function () {
    'use strict';

    // State
    const state = {
        activeCategory: 'all',
        searchQuery: '',
        sortBy: 'newest',
    };

    // DOM refs
    const grid = document.getElementById('productGrid');
    const getCards = () => {
        if (!grid) return [];
        return Array.from(grid.querySelectorAll('.product-card'));
    };
    const searchInput = document.getElementById('productSearch');
    const sortSelect = document.getElementById('sortSelect');
    const filterPills = document.querySelectorAll('.filter-pill');
    const countBadge = document.getElementById('productCount');
    const clearBtn = document.getElementById('clearFilters');
    const toastContainer = document.getElementById('toastContainer');

    // Helper Functions
    function updateCount(visibleCount) {
        if (!countBadge) return;
        const count = visibleCount !== undefined ? visibleCount : getCards().length;
        countBadge.textContent = `${count} item${count !== 1 ? 's' : ''} found`;
    }

    function toggleEmptyState(show) {
        if (!grid) return;
        let emptyState = document.getElementById('emptyState');

        if (show && !emptyState) {
            emptyState = document.createElement('div');
            emptyState.id = 'emptyState';
            emptyState.className = 'empty-state';
            emptyState.innerHTML = `
                <div class="empty-state__icon">🌿</div>
                <div class="empty-state__title">No products found</div>
                <div class="empty-state__text">Try a different search or category.</div>
            `;
            grid.appendChild(emptyState);
        } else if (emptyState) {
            emptyState.style.display = show ? 'flex' : 'none';
        }
    }

    // Filter Functions
    function applyFilters() {
        let visibleCount = 0;
        const searchLower = state.searchQuery.toLowerCase();

        getCards().forEach(card => {
            const category = (card.dataset.category || '').toLowerCase();
            const name = (card.dataset.name || '').toLowerCase();
            const description = (card.dataset.description || '').toLowerCase();
            const producer = (card.dataset.producer || '').toLowerCase();

            const matchesCategory = state.activeCategory === 'all' || category === state.activeCategory;

            let matchesSearch = true;
            if (state.searchQuery) {
                matchesSearch = name.includes(searchLower) ||
                    description.includes(searchLower) ||
                    producer.includes(searchLower);
            }

            const shouldShow = matchesCategory && matchesSearch;
            card.style.display = shouldShow ? '' : 'none';
            if (shouldShow) visibleCount++;
        });

        updateCount(visibleCount);
        toggleEmptyState(visibleCount === 0);
    }

    function onSearch(e) {
        state.searchQuery = e.target.value;
        applyFilters();
    }

    function onFilter(pill) {
        filterPills.forEach(p => p.classList.remove('active'));
        pill.classList.add('active');
        state.activeCategory = pill.dataset.category || 'all';
        applyFilters();
    }

    // Sort Functions
    function sortCards() {
        if (!grid) return;

        const cards = getCards();
        const sorted = [...cards].sort((a, b) => {
            const priceA = parseFloat(a.dataset.price || 0);
            const priceB = parseFloat(b.dataset.price || 0);
            const nameA = (a.dataset.name || '').toLowerCase();
            const nameB = (b.dataset.name || '').toLowerCase();
            const dateA = new Date(a.dataset.date || 0);
            const dateB = new Date(b.dataset.date || 0);

            switch (state.sortBy) {
                case 'price-asc': return priceA - priceB;
                case 'price-desc': return priceB - priceA;
                case 'name-asc': return nameA.localeCompare(nameB);
                case 'name-desc': return nameB.localeCompare(nameA);
                case 'newest': return dateB - dateA;
                case 'oldest': return dateA - dateB;
                default: return 0;
            }
        });

        sorted.forEach(card => grid.appendChild(card));
        applyFilters();
    }

    function onSort(e) {
        state.sortBy = e.target.value;
        sortCards();
    }

    // Clear Filters
    function clearFilters() {
        state.activeCategory = 'all';
        state.searchQuery = '';

        if (searchInput) searchInput.value = '';

        filterPills.forEach(p => p.classList.remove('active'));
        const allPill = document.querySelector('.filter-pill[data-category="all"]');
        if (allPill) allPill.classList.add('active');

        applyFilters();
    }

    // Add to Basket
    function onAddToBasket(e) {
        e.preventDefault();
        const form = e.currentTarget;
        const productName = form.dataset.productName || 'Product';
        const submitBtn = form.querySelector('.btn-add-basket');

        if (submitBtn) {
            const originalHtml = submitBtn.innerHTML;
            submitBtn.innerHTML = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg> Added!`;
            submitBtn.disabled = true;

            const formData = new FormData(form);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token || ''
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        showToast(`✓ ${productName} added to basket`, 'success');
                    } else {
                        showToast(data.message || 'Failed to add item', 'error');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    showToast('Failed to add item to basket', 'error');
                })
                .finally(() => {
                    setTimeout(() => {
                        submitBtn.innerHTML = originalHtml;
                        submitBtn.disabled = false;
                    }, 1500);
                });
        }
    }

    // Toast Notifications
    function showToast(message, type = 'success') {
        if (!toastContainer) {
            console.warn('Toast container not found');
            return;
        }

        const toast = document.createElement('div');
        toast.className = `toast toast--${type}`;
        toast.innerHTML = `
            <span class="toast__icon">${type === 'success' ? '✓' : '✕'}</span>
            <span>${escapeHtml(message)}</span>
        `;

        toastContainer.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('removing');
            toast.addEventListener('animationend', () => toast.remove());
        }, 3000);
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Debounce Utility
    function debounce(fn, delay) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => fn.apply(this, args), delay);
        };
    }

    // Initialization
    function init() {
        if (!grid) {
            console.warn('Product grid not found');
            return;
        }

        console.log('Products.js initialized');

        if (searchInput) {
            searchInput.addEventListener('input', debounce(onSearch, 300));
        }

        if (sortSelect) {
            sortSelect.addEventListener('change', onSort);
        }

        if (clearBtn) {
            clearBtn.addEventListener('click', clearFilters);
        }

        if (filterPills.length > 0) {
            filterPills.forEach(pill => {
                pill.addEventListener('click', () => onFilter(pill));
            });
        }

        const basketForms = document.querySelectorAll('.basket-form');
        if (basketForms.length > 0) {
            basketForms.forEach(form => {
                form.removeEventListener('submit', onAddToBasket);
                form.addEventListener('submit', onAddToBasket);
            });
        }

        sortCards();
        console.log('Products.js ready - Filters and sorting active');
    }

    // Export for external use
    window.GreenHub = {
        showToast,
        refreshFilters: applyFilters
    };

    // Start the application
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();