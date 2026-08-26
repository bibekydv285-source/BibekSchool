/* ==========================================================================
   NOTIFICATIONS.JS — Notification-related functionality
   Used by: Teacher/Notifications.cshtml, Student/Notifications.cshtml, Admin/Notifications.cshtml
   Functions: markAllRead, getNotificationIcon, getNotificationIconClass, markAsRead
   ========================================================================== */

(function () {
    'use strict';

    /**
     * Get the Font Awesome icon class for a notification type
     * @param {string} type - Notification type (Success, Warning, Error, Info)
     * @returns {string} Font Awesome icon class
     */
    function getNotificationIcon(type) {
        switch (type) {
            case 'Success':
                return 'fas fa-check-circle';
            case 'Warning':
                return 'fas fa-exclamation-triangle';
            case 'Error':
                return 'fas fa-times-circle';
            case 'Info':
                return 'fas fa-info-circle';
            default:
                return 'fas fa-bell';
        }
    }

    /**
     * Get the background color class for a notification icon
     * @param {string} type - Notification type (Success, Warning, Error, Info)
     * @returns {string} Bootstrap background color class
     */
    function getNotificationIconClass(type) {
        switch (type) {
            case 'Success':
                return 'bg-success';
            case 'Warning':
                return 'bg-warning';
            case 'Error':
                return 'bg-danger';
            case 'Info':
                return 'bg-info';
            default:
                return 'bg-primary';
        }
    }

    /**
     * Mark all notifications as read via AJAX
     * Reloads the page on success
     */
    function markAllRead() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) return;

        fetch('/Notification/MarkAllAsRead', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/json'
            }
        }).then(() => location.reload());
    }

    /**
     * Mark a single notification as read via AJAX
     * @param {string|number} notificationId - The notification ID
     */
    function markAsRead(notificationId) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) return;

        fetch(`/Notification/MarkAsRead/${notificationId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token,
                'Content-Type': 'application/json'
            }
        }).then(() => location.reload());
    }

    /**
     * Initialize notification event listeners
     * Call this on DOMContentLoaded
     */
    function initNotifications() {
        // Mark all read button (uses ID added in updated views)
        const markAllReadBtn = document.getElementById('btnMarkAllRead');
        if (markAllReadBtn) {
            markAllReadBtn.addEventListener('click', markAllRead);
        }

        // Individual mark-as-read buttons
        document.querySelectorAll('.mark-read-btn').forEach(btn => {
            btn.addEventListener('click', function () {
                const id = this.dataset.id;
                if (id) markAsRead(id);
            });
        });
    }

    // Auto-initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initNotifications);
    } else {
        initNotifications();
    }

    // Expose globally for inline onclick handlers (legacy support)
    window.getNotificationIcon = getNotificationIcon;
    window.getNotificationIconClass = getNotificationIconClass;
    window.markAllRead = markAllRead;
    window.markAsRead = markAsRead;

    // Also expose on BibekSchool namespace
    window.BibekSchool = window.BibekSchool || {};
    window.BibekSchool.notifications = {
        getNotificationIcon,
        getNotificationIconClass,
        markAllRead,
        markAsRead,
        init: initNotifications
    };
})();