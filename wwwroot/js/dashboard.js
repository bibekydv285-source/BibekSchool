// Dashboard JavaScript for Bibek School

(function() {
    'use strict';

    // Animate stat cards on scroll
    function animateStatCards() {
        const statCards = document.querySelectorAll('.stat-card');
        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry, index) => {
                if (entry.isIntersecting) {
                    setTimeout(() => {
                        entry.target.classList.add('fade-in');
                        animateNumber(entry.target);
                    }, index * 100);
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });

        statCards.forEach(card => observer.observe(card));
    }

    // Animate numbers counting up
    function animateNumber(card) {
        const numberElement = card.querySelector('h2');
        if (!numberElement) return;

        const target = parseFloat(numberElement.textContent.replace(/[^0-9.]/g, ''));
        const suffix = numberElement.textContent.replace(/[0-9.]/g, '');
        const duration = 1500;
        const start = 0;
        const startTime = performance.now();

        function updateNumber(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const eased = easeOutCubic(progress);
            const current = start + (target - start) * eased;
            numberElement.textContent = current.toFixed(target % 1 === 0 ? 0 : 2) + suffix;

            if (progress < 1) {
                requestAnimationFrame(updateNumber);
            }
        }

        requestAnimationFrame(updateNumber);
    }

    function easeOutCubic(t) {
        return 1 - Math.pow(1 - t, 3);
    }

    // Initialize charts if Chart.js is available
    function initCharts() {
        if (typeof Chart !== 'undefined') {
            // Performance chart
            const perfCtx = document.getElementById('performanceChart');
            if (perfCtx) {
                new Chart(perfCtx, {
                    type: 'line',
                    data: {
                        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                        datasets: [{
                            label: 'Average Score',
                            data: [72, 75, 78, 80, 82, 85],
                            borderColor: '#2563eb',
                            backgroundColor: 'rgba(37, 99, 235, 0.1)',
                            tension: 0.4,
                            fill: true
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false }
                        },
                        scales: {
                            y: { beginAtZero: true, max: 100 }
                        }
                    }
                });
            }

            // Attendance chart
            const attendCtx = document.getElementById('attendanceChart');
            if (attendCtx) {
                new Chart(attendCtx, {
                    type: 'doughnut',
                    data: {
                        labels: ['Present', 'Absent', 'Late'],
                        datasets: [{
                            data: [85, 10, 5],
                            backgroundColor: ['#10b981', '#ef4444', '#f59e0b'],
                            borderWidth: 0
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { position: 'bottom' }
                        }
                    }
                });
            }
        }
    }

    // Real-time clock
    function initClock() {
        const clockElement = document.getElementById('liveClock');
        if (clockElement) {
            function updateClock() {
                const now = new Date();
                clockElement.textContent = now.toLocaleTimeString('en-US', {
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit',
                    hour12: true
                });
            }
            updateClock();
            setInterval(updateClock, 1000);
        }
    }

    // Notification badge update
    function updateNotificationBadges() {
        const badges = document.querySelectorAll('[data-notification-count]');
        badges.forEach(badge => {
            const count = parseInt(badge.getAttribute('data-notification-count')) || 0;
            badge.textContent = count;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        });
    }

    // Search functionality
    function initSearch() {
        const searchInputs = document.querySelectorAll('input[data-search-target]');
        searchInputs.forEach(input => {
            const targetSelector = input.getAttribute('data-search-target');
            const targets = document.querySelectorAll(targetSelector);

            input.addEventListener('input', BibekSchool.debounce(function() {
                const query = this.value.toLowerCase();
                targets.forEach(target => {
                    const text = target.textContent.toLowerCase();
                    target.style.display = text.includes(query) ? '' : 'none';
                });
            }, 300));
        });
    }

    // Table sorting
    function initTableSort() {
        const sortableTables = document.querySelectorAll('table[data-sortable]');
        sortableTables.forEach(table => {
            const headers = table.querySelectorAll('th[data-sort]');
            headers.forEach(header => {
                header.style.cursor = 'pointer';
                header.addEventListener('click', function() {
                    const column = this.getAttribute('data-sort');
                    const tbody = table.querySelector('tbody');
                    const rows = Array.from(tbody.querySelectorAll('tr'));
                    const isAsc = this.classList.contains('sort-asc');

                    // Reset other headers
                    headers.forEach(h => h.classList.remove('sort-asc', 'sort-desc'));

                    // Toggle sort direction
                    this.classList.add(isAsc ? 'sort-desc' : 'sort-asc');

                    // Sort rows
                    rows.sort((a, b) => {
                        const aVal = a.querySelector(`[data-${column}]`)?.textContent || '';
                        const bVal = b.querySelector(`[data-${column}]`)?.textContent || '';
                        return isAsc ? bVal.localeCompare(aVal, undefined, { numeric: true }) : aVal.localeCompare(bVal, undefined, { numeric: true });
                    });

                    // Re-append sorted rows
                    rows.forEach(row => tbody.appendChild(row));
                });
            });
        });
    }

    // Export table to CSV
    function initExportButtons() {
        const exportButtons = document.querySelectorAll('[data-export-table]');
        exportButtons.forEach(button => {
            button.addEventListener('click', function() {
                const tableSelector = this.getAttribute('data-export-table');
                const table = document.querySelector(tableSelector);
                if (table) {
                    exportTableToCSV(table, this.getAttribute('data-filename') || 'export.csv');
                }
            });
        });
    }

    function exportTableToCSV(table, filename) {
        const rows = table.querySelectorAll('tr');
        const csv = [];

        rows.forEach(row => {
            const cols = row.querySelectorAll('th, td');
            const rowData = [];
            cols.forEach(col => {
                let text = col.textContent.trim();
                if (text.includes(',') || text.includes('"') || text.includes('\n')) {
                    text = '"' + text.replace(/"/g, '""') + '"';
                }
                rowData.push(text);
            });
            csv.push(rowData.join(','));
        });

        const csvContent = csv.join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        link.click();
        URL.revokeObjectURL(link.href);
    }

    // Initialize all dashboard features
    document.addEventListener('DOMContentLoaded', function() {
        animateStatCards();
        initCharts();
        initClock();
        updateNotificationBadges();
        initSearch();
        initTableSort();
        initExportButtons();

        // Refresh stats periodically
        setInterval(updateNotificationBadges, 30000);
    });

    // Expose functions globally
    window.Dashboard = {
        animateStatCards,
        animateNumber,
        initCharts,
        updateNotificationBadges,
        exportTableToCSV
    };

})();