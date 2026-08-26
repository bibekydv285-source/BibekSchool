// Site-wide JavaScript for Bibek School
// Optimized for Samsung Android (Samsung Internet & Chrome)
// Contains ONLY truly global/shared functionality

(function () {
    'use strict';

    // ========================================================================
    // UTILITY FUNCTIONS
    // ========================================================================

    // Check if we're on a touch device
    const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

    // Check if Samsung Internet
    const isSamsungInternet = /SamsungBrowser/i.test(navigator.userAgent);

    // Check if Android
    const isAndroid = /Android/i.test(navigator.userAgent);

    // Debounce function
    function debounce(func, wait) {
        let timeout;
        return function (...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), wait);
        };
    }

    // Throttle function
    function throttle(func, limit) {
        let inThrottle;
        return function (...args) {
            if (!inThrottle) {
                func.apply(this, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    // Safe bootstrap component getter
    function getBootstrapComponent(element, componentName) {
        try {
            return bootstrap[componentName].getInstance(element) ||
                bootstrap[componentName].getOrCreateInstance(element);
        } catch (e) {
            return null;
        }
    }

    // ========================================================================
    // SHARED SCROLL LOCK (FIX: sidebar + modal no longer clobber each other)
    // ========================================================================
    //
    // Previously the sidebar and Bootstrap modal handlers each independently
    // set document.body.style.overflow = 'hidden' / '' whenever they opened
    // or closed. If a modal was opened from inside the sidebar (or the two
    // otherwise overlapped), closing one of them would clear the OTHER's
    // scroll lock — leaving the page either scrollable when it shouldn't be,
    // or in some orderings, stuck with overflow:hidden permanently (i.e.
    // the page appears to stop scrolling entirely). A simple reference
    // counter fixes this: the lock is only released once every "locker"
    // has released it.

    let scrollLockCount = 0;

    function lockScroll() {
        scrollLockCount++;
        document.body.style.overflow = 'hidden';
    }

    function unlockScroll() {
        scrollLockCount = Math.max(0, scrollLockCount - 1);
        if (scrollLockCount === 0) {
            document.body.style.overflow = '';
        }
    }

    // Safety net: if anything ever leaves the page stuck non-scrollable
    // (e.g. a stray modal/sidebar state after navigation via bfcache),
    // make sure we start from a clean, unlocked state.
    window.addEventListener('pageshow', function (e) {
        if (e.persisted) {
            scrollLockCount = 0;
            document.body.style.overflow = '';
        }
    });

    // ========================================================================
    // INITIALIZE BOOTSTRAP COMPONENTS
    // ========================================================================

    // Initialize tooltips
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        new bootstrap.Tooltip(el);
    });

    // Initialize popovers
    document.querySelectorAll('[data-bs-toggle="popover"]').forEach(function (el) {
        new bootstrap.Popover(el);
    });

    // ========================================================================
    // SIDEBAR MOBILE TOGGLE (Optimized for Samsung Android)
    // ========================================================================

    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');

    let sidebarTouchStartX = 0;
    let sidebarTouchStartY = 0;
    let isSidebarDragging = false;

    if (sidebarToggle && sidebar && sidebarOverlay) {
        // Safety: Ensure overlay is hidden on page load (in case of stale state from bfcache)
        sidebarOverlay.classList.remove('show');
        document.body.classList.remove('sidebar-open');
        unlockScroll();

        function openSidebar() {
            sidebar.classList.add('show');
            sidebarOverlay.classList.add('show');
            sidebarToggle.setAttribute('aria-expanded', 'true');
            document.body.classList.add('sidebar-open');
            lockScroll();
            // Trap focus in sidebar for accessibility
            sidebar.focus({ preventScroll: true });
        }

        function closeSidebar() {
            if (!sidebar.classList.contains('show')) {
                // Safety: ensure overlay is also hidden if sidebar is not shown
                sidebarOverlay.classList.remove('show');
                return; // FIX: avoid double-unlock
            }
            sidebar.classList.remove('show');
            sidebarOverlay.classList.remove('show');
            sidebarToggle.setAttribute('aria-expanded', 'false');
            document.body.classList.remove('sidebar-open');
            unlockScroll();
            // Return focus to toggle button
            sidebarToggle.focus({ preventScroll: true });
        }

        function toggleSidebar(e) {
            if (e) {
                e.preventDefault();
                e.stopPropagation();
            }
            if (sidebar.classList.contains('show')) {
                closeSidebar();
            } else {
                openSidebar();
            }
        }

        // Click handler for toggle button
        sidebarToggle.addEventListener('click', toggleSidebar, { passive: false });

        // Touch handler for toggle button (Samsung Internet fix)
        sidebarToggle.addEventListener('touchend', function (e) {
            e.preventDefault();
            toggleSidebar();
        }, { passive: false });

        // Overlay click/touch to close
        sidebarOverlay.addEventListener('click', closeSidebar);
        sidebarOverlay.addEventListener('touchend', function (e) {
            e.preventDefault();
            closeSidebar();
        }, { passive: false });

        // Close sidebar on escape key
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && sidebar.classList.contains('show')) {
                closeSidebar();
            }
        });

        // Close sidebar when clicking a nav link on mobile (including form submits)
        function handleNavClick(e) {
            if (window.innerWidth < 992) {
                const target = e.target.closest('.nav-link, .nav-link button[type="submit"]');
                if (target) {
                    // Small delay to allow navigation to start
                    setTimeout(closeSidebar, 100);
                }
            }
        }

        // Use event delegation for dynamically added links
        sidebar.addEventListener('click', handleNavClick);
        sidebar.addEventListener('submit', function (e) {
            if (window.innerWidth < 992) {
                setTimeout(closeSidebar, 100);
            }
        });

        // Handle window resize with debounce
        window.addEventListener('resize', debounce(function () {
            if (window.innerWidth >= 992 && sidebar.classList.contains('show')) {
                closeSidebar();
            }
        }, 100));

        // Touch swipe to close sidebar (right-to-left swipe)
        sidebar.addEventListener('touchstart', function (e) {
            sidebarTouchStartX = e.touches[0].clientX;
            sidebarTouchStartY = e.touches[0].clientY;
            isSidebarDragging = false;
        }, { passive: true });

        sidebar.addEventListener('touchmove', function (e) {
            if (!sidebar.classList.contains('show')) return;

            const touchX = e.touches[0].clientX;
            const touchY = e.touches[0].clientY;
            const diffX = sidebarTouchStartX - touchX;
            const diffY = Math.abs(sidebarTouchStartY - touchY);

            // Detect horizontal swipe (right to left) with minimal vertical movement
            if (diffX > 50 && diffY < 50) {
                isSidebarDragging = true;
            }
        }, { passive: true });

        sidebar.addEventListener('touchend', function (e) {
            if (isSidebarDragging && sidebar.classList.contains('show')) {
                closeSidebar();
            }
            isSidebarDragging = false;
        }, { passive: true });
    }

    // ========================================================================
    // DROPDOWN TOUCH HANDLING (Samsung Android fix)
    // ========================================================================

    // Ensure dropdowns work properly on touch devices
    document.addEventListener('touchstart', function (e) {
        const dropdownToggle = e.target.closest('[data-bs-toggle="dropdown"]');
        if (dropdownToggle) {
            // Mark that a dropdown was touched
            dropdownToggle.dataset.touched = 'true';

            // Ensure dropdown is initialized
            const dropdown = getBootstrapComponent(dropdownToggle, 'Dropdown');
            if (dropdown) {
                // On Samsung, sometimes need to manually toggle
                if (isSamsungInternet && !dropdownToggle.classList.contains('show')) {
                    dropdown.toggle();
                }
            }
        }
    }, { passive: true });

    // Close dropdowns when clicking outside (but not on the toggle)
    document.addEventListener('click', function (e) {
        const dropdowns = document.querySelectorAll('.dropdown-menu.show');
        dropdowns.forEach(function (dropdown) {
            const toggle = dropdown.previousElementSibling;
            if (toggle && !dropdown.contains(e.target) && !toggle.contains(e.target)) {
                const bsDropdown = getBootstrapComponent(toggle, 'Dropdown');
                if (bsDropdown) bsDropdown.hide();
            }
        });
    });

    // Touchend handler for dropdowns on Samsung
    document.addEventListener('touchend', function (e) {
        const dropdownToggle = e.target.closest('[data-bs-toggle="dropdown"]');
        if (dropdownToggle && dropdownToggle.dataset.touched) {
            delete dropdownToggle.dataset.touched;
            const dropdown = getBootstrapComponent(dropdownToggle, 'Dropdown');
            if (dropdown && isSamsungInternet) {
                // Ensure dropdown stays open on Samsung
                const menu = dropdownToggle.nextElementSibling;
                if (menu && !menu.classList.contains('show')) {
                    dropdown.toggle();
                }
            }
        }
    }, { passive: true });

    // ========================================================================
    // MODAL TOUCH HANDLING & FOCUS TRAPPING
    // ========================================================================

    // Ensure modals work properly on Samsung
    document.addEventListener('show.bs.modal', function (e) {
        const modal = e.target;
        // FIX: use the shared lock instead of a plain overflow assignment
        // so this can't clobber a sidebar (or another modal) that's
        // also relying on the lock.
        lockScroll();
        // Ensure modal has proper z-index
        modal.style.zIndex = '1050';
    });

    document.addEventListener('hidden.bs.modal', function (e) {
        // FIX: release this modal's share of the lock. With a counter we
        // no longer need to manually check "is another modal still open" —
        // the count handles stacked modals and an open sidebar correctly.
        unlockScroll();
    });

    // Focus trapping for modals on mobile
    document.addEventListener('shown.bs.modal', function (e) {
        const modal = e.target;
        const focusableElements = modal.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        if (focusableElements.length > 0) {
            focusableElements[0].focus({ preventScroll: true });
        }
    });

    // ========================================================================
    // FORM INPUT ZOOM PREVENTION (Android/Samsung)
    // ========================================================================

    // Prevent zoom on input focus by ensuring font-size >= 16px
    function preventInputZoom() {
        const inputs = document.querySelectorAll('input, select, textarea');
        inputs.forEach(function (input) {
            // Skip hidden inputs
            if (input.type === 'hidden') return;

            const style = window.getComputedStyle(input);
            const fontSize = parseFloat(style.fontSize);

            if (fontSize < 16) {
                input.style.fontSize = '16px';
            }

            // Ensure touch-action for better touch handling
            input.style.touchAction = 'manipulation';
        });
    }

    // Run on DOM ready and after dynamic content
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', preventInputZoom);
    } else {
        preventInputZoom();
    }

    // Re-run for dynamically added inputs
    if ('MutationObserver' in window) {
        const inputObserver = new MutationObserver(function (mutations) {
            let shouldCheck = false;
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) { // Element node
                        if (node.matches && (node.matches('input, select, textarea') || node.querySelector('input, select, textarea'))) {
                            shouldCheck = true;
                        }
                    }
                });
            });
            if (shouldCheck) preventInputZoom();
        });
        inputObserver.observe(document.body, { childList: true, subtree: true });
    }

    // ========================================================================
    // AUTO-DISMISS ALERTS
    // ========================================================================

    setTimeout(function () {
        const alerts = document.querySelectorAll('.alert-dismissible:not(.alert-permanent)');
        alerts.forEach(function (alert) {
            const bsAlert = getBootstrapComponent(alert, 'Alert');
            if (bsAlert) bsAlert.close();
        });
    }, 5000);

    // ========================================================================
    // FORM VALIDATION ENHANCEMENT
    // ========================================================================

    document.querySelectorAll('.needs-validation').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // ========================================================================
    // PASSWORD STRENGTH INDICATOR (global fallback for data-strength)
    // ========================================================================

    document.querySelectorAll('input[type="password"][data-strength]').forEach(function (input) {
        const strengthBar = document.createElement('div');
        strengthBar.className = 'password-strength';
        strengthBar.innerHTML = '<div class="password-strength-bar"></div>';
        input.parentNode.appendChild(strengthBar);

        input.addEventListener('input', function () {
            const password = this.value;
            const bar = strengthBar.querySelector('.password-strength-bar');
            let strength = 0;

            if (password.length >= 8) strength++;
            if (password.match(/[a-z]+/)) strength++;
            if (password.match(/[A-Z]+/)) strength++;
            if (password.match(/[0-9]+/)) strength++;
            if (password.match(/[^a-zA-Z0-9]+/)) strength++;

            bar.className = 'password-strength-bar';
            if (strength <= 1) bar.classList.add('password-strength-weak');
            else if (strength === 2) bar.classList.add('password-strength-fair');
            else if (strength === 3) bar.classList.add('password-strength-good');
            else bar.classList.add('password-strength-strong');
        });
    });

    // ========================================================================
    // CONFIRMATION DIALOGS
    // ========================================================================

    document.addEventListener('click', function (e) {
        const confirmBtn = e.target.closest('[data-confirm]');
        if (confirmBtn) {
            const message = confirmBtn.getAttribute('data-confirm') || 'Are you sure?';
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        }
    });

    

    // ========================================================================
    // AUTO-RESIZE TEXTAREAS
    // ========================================================================

    document.querySelectorAll('textarea[data-auto-resize]').forEach(function (textarea) {
        textarea.addEventListener('input', function () {
            this.style.height = 'auto';
            this.style.height = (this.scrollHeight) + 'px';
        });
        // Trigger once on load
        textarea.dispatchEvent(new Event('input'));
    });

    // ========================================================================
    // LOADING STATE FOR BUTTONS
    // ========================================================================

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-loading-text]');
        if (btn && !btn.disabled) {
            const originalText = btn.innerHTML;
            const loadingText = btn.getAttribute('data-loading-text');
            btn.disabled = true;
            btn.innerHTML = loadingText;
            btn.classList.add('loading');

            // Re-enable after form submit or timeout
            const form = btn.closest('form');
            if (form) {
                form.addEventListener('submit', function () {
                    setTimeout(function () {
                        btn.disabled = false;
                        btn.innerHTML = originalText;
                        btn.classList.remove('loading');
                    }, 3000);
                }, { once: true });
            } else {
                setTimeout(function () {
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                    btn.classList.remove('loading');
                }, 3000);
            }
        }
    });

    // ========================================================================
    // COPY TO CLIPBOARD
    // ========================================================================

    document.addEventListener('click', function (e) {
        const copyBtn = e.target.closest('[data-copy]');
        if (copyBtn) {
            const text = copyBtn.getAttribute('data-copy');
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(function () {
                    const originalText = copyBtn.innerHTML;
                    copyBtn.innerHTML = '<i class="fas fa-check me-1"></i>Copied!';
                    copyBtn.classList.add('btn-success');
                    copyBtn.classList.remove('btn-outline-secondary');
                    setTimeout(function () {
                        copyBtn.innerHTML = originalText;
                        copyBtn.classList.remove('btn-success');
                        copyBtn.classList.add('btn-outline-secondary');
                    }, 2000);
                }).catch(function () {
                    fallbackCopyText(text, copyBtn);
                });
            } else {
                fallbackCopyText(text, copyBtn);
            }
        }
    });

    function fallbackCopyText(text, btn) {
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.opacity = '0';
        document.body.appendChild(textArea);
        textArea.select();
        try {
            document.execCommand('copy');
            const originalText = btn.innerHTML;
            btn.innerHTML = '<i class="fas fa-check me-1"></i>Copied!';
            btn.classList.add('btn-success');
            btn.classList.remove('btn-outline-secondary');
            setTimeout(function () {
                btn.innerHTML = originalText;
                btn.classList.remove('btn-success');
                btn.classList.add('btn-outline-secondary');
            }, 2000);
        } catch (err) {
            console.error('Copy failed:', err);
        }
        document.body.removeChild(textArea);
    }

    // ========================================================================
    // PRINT BUTTON
    // ========================================================================

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-print]')) {
            window.print();
        }
    });

    // ========================================================================
    // BACK BUTTON
    // ========================================================================

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-back]')) {
            history.back();
        }
    });

    // ========================================================================
    // REFRESH PAGE
    // ========================================================================

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-refresh]')) {
            location.reload();
        }
    });

    // ========================================================================
    // THEME TOGGLE (if implemented)
    // ========================================================================

    const themeToggle = document.getElementById('themeToggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            document.body.classList.toggle('dark-mode');
            localStorage.setItem('darkMode', document.body.classList.contains('dark-mode'));
        });

        // Load saved theme
        if (localStorage.getItem('darkMode') === 'true') {
            document.body.classList.add('dark-mode');
        }
    }

    // ========================================================================
    // KEYBOARD SHORTCUTS
    // ========================================================================

    document.addEventListener('keydown', function (e) {
        // Ctrl/Cmd + K for search
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            const searchInput = document.querySelector('input[type="search"], input[name="search"]');
            if (searchInput) {
                searchInput.focus();
            }
        }

        // Escape to close modals
        if (e.key === 'Escape') {
            const openModal = document.querySelector('.modal.show');
            if (openModal) {
                const modal = getBootstrapComponent(openModal, 'Modal');
                if (modal) modal.hide();
            }
        }
    });

    // ========================================================================
    // SMOOTH SCROLL FOR ANCHOR LINKS
    // ========================================================================

    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;

            const target = document.querySelector(targetId);
            if (target) {
                e.preventDefault();
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
                target.focus({ preventScroll: true });
            }
        });
    });

    // ========================================================================
    // LAZY LOAD IMAGES
    // ========================================================================

    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    observer.unobserve(img);
                }
            });
        });

        document.querySelectorAll('img.lazy[data-src]').forEach(img => {
            imageObserver.observe(img);
        });
    }

    // ========================================================================
    // MOBILE TABLE STACKING
    // ========================================================================

    function initTableStacking() {
        const tables = document.querySelectorAll('.table-mobile-stack');
        tables.forEach(function (table) {
            // Get visible headers (not hidden on mobile)
            const headers = Array.from(table.querySelectorAll('thead th:not([data-mobile-hide])')).map(function (th) {
                return th.textContent.trim();
            });

            table.querySelectorAll('tbody tr').forEach(function (row) {
                const visibleCells = Array.from(row.querySelectorAll('td:not([data-mobile-hide])'));
                visibleCells.forEach(function (td, index) {
                    if (headers[index]) {
                        td.setAttribute('data-label', headers[index]);
                    }
                });
            });
        });
    }

    // Initialize table stacking on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTableStacking);
    } else {
        initTableStacking();
    }

    // Re-initialize on dynamic content load
    document.addEventListener('htmx:afterSwap', initTableStacking);

    // Also observe for dynamically added tables
    if ('MutationObserver' in window) {
        const tableObserver = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) { // Element node
                        if (node.matches && node.matches('.table-mobile-stack')) {
                            initTableStacking();
                        } else if (node.querySelectorAll) {
                            const tables = node.querySelectorAll('.table-mobile-stack');
                            if (tables.length) initTableStacking();
                        }
                    }
                });
            });
        });
        tableObserver.observe(document.body, { childList: true, subtree: true });
    }

    // ========================================================================
    // EXPORT GLOBAL FUNCTIONS
    // ========================================================================

    window.BibekSchool = {
        showAlert: function (message, type, container) {
            type = type || 'info';
            container = container || document.body;

            const alert = document.createElement('div');
            alert.className = `alert alert-${type} alert-dismissible fade show`;
            alert.role = 'alert';
            alert.innerHTML = `
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;

            container.insertBefore(alert, container.firstChild);

            setTimeout(() => {
                const bsAlert = getBootstrapComponent(alert, 'Alert');
                if (bsAlert) bsAlert.close();
            }, 5000);
        },

        showLoading: function (element) {
            if (element) {
                element.classList.add('loading');
                element.disabled = true;
            }
        },

        hideLoading: function (element) {
            if (element) {
                element.classList.remove('loading');
                element.disabled = false;
            }
        },

        confirmAction: function (message, callback) {
            if (confirm(message)) {
                callback();
            }
        },

        formatDate: function (date) {
            return new Date(date).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            });
        },

        formatDateTime: function (date) {
            return new Date(date).toLocaleString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        },

        formatNumber: function (num, decimals = 2) {
            return Number(num).toFixed(decimals);
        },

        // Exposed for pages/components that need manual scroll locking
        // (e.g. a custom off-canvas panel) so they stay in sync with the
        // sidebar/modal lock instead of setting overflow directly.
        lockScroll: lockScroll,
        unlockScroll: unlockScroll,

        debounce: debounce,
        throttle: throttle
    };

    // ========================================================================
    // SAMSUNG ANDROID SPECIFIC FIXES
    // ========================================================================

    // Fix for Samsung: ensure active states work on touch
    if (isTouchDevice) {
        document.addEventListener('touchstart', function () { }, { passive: true });
    }

})();
