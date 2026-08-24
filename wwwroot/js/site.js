// Site-wide JavaScript for Bibek School

(function() {
    'use strict';

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function(popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function() {
        const alerts = document.querySelectorAll('.alert-dismissible:not(.alert-permanent)');
        alerts.forEach(function(alert) {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        });
    }, 5000);

    // Form validation enhancement
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // Password strength indicator
    const passwordInputs = document.querySelectorAll('input[type="password"][data-strength]');
    passwordInputs.forEach(function(input) {
        const strengthBar = document.createElement('div');
        strengthBar.className = 'password-strength';
        strengthBar.innerHTML = '<div class="password-strength-bar"></div>';
        input.parentNode.appendChild(strengthBar);

        input.addEventListener('input', function() {
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

    // Confirmation dialogs
    document.addEventListener('click', function(e) {
        const confirmBtn = e.target.closest('[data-confirm]');
        if (confirmBtn) {
            const message = confirmBtn.getAttribute('data-confirm') || 'Are you sure?';
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        }
    });

    // Toggle password visibility - robust implementation using event delegation
    document.addEventListener('click', function(e) {
        const toggleBtn = e.target.closest('.toggle-password');
        if (!toggleBtn) return;

        const targetId = toggleBtn.getAttribute('data-target');
        if (!targetId) return;

        const input = document.getElementById(targetId);
        const icon = toggleBtn.querySelector('i');

        if (input && icon) {
            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.remove('fa-eye');
                icon.classList.add('fa-eye-slash');
            } else {
                input.type = 'password';
                icon.classList.remove('fa-eye-slash');
                icon.classList.add('fa-eye');
            }
        }
    });

    // Auto-resize textareas
    const textareas = document.querySelectorAll('textarea[data-auto-resize]');
    textareas.forEach(function(textarea) {
        textarea.addEventListener('input', function() {
            this.style.height = 'auto';
            this.style.height = (this.scrollHeight) + 'px';
        });
        // Trigger once on load
        textarea.dispatchEvent(new Event('input'));
    });

    // Loading state for buttons
    document.addEventListener('click', function(e) {
        const btn = e.target.closest('[data-loading-text]');
        if (btn && !btn.disabled) {
            const originalText = btn.innerHTML;
            const loadingText = btn.getAttribute('data-loading-text');
            btn.disabled = true;
            btn.innerHTML = loadingText;

            // Re-enable after form submit or timeout
            const form = btn.closest('form');
            if (form) {
                form.addEventListener('submit', function() {
                    setTimeout(function() {
                        btn.disabled = false;
                        btn.innerHTML = originalText;
                    }, 3000);
                }, { once: true });
            } else {
                setTimeout(function() {
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }, 3000);
            }
        }
    });

    // Copy to clipboard
    document.addEventListener('click', function(e) {
        const copyBtn = e.target.closest('[data-copy]');
        if (copyBtn) {
            const text = copyBtn.getAttribute('data-copy');
            navigator.clipboard.writeText(text).then(function() {
                const originalText = copyBtn.innerHTML;
                copyBtn.innerHTML = '<i class="fas fa-check me-1"></i>Copied!';
                copyBtn.classList.add('btn-success');
                copyBtn.classList.remove('btn-outline-secondary');
                setTimeout(function() {
                    copyBtn.innerHTML = originalText;
                    copyBtn.classList.remove('btn-success');
                    copyBtn.classList.add('btn-outline-secondary');
                }, 2000);
            });
        }
    });

    // Print button
    document.addEventListener('click', function(e) {
        if (e.target.closest('[data-print]')) {
            window.print();
        }
    });

    // Back button
    document.addEventListener('click', function(e) {
        if (e.target.closest('[data-back]')) {
            history.back();
        }
    });

    // Refresh page
    document.addEventListener('click', function(e) {
        if (e.target.closest('[data-refresh]')) {
            location.reload();
        }
    });

    // Theme toggle (if implemented)
    const themeToggle = document.getElementById('themeToggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', function() {
            document.body.classList.toggle('dark-mode');
            localStorage.setItem('darkMode', document.body.classList.contains('dark-mode'));
        });

        // Load saved theme
        if (localStorage.getItem('darkMode') === 'true') {
            document.body.classList.add('dark-mode');
        }
    }

    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
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
                const modal = bootstrap.Modal.getInstance(openModal);
                if (modal) modal.hide();
            }
        }
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
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

    // Lazy load images
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

    // Export functions for global use
    window.BibekSchool = {
        showAlert: function(message, type, container) {
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
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                bsAlert.close();
            }, 5000);
        },

        showLoading: function(element) {
            if (element) {
                element.classList.add('loading');
                element.disabled = true;
            }
        },

        hideLoading: function(element) {
            if (element) {
                element.classList.remove('loading');
                element.disabled = false;
            }
        },

        confirmAction: function(message, callback) {
            if (confirm(message)) {
                callback();
            }
        },

        formatDate: function(date) {
            return new Date(date).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            });
        },

        formatDateTime: function(date) {
            return new Date(date).toLocaleString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        },

        formatNumber: function(num, decimals = 2) {
            return Number(num).toFixed(decimals);
        },

        debounce: function(func, wait) {
            let timeout;
            return function(...args) {
                clearTimeout(timeout);
                timeout = setTimeout(() => func.apply(this, args), wait);
            };
        },

        throttle: function(func, limit) {
            let inThrottle;
            return function(...args) {
                if (!inThrottle) {
                    func.apply(this, args);
                    inThrottle = true;
                    setTimeout(() => inThrottle = false, limit);
                }
            };
        }
    };

})();