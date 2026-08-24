// Admin-specific JavaScript for Bibek School

(function() {
    'use strict';

    // Sidebar toggle functionality
    function initSidebarToggle() {
        const sidebarToggle = document.getElementById('sidebarToggle');
        const sidebar = document.getElementById('sidebar');
        const mainContent = document.querySelector('.main-content');
        const overlay = document.querySelector('.sidebar-overlay');

        if (sidebarToggle && sidebar) {
            sidebarToggle.addEventListener('click', function() {
                sidebar.classList.toggle('show');
                if (overlay) {
                    overlay.classList.toggle('show');
                }
            });

            // Close sidebar when clicking overlay
            if (overlay) {
                overlay.addEventListener('click', function() {
                    sidebar.classList.remove('show');
                    overlay.classList.remove('show');
                });
            }

            // Close sidebar on window resize > tablet
            window.addEventListener('resize', function() {
                if (window.innerWidth > 991.98) {
                    sidebar.classList.remove('show');
                    if (overlay) overlay.classList.remove('show');
                }
            });
        }
    }

    // Collapse sidebar on desktop
    function initSidebarCollapse() {
        const collapseBtn = document.getElementById('sidebarCollapse');
        const sidebar = document.getElementById('sidebar');
        const mainContent = document.querySelector('.main-content');

        if (collapseBtn && sidebar && mainContent) {
            collapseBtn.addEventListener('click', function() {
                sidebar.classList.toggle('collapsed');
                mainContent.classList.toggle('expanded');

                const icon = collapseBtn.querySelector('i');
                if (icon) {
                    icon.classList.toggle('fa-chevron-left');
                    icon.classList.toggle('fa-chevron-right');
                }

                // Save preference
                localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
            });

            // Load saved preference
            if (localStorage.getItem('sidebarCollapsed') === 'true') {
                sidebar.classList.add('collapsed');
                mainContent.classList.add('expanded');
                const icon = collapseBtn.querySelector('i');
                if (icon) {
                    icon.classList.remove('fa-chevron-left');
                    icon.classList.add('fa-chevron-right');
                }
            }
        }
    }

    // Active navigation highlighting
    function initActiveNav() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.sidebar-nav .nav-link');

        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && currentPath.startsWith(href) && href !== '/') {
                link.classList.add('active');
            } else if (href === '/' && currentPath === '/') {
                link.classList.add('active');
            }
        });
    }

    // Bulk actions for tables
    function initBulkActions() {
        const selectAll = document.getElementById('selectAll');
        const rowCheckboxes = document.querySelectorAll('.row-checkbox');
        const bulkActionBar = document.getElementById('bulkActionBar');
        const selectedCount = document.getElementById('selectedCount');

        if (selectAll && rowCheckboxes.length) {
            selectAll.addEventListener('change', function() {
                rowCheckboxes.forEach(cb => {
                    cb.checked = this.checked;
                });
                updateBulkActionBar();
            });

            rowCheckboxes.forEach(cb => {
                cb.addEventListener('change', function() {
                    selectAll.checked = [...rowCheckboxes].every(c => c.checked);
                    selectAll.indeterminate = [...rowCheckboxes].some(c => c.checked) && !selectAll.checked;
                    updateBulkActionBar();
                });
            });
        }

        function updateBulkActionBar() {
            const checked = document.querySelectorAll('.row-checkbox:checked').length;
            if (bulkActionBar && selectedCount) {
                selectedCount.textContent = checked;
                bulkActionBar.style.display = checked > 0 ? 'flex' : 'none';
            }
        }

        // Bulk action buttons
        document.querySelectorAll('[data-bulk-action]').forEach(btn => {
            btn.addEventListener('click', function() {
                const action = this.getAttribute('data-bulk-action');
                const ids = Array.from(document.querySelectorAll('.row-checkbox:checked'))
                    .map(cb => cb.value);

                if (ids.length === 0) {
                    BibekSchool.showAlert('Please select at least one item.', 'warning');
                    return;
                }

                if (confirm(`Apply ${action} to ${ids.length} selected items?`)) {
                    performBulkAction(action, ids);
                }
            });
        });
    }

    function performBulkAction(action, ids) {
        // Show loading
        BibekSchool.showLoading(document.querySelector('[data-bulk-action="' + action + '"]'));

        fetch('/Admin/BulkAction', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ action, ids })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                BibekSchool.showAlert(data.message || 'Action completed successfully.', 'success');
                setTimeout(() => location.reload(), 1000);
            } else {
                BibekSchool.showAlert(data.message || 'Action failed.', 'danger');
            }
        })
        .catch(error => {
            console.error('Bulk action error:', error);
            BibekSchool.showAlert('An error occurred.', 'danger');
        })
        .finally(() => {
            BibekSchool.hideLoading(document.querySelector('[data-bulk-action="' + action + '"]'));
        });
    }

    // Delete confirmations
    function initDeleteConfirmations() {
        document.querySelectorAll('form[data-confirm-delete]').forEach(form => {
            form.addEventListener('submit', function(e) {
                const message = this.getAttribute('data-confirm-delete') || 'Are you sure you want to delete this item?';
                if (!confirm(message)) {
                    e.preventDefault();
                }
            });
        });
    }

    // Form enhancements
    function initFormEnhancements() {
        // Auto-save draft
        const forms = document.querySelectorAll('form[data-auto-save]');
        forms.forEach(form => {
            const inputs = form.querySelectorAll('input, textarea, select');
            let saveTimer;

            inputs.forEach(input => {
                input.addEventListener('input', function() {
                    clearTimeout(saveTimer);
                    saveTimer = setTimeout(() => saveDraft(form), 2000);
                });
            });

            function saveDraft(form) {
                const formData = new FormData(form);
                const data = Object.fromEntries(formData.entries());

                localStorage.setItem('draft_' + form.id, JSON.stringify(data));
                showDraftSavedIndicator(form);
            }

            function showDraftSavedIndicator(form) {
                let indicator = form.querySelector('.draft-saved-indicator');
                if (!indicator) {
                    indicator = document.createElement('span');
                    indicator.className = 'draft-saved-indicator badge bg-success ms-2';
                    form.querySelector('button[type="submit"]')?.parentNode.appendChild(indicator);
                }
                indicator.textContent = 'Draft saved';
                indicator.style.display = 'inline-block';
                setTimeout(() => indicator.style.display = 'none', 3000);
            }

            // Load draft on page load
            const savedDraft = localStorage.getItem('draft_' + form.id);
            if (savedDraft) {
                try {
                    const data = JSON.parse(savedDraft);
                    Object.keys(data).forEach(key => {
                        const input = form.querySelector(`[name="${key}"]`);
                        if (input) input.value = data[key];
                    });
                    BibekSchool.showAlert('Draft restored from previous session.', 'info');
                } catch (e) {
                    console.error('Failed to load draft:', e);
                }
            }

            // Clear draft on successful submit
            form.addEventListener('submit', function() {
                localStorage.removeItem('draft_' + form.id);
            });
        });

        // Image preview
        document.querySelectorAll('input[type="file"][data-preview]').forEach(input => {
            input.addEventListener('change', function() {
                const previewId = this.getAttribute('data-preview');
                const preview = document.getElementById(previewId);
                if (preview && this.files[0]) {
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        preview.src = e.target.result;
                        preview.style.display = 'block';
                    };
                    reader.readAsDataURL(this.files[0]);
                }
            });
        });
    }

    // DataTables initialization if available
    function initDataTables() {
        if (typeof $.fn.DataTable !== 'undefined') {
            $('.datatable').DataTable({
                responsive: true,
                pageLength: 25,
                order: [[0, 'desc']],
                language: {
                    search: '_INPUT_',
                    searchPlaceholder: 'Search...',
                    lengthMenu: '_MENU_ records per page',
                    info: 'Showing _START_ to _END_ of _TOTAL_ entries',
                    paginate: {
                        previous: '<i class="fas fa-chevron-left"></i>',
                        next: '<i class="fas fa-chevron-right"></i>'
                    }
                },
                dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rtip',
                drawCallback: function() {
                    // Reinitialize tooltips
                    const tooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
                    tooltips.map(el => new bootstrap.Tooltip(el));
                }
            });
        }
    }

    // Notification real-time updates
    function initNotificationPolling() {
        const notificationBell = document.querySelector('.notification-bell');
        if (notificationBell) {
            function fetchNotificationCount() {
                fetch('/Notification/UnreadCount')
                    .then(res => res.json())
                    .then(data => {
                        const badge = notificationBell.querySelector('.badge');
                        if (badge) {
                            badge.textContent = data.count;
                            badge.style.display = data.count > 0 ? 'inline-block' : 'none';
                        }
                    })
                    .catch(err => console.error('Notification poll error:', err));
            }

            fetchNotificationCount();
            setInterval(fetchNotificationCount, 60000); // Every minute
        }
    }

    // Keyboard shortcuts for admin
    function initAdminShortcuts() {
        document.addEventListener('keydown', function(e) {
            // Alt + N for new record
            if (e.altKey && e.key === 'n') {
                e.preventDefault();
                const newBtn = document.querySelector('[data-shortcut="new"]');
                if (newBtn) newBtn.click();
            }

            // Alt + S for search
            if (e.altKey && e.key === 's') {
                e.preventDefault();
                const searchInput = document.querySelector('input[type="search"], input[name="search"]');
                if (searchInput) searchInput.focus();
            }

            // Escape to close sidebar
            if (e.key === 'Escape') {
                const sidebar = document.getElementById('sidebar');
                const overlay = document.querySelector('.sidebar-overlay');
                if (sidebar && sidebar.classList.contains('show')) {
                    sidebar.classList.remove('show');
                    if (overlay) overlay.classList.remove('show');
                }
            }
        });
    }

    // Print report
    function initPrintReport() {
        document.querySelectorAll('[data-print-report]').forEach(btn => {
            btn.addEventListener('click', function() {
                const reportId = this.getAttribute('data-print-report');
                const report = document.getElementById(reportId);
                if (report) {
                    const printWindow = window.open('', '_blank');
                    printWindow.document.write(`
                        <html>
                        <head>
                            <title>Report</title>
                            <link href="/css/site.css" rel="stylesheet">
                            <link href="/css/admin.css" rel="stylesheet">
                            <link href="/css/dashboard.css" rel="stylesheet">
                            <style>
                                @media print { .no-print { display: none !important; } }
                            </style>
                        </head>
                        <body>${report.outerHTML}</body>
                        </html>
                    `);
                    printWindow.document.close();
                    printWindow.focus();
                    setTimeout(() => printWindow.print(), 500);
                }
            });
        });
    }

    // Initialize all admin features
    document.addEventListener('DOMContentLoaded', function() {
        initSidebarToggle();
        initSidebarCollapse();
        initActiveNav();
        initBulkActions();
        initDeleteConfirmations();
        initFormEnhancements();
        initDataTables();
        initNotificationPolling();
        initAdminShortcuts();
        initPrintReport();
    });

    // Global admin functions
    window.Admin = {
        refreshSidebar: initActiveNav,
        showBulkActions: function() {
            document.getElementById('bulkActionBar')?.style.display = 'flex';
        },
        hideBulkActions: function() {
            document.getElementById('bulkActionBar')?.style.display = 'none';
        },
        toggleSidebar: function() {
            document.getElementById('sidebar')?.classList.toggle('show');
            document.querySelector('.sidebar-overlay')?.classList.toggle('show');
        }
    };

})();