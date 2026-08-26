/* ==========================================================================
   DASHBOARD.JS — Dashboard widgets, grade calculation, ForgotPassword flow
   Used by: Teacher/CreateMark.cshtml, Teacher/EditMark.cshtml, Account/ForgotPassword.cshtml
   Functions: calculateGrade, initGradeCalculator, initForgotPasswordFlow
   ========================================================================== */

(function () {
    'use strict';

    // ========================================================================
    // GRADE CALCULATOR (CreateMark / EditMark)
    // ========================================================================

    /**
     * Calculate and display grade/percentage based on obtained, full, and pass marks
     * @param {HTMLInputElement} obtainedInput - Obtained marks input
     * @param {HTMLInputElement} fullInput - Full marks input
     * @param {HTMLInputElement} passInput - Pass marks input
     * @param {HTMLElement} gradeDisplay - Element to show grade badge
     * @param {HTMLElement} percentageDisplay - Element to show percentage badge
     */
    function calculateGrade(obtainedInput, fullInput, passInput, gradeDisplay, percentageDisplay) {
        const obtained = parseFloat(obtainedInput?.value) || 0;
        const full = parseFloat(fullInput?.value) || 100;
        const pass = parseFloat(passInput?.value) || 40;

        if (full > 0) {
            const percentage = (obtained / full) * 100;
            let grade;

            if (obtained < pass) grade = 'F';
            else if (percentage >= 90) grade = 'A+';
            else if (percentage >= 80) grade = 'A';
            else if (percentage >= 70) grade = 'B+';
            else if (percentage >= 60) grade = 'B';
            else if (percentage >= 50) grade = 'C+';
            else if (percentage >= 40) grade = 'C';
            else grade = 'D';

            if (gradeDisplay) {
                gradeDisplay.textContent = grade;
                gradeDisplay.className = 'badge fs-6 ' + (obtained >= pass ? 'bg-success' : 'bg-danger');
            }
            if (percentageDisplay) {
                percentageDisplay.textContent = percentage.toFixed(2) + '%';
            }
        }
    }

    /**
     * Initialize grade calculator for CreateMark/EditMark forms
     * Call this on DOMContentLoaded
     */
    function initGradeCalculator() {
        const subjectSelect = document.querySelector('[name="SubjectId"]');
        const fullMarksInput = document.querySelector('[name="FullMarks"]');
        const passMarksInput = document.querySelector('[name="PassMarks"]');
        const obtainedMarksInput = document.getElementById('obtainedMarksInput');
        const gradeDisplay = document.getElementById('gradeDisplay');
        const percentageDisplay = document.getElementById('percentageDisplay');

        if (!obtainedMarksInput || !gradeDisplay || !percentageDisplay) return;

        function recalc() {
            calculateGrade(obtainedMarksInput, fullMarksInput, passMarksInput, gradeDisplay, percentageDisplay);
        }

        // Load subject details when subject changes
        if (subjectSelect) {
            subjectSelect.addEventListener('change', function () {
                const selectedOption = this.options[this.selectedIndex];
                const fullMarks = selectedOption.getAttribute('data-full-marks');
                const passMarks = selectedOption.getAttribute('data-pass-marks');

                if (fullMarks) fullMarksInput.value = fullMarks;
                if (passMarks) passMarksInput.value = passMarks;
                recalc();
            });
        }

        // Recalculate on input changes
        if (obtainedMarksInput) obtainedMarksInput.addEventListener('input', recalc);
        if (fullMarksInput) fullMarksInput.addEventListener('input', recalc);
        if (passMarksInput) passMarksInput.addEventListener('input', recalc);

        // Initial calculation
        recalc();

        // Trigger subject change if already selected (for pre-filled forms)
        if (subjectSelect && subjectSelect.value) {
            subjectSelect.dispatchEvent(new Event('change'));
        }
    }

    // ========================================================================
    // FORGOT PASSWORD FLOW (Account/ForgotPassword)
    // ========================================================================

    /**
     * Initialize the multi-step Forgot Password flow
     * Handles: email entry -> OTP verification -> new password
     */
    function initForgotPasswordFlow() {
        const form = document.getElementById('forgotPasswordForm');
        if (!form) return;

        // Form state
        const steps = {
            1: document.getElementById('step1'),
            2: document.getElementById('step2'),
            3: document.getElementById('step3')
        };
        const stepDescription = document.getElementById('stepDescription');
        let resendTimer = null;
        let resendCooldown = 60;
        let currentStep = 1;

        // DOM elements
        const emailInput = document.getElementById('Email');
        const otpInputs = [
            document.getElementById('otp1'),
            document.getElementById('otp2'),
            document.getElementById('otp3'),
            document.getElementById('otp4'),
            document.getElementById('otp5'),
            document.getElementById('otp6')
        ];
        const otpCodeHidden = document.getElementById('otpCode');
        const otpEmailDisplay = document.getElementById('otpEmail');
        const otpError = document.getElementById('otpError');
        const resendBtn = document.getElementById('btnResendCode');
        const resendCountdown = document.getElementById('resendCountdown');
        const newPasswordInput = document.getElementById('NewPassword');
        const confirmPasswordInput = document.getElementById('ConfirmPassword');
        const passwordStrength = document.getElementById('passwordStrength');
        const passwordStrengthBar = passwordStrength?.querySelector('.progress-bar');
        const passwordStrengthText = document.getElementById('passwordStrengthText');

        // Button elements
        const btnSendCode = document.getElementById('btnSendCode');
        const btnVerifyCode = document.getElementById('btnVerifyCode');
        const btnResetPassword = document.getElementById('btnResetPassword');

        // Utility functions
        function isValidEmail(email) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(email);
        }

        function clearValidationErrors() {
            form.querySelectorAll('.text-danger').forEach(el => el.textContent = '');
            form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
            form.querySelectorAll('.is-valid').forEach(el => el.classList.remove('is-valid'));
        }

        function showAlert(message, type = 'info', container = null) {
            const alertContainer = container || form;
            const existingAlert = alertContainer.querySelector('.alert');
            if (existingAlert) existingAlert.remove();

            const alert = document.createElement('div');
            alert.className = `alert alert-${type} alert-dismissible fade show`;
            alert.role = 'alert';
            alert.innerHTML = `
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;
            alertContainer.insertBefore(alert, alertContainer.firstChild);

            setTimeout(() => {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                if (bsAlert) bsAlert.close();
            }, 5000);
        }

        function setButtonLoading(button, isLoading) {
            if (!button) return;
            const btnText = button.querySelector('.btn-text');
            const loadingText = button.getAttribute('data-loading-text');

            if (isLoading) {
                button.disabled = true;
                if (btnText) btnText.textContent = loadingText;
                button.classList.add('disabled');
            } else {
                button.disabled = false;
                if (btnText) btnText.textContent = button.getAttribute('data-original-text') || btnText.textContent;
                button.classList.remove('disabled');
            }
        }

        function storeOriginalButtonText() {
            [btnSendCode, btnVerifyCode, btnResetPassword].forEach(btn => {
                if (btn) {
                    const btnText = btn.querySelector('.btn-text');
                    if (btnText) {
                        btn.setAttribute('data-original-text', btnText.textContent);
                    }
                }
            });
        }

        /**
         * Display server-side validation errors on the form.
         * Accepts either:
         *   1) an object keyed by field name, e.g. { Email: ["msg1", "msg2"], OtpCode: ["msg"] }
         *   2) a plain array of strings, e.g. ["msg1", "msg2"]
         * This was the missing function causing "displayValidationErrors is not defined".
         */
        function displayValidationErrors(errors) {
            if (!errors) return;

            if (Array.isArray(errors)) {
                showAlert(errors.join('<br>'), 'danger');
                return;
            }

            Object.keys(errors).forEach(field => {
                const messages = Array.isArray(errors[field]) ? errors[field] : [errors[field]];
                const message = messages.join(' ');

                // Try known error span patterns: asp-validation-for spans, or manual *Error ids
                const errorSpan = form.querySelector(`span[data-valmsg-for="${field}"]`)
                    || document.getElementById(`${field.charAt(0).toLowerCase()}${field.slice(1)}Error`);

                if (errorSpan) {
                    errorSpan.textContent = message;
                }

                const inputEl = document.getElementById(field) || form.querySelector(`[name="${field}"]`);
                if (inputEl) {
                    inputEl.classList.add('is-invalid');
                }
            });
        }

        function showStep(stepNumber) {
            Object.values(steps).forEach(step => step.classList.add('d-none'));
            if (steps[stepNumber]) {
                steps[stepNumber].classList.remove('d-none');
                currentStep = stepNumber;
            }

            const descriptions = {
                1: 'Enter your registered email to receive a verification code',
                2: 'Enter the 6-digit code sent to your email',
                3: 'Create your new password'
            };
            if (stepDescription) {
                stepDescription.textContent = descriptions[stepNumber] || '';
            }

            setTimeout(() => {
                if (stepNumber === 1) emailInput?.focus();
                else if (stepNumber === 2) otpInputs[0]?.focus();
                else if (stepNumber === 3) newPasswordInput?.focus();
            }, 100);

            // Initialize password toggles for dynamically shown step 3
            if (stepNumber === 3 && window.BibekSchool?.validation?.initPasswordToggle) {
                window.BibekSchool.validation.initPasswordToggle();
            }
        }

        function getOtpCode() {
            return otpInputs.map(input => input.value).join('');
        }

        function updateOtpHidden() {
            otpCodeHidden.value = getOtpCode();
        }

        // OTP Input handling
        otpInputs.forEach((input, index) => {
            if (!input) return;
            input.addEventListener('input', function (e) {
                this.value = this.value.replace(/[^0-9]/g, '');
                updateOtpHidden();
                if (otpError) otpError.textContent = '';

                if (this.value && index < otpInputs.length - 1) {
                    otpInputs[index + 1]?.focus();
                }

                if (getOtpCode().length === 6) {
                    otpInputs.forEach(inp => inp?.classList.add('is-valid'));
                } else {
                    otpInputs.forEach(inp => inp?.classList.remove('is-valid'));
                }
            });

            input.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !this.value && index > 0) {
                    otpInputs[index - 1]?.focus();
                }
                if (e.key === 'ArrowLeft' && index > 0) {
                    otpInputs[index - 1]?.focus();
                }
                if (e.key === 'ArrowRight' && index < otpInputs.length - 1) {
                    otpInputs[index + 1]?.focus();
                }
            });

            input.addEventListener('paste', function (e) {
                e.preventDefault();
                const pastedData = e.clipboardData.getData('text').replace(/[^0-9]/g, '').slice(0, 6);
                pastedData.split('').forEach((char, i) => {
                    if (i < otpInputs.length) {
                        otpInputs[i].value = char;
                    }
                });
                updateOtpHidden();
                const lastFilled = Math.min(pastedData.length, otpInputs.length) - 1;
                if (lastFilled >= 0) otpInputs[lastFilled]?.focus();
            });
        });

        // Password strength indicator
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

        if (newPasswordInput && passwordStrength && passwordStrengthBar && passwordStrengthText) {
            newPasswordInput.addEventListener('input', function () {
                const password = this.value;
                if (password.length > 0) {
                    passwordStrength.style.display = 'block';
                    const { strength, feedback } = calculatePasswordStrength(password);
                    const percentage = (strength / 5) * 100;

                    let colorClass = 'bg-danger';
                    if (strength >= 4) colorClass = 'bg-success';
                    else if (strength >= 3) colorClass = 'bg-warning';
                    else if (strength >= 2) colorClass = 'bg-info';

                    passwordStrengthBar.className = `progress-bar ${colorClass}`;
                    passwordStrengthBar.style.width = `${percentage}%`;
                    passwordStrengthBar.setAttribute('aria-valuenow', percentage);

                    passwordStrengthText.textContent = feedback.length
                        ? 'Missing: ' + feedback.join(', ')
                        : 'Strong password!';
                } else {
                    passwordStrength.style.display = 'none';
                }
            });
        }

        // Resend cooldown timer
        function startResendCooldown(seconds) {
            resendCooldown = seconds;
            if (resendBtn) resendBtn.disabled = true;
            if (resendCountdown) resendCountdown.textContent = resendCooldown;

            if (resendTimer) clearInterval(resendTimer);

            resendTimer = setInterval(() => {
                resendCooldown--;
                if (resendCountdown) resendCountdown.textContent = resendCooldown;

                if (resendCooldown <= 0) {
                    clearInterval(resendTimer);
                    if (resendBtn) resendBtn.disabled = false;
                    if (resendCountdown) resendCountdown.textContent = 60;
                }
            }, 1000);
        }

        // API calls
        async function apiCall(url, data, button = null) {
            if (button) setButtonLoading(button, true);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (!token) {
                console.error('Anti-forgery token not found in page');
            }

            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token || ''
                    },
                    body: JSON.stringify(data)
                });

                const contentType = response.headers.get('content-type') || '';

                if (!contentType.includes('application/json')) {
                    const text = await response.text();
                    console.error('Non-JSON response from server:', text);
                    throw new Error(
                        response.status === 500
                            ? 'Server error occurred. Please check server logs.'
                            : `Unexpected response from server (status ${response.status}).`
                    );
                }

                const result = await response.json();

                if (!response.ok) {
                    throw new Error(result.message || 'Request failed');
                }

                return result;
            } catch (error) {
                throw error;
            } finally {
                if (button) setButtonLoading(button, false);
            }
        }

        // Event handlers
        btnSendCode?.addEventListener('click', async () => {
            clearValidationErrors();

            const email = emailInput?.value?.trim();
            if (!email) {
                showAlert('Please enter your email address.', 'warning');
                emailInput?.focus();
                emailInput?.classList.add('is-invalid');
                const emailError = form.querySelector('#step1 span[data-valmsg-for="Email"]');
                if (emailError) emailError.textContent = 'Email is required';
                return;
            }

            if (!isValidEmail(email)) {
                showAlert('Please enter a valid email address.', 'warning');
                emailInput?.focus();
                emailInput?.classList.add('is-invalid');
                const emailError = form.querySelector('#step1 span[data-valmsg-for="Email"]');
                if (emailError) emailError.textContent = 'Invalid email address';
                return;
            }

            try {
                const result = await apiCall('/Account/ForgotPassword', { Email: email }, btnSendCode);

                if (result.success) {
                    showAlert(result.message, 'success');
                    if (otpEmailDisplay) otpEmailDisplay.textContent = email;
                    startResendCooldown(result.resendCooldown || 60);
                    showStep(2);
                } else {
                    showAlert(result.message, 'danger');
                    if (result.errors) displayValidationErrors(result.errors);
                    if (result.resendCooldown) startResendCooldown(result.resendCooldown);
                }
            } catch (error) {
                showAlert(error.message || 'Failed to send verification code. Please try again.', 'danger');
            }
        });

        btnVerifyCode?.addEventListener('click', async () => {
            clearValidationErrors();

            const otpCode = getOtpCode();
            if (otpCode.length !== 6) {
                if (otpError) otpError.textContent = 'Please enter the complete 6-digit code.';
                otpInputs.forEach(inp => inp?.classList.add('is-invalid'));
                otpInputs[0]?.focus();
                return;
            }

            const email = emailInput?.value?.trim();

            try {
                const result = await apiCall('/Account/VerifyOtp', { Email: email, OtpCode: otpCode }, btnVerifyCode);

                if (result.success) {
                    showAlert(result.message, 'success');
                    showStep(3);
                } else {
                    showAlert(result.message, 'danger');
                    if (result.errors) displayValidationErrors(result.errors);
                    if (otpError) otpError.textContent = result.message;
                    otpInputs.forEach(inp => inp?.classList.add('is-invalid'));
                    setTimeout(() => otpInputs.forEach(inp => inp?.classList.remove('is-invalid')), 3000);
                }
            } catch (error) {
                showAlert(error.message || 'Failed to verify code. Please try again.', 'danger');
            }
        });

        btnResetPassword?.addEventListener('click', async () => {
            const newPassword = newPasswordInput?.value;
            const confirmPassword = confirmPasswordInput?.value;
            const otpCode = otpCodeHidden?.value;
            const email = emailInput?.value?.trim();

            if (!newPassword) {
                showAlert('Please enter a new password.', 'warning');
                newPasswordInput?.focus();
                return;
            }

            if (newPassword.length < 6) {
                showAlert('Password must be at least 6 characters.', 'warning');
                newPasswordInput?.focus();
                return;
            }

            if (newPassword !== confirmPassword) {
                showAlert('Passwords do not match.', 'warning');
                confirmPasswordInput?.focus();
                return;
            }

            if (!otpCode || otpCode.length !== 6) {
                showAlert('Session expired. Please start over.', 'danger');
                showStep(1);
                return;
            }

            try {
                const formData = new FormData();
                formData.append('Email', email);
                formData.append('OtpCode', otpCode);
                formData.append('NewPassword', newPassword);
                formData.append('ConfirmPassword', confirmPassword);
                formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]')?.value || '');

                setButtonLoading(btnResetPassword, true);

                const response = await fetch('/Account/ResetPassword', {
                    method: 'POST',
                    body: formData
                });

                if (response.redirected) {
                    window.location.href = response.url;
                    return;
                }

                const contentType = response.headers.get('content-type') || '';
                if (!contentType.includes('application/json')) {
                    const text = await response.text();
                    console.error('Non-JSON response from server:', text);
                    throw new Error(
                        response.status === 500
                            ? 'Server error occurred. Please check server logs.'
                            : `Unexpected response from server (status ${response.status}).`
                    );
                }

                const result = await response.json();
                if (result.success) {
                    showAlert(result.message, 'success');
                    // Use redirect URL from response or fallback to login page
                    const redirectUrl = result.redirectUrl || '/Account/Login';
                    setTimeout(() => window.location.href = redirectUrl, 1500);
                } else {
                    showAlert(result.message || 'Failed to reset password.', 'danger');
                }
            } catch (error) {
                showAlert(error.message || 'Failed to reset password. Please try again.', 'danger');
            } finally {
                setButtonLoading(btnResetPassword, false);
            }
        });

        resendBtn?.addEventListener('click', async () => {
            if (resendBtn.disabled) return;

            clearValidationErrors();

            const email = emailInput?.value?.trim();
            if (!email) {
                showAlert('Please enter your email address.', 'warning');
                emailInput?.focus();
                emailInput?.classList.add('is-invalid');
                const emailError = form.querySelector('#step1 span[data-valmsg-for="Email"]');
                if (emailError) emailError.textContent = 'Email is required';
                return;
            }

            if (!isValidEmail(email)) {
                showAlert('Please enter a valid email address.', 'warning');
                emailInput?.focus();
                emailInput?.classList.add('is-invalid');
                const emailError = form.querySelector('#step1 span[data-valmsg-for="Email"]');
                if (emailError) emailError.textContent = 'Invalid email address';
                return;
            }

            try {
                const result = await apiCall('/Account/ResendOtp', { Email: email });

                if (result.success) {
                    showAlert(result.message, 'success');
                    startResendCooldown(result.cooldownSeconds || 60);
                    otpInputs.forEach(inp => inp.value = '');
                    updateOtpHidden();
                    otpInputs[0]?.focus();
                } else {
                    showAlert(result.message, 'danger');
                    if (result.errors) displayValidationErrors(result.errors);
                    if (result.cooldownSeconds) startResendCooldown(result.cooldownSeconds);
                }
            } catch (error) {
                showAlert(error.message || 'Failed to resend code. Please try again.', 'danger');
            }
        });

        // Allow Enter key to submit
        emailInput?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                btnSendCode?.click();
            }
        });

        otpInputs.forEach(input => {
            input?.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && getOtpCode().length === 6) {
                    e.preventDefault();
                    btnVerifyCode?.click();
                }
            });
        });

        // Initialize
        storeOriginalButtonText();
        showStep(1);
    }

    // Auto-initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initGradeCalculator();
            initForgotPasswordFlow();
        });
    } else {
        initGradeCalculator();
        initForgotPasswordFlow();
    }

    // Expose globally
    window.BibekSchool = window.BibekSchool || {};
    window.BibekSchool.dashboard = {
        calculateGrade,
        initGradeCalculator,
        initForgotPasswordFlow
    };
})();