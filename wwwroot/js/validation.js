/* ==========================================================================
   VALIDATION.JS — Custom client-side validation (password strength, etc.)
   Used by: Profile/ChangePassword.cshtml, Account/Register.cshtml
   Functions: initPasswordStrength, initPasswordToggle
   ========================================================================== */

(function () {
    'use strict';

    /**
     * Calculate password strength based on common criteria
     * @param {string} password - The password to check
     * @returns {{strength: number, feedback: string[]}} Strength score (0-5) and missing criteria
     */
    function calculatePasswordStrength(password) {
        let strength = 0;
        let feedback = [];

        if (password.length >= 8) strength++;
        else feedback.push('At least 8 characters');

        if (password.match(/[a-z]+/)) strength++;
        else feedback.push('Lowercase letter');

        if (password.match(/[A-Z]+/)) strength++;
        else feedback.push('Uppercase letter');

        if (password.match(/[0-9]+/)) strength++;
        else feedback.push('Number');

        if (password.match(/[^a-zA-Z0-9]+/)) strength++;
        else feedback.push('Special character');

        return { strength, feedback };
    }

    /**
     * Initialize password strength indicator for an input
     * @param {HTMLInputElement} passwordInput - The password input element
     * @param {HTMLElement} container - Container element to render strength UI
     */
    function initPasswordStrength(passwordInput, container) {
        if (!passwordInput || !container) return;

        passwordInput.addEventListener('input', function () {
            const password = this.value;
            if (password.length > 0) {
                const { strength, feedback } = calculatePasswordStrength(password);
                const percentage = (strength / 5) * 100;

                let colorClass = 'bg-danger';
                if (strength >= 4) colorClass = 'bg-success';
                else if (strength >= 3) colorClass = 'bg-warning';
                else if (strength >= 2) colorClass = 'bg-info';

                container.innerHTML = `
                    <div class="progress" style="height: 6px;">
                        <div class="progress-bar ${colorClass}" role="progressbar" style="width: ${percentage}%"></div>
                    </div>
                    <small class="text-muted">${feedback.length ? 'Missing: ' + feedback.join(', ') : 'Strong password!'}</small>
                `;
            } else {
                container.innerHTML = '';
            }
        });
    }

    /**
     * Initialize password visibility toggle buttons
     * Finds all .toggle-password buttons and wires them up
     * Handles dynamically added elements via MutationObserver
     */
    function initPasswordToggle() {
        const toggleButtons = document.querySelectorAll('.toggle-password:not([data-bound="true"])');
        toggleButtons.forEach(btn => {
            btn.dataset.bound = 'true';

            btn.addEventListener('click', function () {
                const targetId = this.getAttribute('data-target');
                const input = document.getElementById(targetId);
                const icon = this.querySelector('i');

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
        });
    }

    /**
     * Initialize MutationObserver to handle dynamically added password toggles
     */
    function initPasswordToggleObserver() {
        if (window.__passwordToggleObserver) return;

        window.__passwordToggleObserver = new MutationObserver((mutations) => {
            let shouldCheck = false;
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === 1) { // Element node
                        if (node.matches && node.matches('.toggle-password')) {
                            shouldCheck = true;
                        } else if (node.querySelector && node.querySelector('.toggle-password')) {
                            shouldCheck = true;
                        }
                    }
                });
            });
            if (shouldCheck) {
                initPasswordToggle();
            }
        });

        window.__passwordToggleObserver.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    /**
     * Initialize all validation enhancements
     * Call this on DOMContentLoaded
     */
    function initValidation() {
        initPasswordToggle();
        initPasswordToggleObserver();

        // Password strength for any input with data-strength attribute
        document.querySelectorAll('input[type="password"][data-strength]').forEach(input => {
            const targetId = input.getAttribute('data-strength-target');
            const container = targetId ? document.getElementById(targetId) : input.parentNode.querySelector('[id$="Strength"], [id$="strength"], .password-strength');
            if (container) {
                initPasswordStrength(input, container);
            }
        });

        // Specific: ChangePassword page
        const newPasswordInput = document.getElementById('NewPassword');
        const strengthContainer = document.getElementById('passwordStrength');
        if (newPasswordInput && strengthContainer) {
            initPasswordStrength(newPasswordInput, strengthContainer);
        }
    }

    // Auto-initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initValidation);
    } else {
        initValidation();
    }

    // Expose globally
    window.BibekSchool = window.BibekSchool || {};
    window.BibekSchool.validation = {
        calculatePasswordStrength,
        initPasswordStrength,
        initPasswordToggle,
        initPasswordToggleObserver,
        init: initValidation
    };
})();