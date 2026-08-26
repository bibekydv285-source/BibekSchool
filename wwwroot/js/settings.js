/* ==========================================================================
   SETTINGS.JS — Admin Settings page functionality
   Used by: Admin/Settings.cshtml
   Functions: createBackup, clearLogs, optimizeDb
   ========================================================================== */

(function () {
    'use strict';

    /**
     * Create database backup
     * Shows confirmation dialog before proceeding
     */
    function createBackup() {
        if (confirm('Create a database backup? This may take a few minutes.')) {
            // TODO: Replace with actual API call when backend endpoint is ready
            alert('Backup initiated. You will be notified when complete.');
        }
    }

    /**
     * Clear old audit logs
     * Shows confirmation dialog before proceeding
     */
    function clearLogs() {
        if (confirm('Clear audit logs older than 90 days? This cannot be undone.')) {
            // TODO: Replace with actual API call when backend endpoint is ready
            alert('Old logs cleared.');
        }
    }

    /**
     * Optimize database
     * Shows confirmation dialog before proceeding
     */
    function optimizeDb() {
        if (confirm('Run database optimization?')) {
            // TODO: Replace with actual API call when backend endpoint is ready
            alert('Database optimization completed.');
        }
    }

    /**
     * Initialize settings page event listeners
     * Call this on DOMContentLoaded
     */
    function initSettings() {
        // Wire up buttons by ID (more reliable than inline onclick)
        const backupBtn = document.getElementById('btnCreateBackup');
        const clearLogsBtn = document.getElementById('btnClearLogs');
        const optimizeBtn = document.getElementById('btnOptimizeDb');

        if (backupBtn) backupBtn.addEventListener('click', createBackup);
        if (clearLogsBtn) clearLogsBtn.addEventListener('click', clearLogs);
        if (optimizeBtn) optimizeBtn.addEventListener('click', optimizeDb);
    }

    // Auto-initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSettings);
    } else {
        initSettings();
    }

    // Expose globally (for any remaining inline onclick handlers)
    window.createBackup = createBackup;
    window.clearLogs = clearLogs;
    window.optimizeDb = optimizeDb;

    // Also expose on BibekSchool namespace
    window.BibekSchool = window.BibekSchool || {};
    window.BibekSchool.settings = {
        createBackup,
        clearLogs,
        optimizeDb,
        init: initSettings
    };
})();