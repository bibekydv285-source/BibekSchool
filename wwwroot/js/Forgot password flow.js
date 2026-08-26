/* ==========================================================================
   FORGOT-PASSWORD-FLOW.JS
   Drives the 3-step Forgot Password view: email -> OTP -> new password.
   Depends on: ForgotPassword.cshtml element IDs, validation.js (password
   toggle + strength, already auto-initialized), antiforgery token in form.

   Endpoints used (from AccountController):
     POST /Account/ForgotPassword  -> ForgotPasswordResponse
     POST /Account/ResendOtp       -> ResendOtpResponse
     POST /Account/VerifyOtp       -> ForgotPasswordResponse
     POST /Account/ResetPassword   -> ForgotPasswordResponse (ajax) or redirect
   ========================================================================== */

(function () {
    'use strict';

    function initForgotPasswordFlow() {
        var form = document.getElementById('forgotPasswordForm');
        if (!form) return; // not on this page

        // ------------------------------------------------------------
        // Element refs
        // ------------------------------------------------------------
        var step1 = document.getElementById('step1');
        var step2 = document.getElementById('step2');
        var step3 = document.getElementById('step3');
        var stepDescription = document.getElementById('stepDescription');

        var emailInput = document.getElementById('Email');
        var emailError = document.querySelector('span[data-valmsg-for="Email"]') ||
            form.querySelector('.text-danger[asp-validation-for="Email"]') ||
            form.querySelector('span.text-danger'); // asp-validation-for renders as span with data-valmsg-for

        var btnSendCode = document.getElementById('btnSendCode');
        var btnVerifyCode = document.getElementById('btnVerifyCode');
        var btnResendCode = document.getElementById('btnResendCode');
        var btnResetPassword = document.getElementById('btnResetPassword');

        var otpDigits = [1, 2, 3, 4, 5, 6].map(function (n) { return document.getElementById('otp' + n); });
        var otpCodeHidden = document.getElementById('otpCode');
        var otpError = document.getElementById('otpError');
        var otpEmailLabel = document.getElementById('otpEmail');
        var resendCountdownEl = document.getElementById('resendCountdown');

        var newPasswordInput = document.getElementById('NewPassword');
        var confirmPasswordInput = document.getElementById('ConfirmPassword');
        var newPasswordError = document.getElementById('newPasswordError');
        var confirmPasswordError = document.getElementById('confirmPasswordError');

        var verifiedEmail = '';
        var resendIntervalId = null;

        // ------------------------------------------------------------
        // Generic alert (top of form) — created on demand
        // ------------------------------------------------------------
        function getOrCreateAlertBox() {
            var box = form.querySelector('.form-alert');
            if (!box) {
                box = document.createElement('div');
                box.className = 'alert alert-danger form-alert';
                form.prepend(box);
            }
            return box;
        }

        function showFormAlert(message) {
            var box = getOrCreateAlertBox();
            box.textContent = message;
            box.classList.remove('d-none');
            box.classList.remove('alert-success');
            box.classList.add('alert-danger');
        }

        function showFormSuccess(message) {
            var box = getOrCreateAlertBox();
            box.textContent = message;
            box.classList.remove('d-none');
            box.classList.remove('alert-danger');
            box.classList.add('alert-success');
        }

        function hideFormAlert() {
            var box = form.querySelector('.form-alert');
            if (box) box.classList.add('d-none');
        }

        /**
         * Displays validation errors returned by the server.
         * Accepts either:
         *   - a Dictionary<string,string[]> (matches ForgotPasswordResponse.Errors)
         *   - null/undefined, in which case `message` is shown instead
         * Also populates the per-field <span> elements when a matching one exists.
         */
        function displayValidationErrors(errors, message) {
            // Clear old per-field messages
            [emailError, otpError, newPasswordError, confirmPasswordError].forEach(function (el) {
                if (el) el.textContent = '';
            });

            var combined = [];

            if (errors && typeof errors === 'object') {
                Object.keys(errors).forEach(function (key) {
                    var msgs = errors[key];
                    if (!msgs) return;
                    if (!Array.isArray(msgs)) msgs = [msgs];
                    combined = combined.concat(msgs);

                    var text = msgs.join(' ');
                    if (key === 'Email' && emailError) emailError.textContent = text;
                    else if (key === 'OtpCode' && otpError) otpError.textContent = text;
                    else if (key === 'NewPassword' && newPasswordError) newPasswordError.textContent = text;
                    else if (key === 'ConfirmPassword' && confirmPasswordError) confirmPasswordError.textContent = text;
                });
            }

            var finalMessage = combined.length ? combined.join(' ') : (message || 'Something went wrong. Please try again.');
            showFormAlert(finalMessage);
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------
        function getAntiForgeryToken() {
            var input = form.querySelector('input[name="__RequestVerificationToken"]');
            return input ? input.value : '';
        }

        function setButtonLoading(button, isLoading) {
            if (!button) return;
            var textEl = button.querySelector('.btn-text');
            if (isLoading) {
                button.disabled = true;
                button.dataset.originalText = textEl ? textEl.textContent : '';
                if (textEl) textEl.textContent = button.getAttribute('data-loading-text') || 'Loading...';
            } else {
                button.disabled = false;
                if (textEl && button.dataset.originalText) textEl.textContent = button.dataset.originalText;
            }
        }

        function goToStep(stepEl, description) {
            [step1, step2, step3].forEach(function (s) {
                if (s) s.classList.add('d-none');
            });
            if (stepEl) stepEl.classList.remove('d-none');
            if (description && stepDescription) stepDescription.textContent = description;
            hideFormAlert();
        }

        async function postForm(url, dataObj) {
            var formData = new FormData();
            Object.keys(dataObj).forEach(function (key) {
                formData.append(key, dataObj[key]);
            });
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            var response = await fetch(url, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });

            return response.json();
        }

        // ------------------------------------------------------------
        // OTP digit box behavior: auto-advance, backspace, paste
        // ------------------------------------------------------------
        function syncOtpHidden() {
            otpCodeHidden.value = otpDigits.map(function (d) { return d.value; }).join('');
        }

        otpDigits.forEach(function (digit, idx) {
            digit.addEventListener('input', function () {
                this.value = this.value.replace(/\D/g, '').slice(0, 1);
                if (this.value && idx < otpDigits.length - 1) {
                    otpDigits[idx + 1].focus();
                }
                syncOtpHidden();
            });

            digit.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !this.value && idx > 0) {
                    otpDigits[idx - 1].focus();
                }
            });

            digit.addEventListener('paste', function (e) {
                e.preventDefault();
                var pasted = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '').slice(0, 6);
                pasted.split('').forEach(function (char, i) {
                    if (otpDigits[i]) otpDigits[i].value = char;
                });
                syncOtpHidden();
                var nextEmpty = otpDigits.find(function (d) { return !d.value; });
                (nextEmpty || otpDigits[otpDigits.length - 1]).focus();
            });
        });

        // ------------------------------------------------------------
        // Resend cooldown
        // ------------------------------------------------------------
        function startResendCooldown(seconds) {
            clearInterval(resendIntervalId);
            var remaining = seconds || 60;
            btnResendCode.disabled = true;
            resendCountdownEl.textContent = remaining;
            btnResendCode.innerHTML = 'Resend code (<span id="resendCountdown">' + remaining + '</span>s)';

            resendIntervalId = setInterval(function () {
                remaining--;
                var el = document.getElementById('resendCountdown');
                if (el) el.textContent = remaining;
                if (remaining <= 0) {
                    clearInterval(resendIntervalId);
                    btnResendCode.disabled = false;
                    btnResendCode.textContent = 'Resend code';
                }
            }, 1000);
        }

        // ------------------------------------------------------------
        // STEP 1 -> STEP 2: send code
        // ------------------------------------------------------------
        btnSendCode.addEventListener('click', async function () {
            hideFormAlert();
            var email = emailInput.value.trim();

            if (!email) {
                displayValidationErrors({ Email: ['Email is required'] });
                return;
            }

            setButtonLoading(btnSendCode, true);
            try {
                var data = await postForm('/Account/ForgotPassword', { Email: email });

                if (!data.success) {
                    displayValidationErrors(data.errors, data.message);
                    return;
                }

                verifiedEmail = data.email || email;
                otpEmailLabel.textContent = verifiedEmail;
                otpDigits.forEach(function (d) { d.value = ''; });
                syncOtpHidden();

                goToStep(step2, 'Enter the 6-digit code sent to your email');
                startResendCooldown(data.resendCooldown || 60);
                otpDigits[0].focus();

            } catch (err) {
                console.error('ForgotPassword request failed:', err);
                showFormAlert('Network error. Please check your connection and try again.');
            } finally {
                setButtonLoading(btnSendCode, false);
            }
        });

        // ------------------------------------------------------------
        // Resend code
        // ------------------------------------------------------------
        btnResendCode.addEventListener('click', async function () {
            hideFormAlert();
            try {
                var data = await postForm('/Account/ResendOtp', { Email: verifiedEmail });

                if (!data.success) {
                    showFormAlert(data.message || 'Could not resend code.');
                    startResendCooldown(data.cooldownSeconds || 60);
                    return;
                }

                showFormSuccess(data.message || 'New verification code sent.');
                startResendCooldown(data.cooldownSeconds || 60);

            } catch (err) {
                console.error('ResendOtp request failed:', err);
                showFormAlert('Network error. Please check your connection and try again.');
                btnResendCode.disabled = false;
            }
        });

        // ------------------------------------------------------------
        // STEP 2 -> STEP 3: verify code
        // ------------------------------------------------------------
        btnVerifyCode.addEventListener('click', async function () {
            hideFormAlert();
            syncOtpHidden();
            var otpCode = otpCodeHidden.value;

            if (!/^\d{6}$/.test(otpCode)) {
                displayValidationErrors({ OtpCode: ['Please enter the full 6-digit code'] });
                return;
            }

            setButtonLoading(btnVerifyCode, true);
            try {
                var data = await postForm('/Account/VerifyOtp', { Email: verifiedEmail, OtpCode: otpCode });

                if (!data.success) {
                    displayValidationErrors(data.errors, data.message);
                    return;
                }

                goToStep(step3, 'Create a new password for your account');

            } catch (err) {
                console.error('VerifyOtp request failed:', err);
                showFormAlert('Network error. Please check your connection and try again.');
            } finally {
                setButtonLoading(btnVerifyCode, false);
            }
        });

        // ------------------------------------------------------------
        // STEP 3: reset password
        // ------------------------------------------------------------
        btnResetPassword.addEventListener('click', async function () {
            hideFormAlert();
            var newPassword = newPasswordInput.value;
            var confirmPassword = confirmPasswordInput.value;

            var fieldErrors = {};
            if (!newPassword || newPassword.length < 6) {
                fieldErrors.NewPassword = ['Password must be at least 6 characters'];
            }
            if (newPassword !== confirmPassword) {
                fieldErrors.ConfirmPassword = ['Passwords do not match'];
            }
            if (Object.keys(fieldErrors).length) {
                displayValidationErrors(fieldErrors);
                return;
            }

            setButtonLoading(btnResetPassword, true);
            try {
                var data = await postForm('/Account/ResetPassword', {
                    Email: verifiedEmail,
                    OtpCode: otpCodeHidden.value,
                    NewPassword: newPassword,
                    ConfirmPassword: confirmPassword
                });

                if (!data.success) {
                    displayValidationErrors(data.errors, data.message);
                    return;
                }

                showFormSuccess(data.message || 'Password reset successfully. Redirecting to login...');
                setTimeout(function () {
                    window.location.href = '/Account/Login';
                }, 1500);

            } catch (err) {
                console.error('ResetPassword request failed:', err);
                showFormAlert('Network error. Please check your connection and try again.');
            } finally {
                setButtonLoading(btnResetPassword, false);
            }
        });
    }

    // Auto-init on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initForgotPasswordFlow);
    } else {
        initForgotPasswordFlow();
    }

    // Expose for manual re-init if needed (e.g. if loaded after partial render)
    window.BibekSchool = window.BibekSchool || {};
    window.BibekSchool.initForgotPasswordFlow = initForgotPasswordFlow;
})();