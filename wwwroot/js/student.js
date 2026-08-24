// Student-specific JavaScript for Bibek School

(function() {
    'use strict';

    // ===== Sidebar Toggle for Mobile/Tablet =====
    function initSidebarToggle() {
        const sidebarToggle = document.getElementById('sidebarToggle');
        const sidebar = document.getElementById('sidebar');
        const sidebarOverlay = document.getElementById('sidebarOverlay');

        if (!sidebarToggle || !sidebar || !sidebarOverlay) return;

        // Toggle sidebar
        sidebarToggle.addEventListener('click', function() {
            const isExpanded = sidebar.classList.toggle('show');
            sidebarOverlay.classList.toggle('show', isExpanded);
            sidebarToggle.setAttribute('aria-expanded', isExpanded);
            document.body.style.overflow = isExpanded ? 'hidden' : '';
        });

        // Close sidebar when clicking overlay
        sidebarOverlay.addEventListener('click', function() {
            sidebar.classList.remove('show');
            sidebarOverlay.classList.remove('show');
            sidebarToggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
        });

        // Close sidebar on escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && sidebar.classList.contains('show')) {
                sidebar.classList.remove('show');
                sidebarOverlay.classList.remove('show');
                sidebarToggle.setAttribute('aria-expanded', 'false');
                document.body.style.overflow = '';
                sidebarToggle.focus();
            }
        });

        // Close sidebar when clicking a nav link on mobile
        if (window.innerWidth < 992) {
            sidebar.querySelectorAll('.nav-link').forEach(link => {
                link.addEventListener('click', function() {
                    if (window.innerWidth < 992) {
                        sidebar.classList.remove('show');
                        sidebarOverlay.classList.remove('show');
                        sidebarToggle.setAttribute('aria-expanded', 'false');
                        document.body.style.overflow = '';
                    }
                });
            });
        }

        // Handle resize
        let resizeTimer;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function() {
                if (window.innerWidth >= 992) {
                    sidebar.classList.remove('show');
                    sidebarOverlay.classList.remove('show');
                    sidebarToggle.setAttribute('aria-expanded', 'false');
                    document.body.style.overflow = '';
                }
            }, 250);
        });
    }

    // Marks visualization
    function initMarksVisualization() {
        const marksTable = document.querySelector('.marks-table');
        if (!marksTable) return;

        // Add progress bars to percentage column
        marksTable.querySelectorAll('tbody tr').forEach(row => {
            const percentageCell = row.querySelector('[data-percentage]');
            if (percentageCell) {
                const percentage = parseFloat(percentageCell.getAttribute('data-percentage'));
                if (!isNaN(percentage)) {
                    const progressBar = document.createElement('div');
                    progressBar.className = 'progress';
                    progressBar.style.height = '20px';
                    progressBar.innerHTML = `
                        <div class="progress-bar ${percentage >= 40 ? 'bg-success' : 'bg-danger'}" 
                             role="progressbar" 
                             style="width: ${Math.min(percentage, 100)}%" 
                             aria-valuenow="${percentage}" 
                             aria-valuemin="0" 
                             aria-valuemax="100">
                            ${percentage.toFixed(1)}%
                        </div>
                    `;
                    percentageCell.innerHTML = '';
                    percentageCell.appendChild(progressBar);
                }
            }
        });

        // Color code grade badges
        marksTable.querySelectorAll('[data-grade]').forEach(badge => {
            const grade = badge.getAttribute('data-grade');
            if (grade) {
                badge.className = 'badge fs-6 ' + getGradeColorClass(grade);
            }
        });

        // Color code status badges
        marksTable.querySelectorAll('[data-status]').forEach(badge => {
            const status = badge.getAttribute('data-status');
            if (status === 'Pass') {
                badge.className = 'badge bg-success';
            } else if (status === 'Fail') {
                badge.className = 'badge bg-danger';
            }
        });
    }

    function getGradeColorClass(grade) {
        switch (grade) {
            case 'A+': return 'bg-success';
            case 'A': return 'bg-success';
            case 'B+': return 'bg-info';
            case 'B': return 'bg-info';
            case 'C+': return 'bg-warning text-dark';
            case 'C': return 'bg-warning text-dark';
            case 'D': return 'bg-secondary';
            case 'F': return 'bg-danger';
            default: return 'bg-secondary';
        }
    }

    // Result visualization
    function initResultVisualization() {
        const resultCard = document.querySelector('.result-card');
        if (!resultCard) return;

        const gradeBadge = resultCard.querySelector('[data-overall-grade]');
        if (gradeBadge) {
            const grade = gradeBadge.getAttribute('data-overall-grade');
            if (grade) {
                gradeBadge.className = 'badge fs-1 p-3 ' + getGradeColorClass(grade);
            }
        }

        // Animate percentage counter
        const percentageElement = resultCard.querySelector('[data-percentage-counter]');
        if (percentageElement) {
            const target = parseFloat(percentageElement.getAttribute('data-percentage-counter'));
            animateCounter(percentageElement, target, '%');
        }

        // Subject marks with progress bars
        resultCard.querySelectorAll('[data-subject-percentage]').forEach(cell => {
            const percentage = parseFloat(cell.getAttribute('data-subject-percentage'));
            if (!isNaN(percentage)) {
                const progressBar = document.createElement('div');
                progressBar.className = 'progress';
                progressBar.style.height = '15px';
                progressBar.innerHTML = `
                    <div class="progress-bar ${percentage >= 40 ? 'bg-success' : 'bg-danger'}" 
                         role="progressbar" 
                         style="width: ${Math.min(percentage, 100)}%" 
                         aria-valuenow="${percentage}" 
                         aria-valuemin="0" 
                         aria-valuemax="100">
                        <small>${percentage.toFixed(1)}%</small>
                    </div>
                `;
                cell.innerHTML = '';
                cell.appendChild(progressBar);
            }
        });
    }

    function animateCounter(element, target, suffix) {
        const duration = 1500;
        const start = 0;
        const startTime = performance.now();

        function update(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            const current = start + (target - start) * eased;
            element.textContent = current.toFixed(1) + suffix;

            if (progress < 1) {
                requestAnimationFrame(update);
            }
        }

        requestAnimationFrame(update);
    }

    // Academic year filter for marks
    function initAcademicYearFilter() {
        const filterForm = document.getElementById('academicYearFilter');
        if (!filterForm) return;

        const yearSelect = filterForm.querySelector('select[name="academicYear"]');
        if (yearSelect) {
            yearSelect.addEventListener('change', function() {
                filterForm.submit();
            });
        }
    }

    // Notification handling
    function initStudentNotifications() {
        // Mark as read
        document.querySelectorAll('.mark-read-btn').forEach(btn => {
            btn.addEventListener('click', function() {
                const notificationId = this.getAttribute('data-id');
                const item = this.closest('.notification-item');

                fetch(`/Notification/MarkAsRead/${notificationId}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        item.classList.remove('bg-light');
                        item.querySelector('h6')?.classList.remove('fw-bold');
                        this.remove();

                        // Update count
                        const countBadge = document.querySelector('[data-notification-count]');
                        if (countBadge) {
                            const count = parseInt(countBadge.textContent) - 1;
                            countBadge.textContent = Math.max(0, count);
                            countBadge.style.display = count > 0 ? 'inline-block' : 'none';
                        }
                    }
                });
            });
        });

        // Mark all as read
        const markAllBtn = document.querySelector('[onclick*="markAllRead"]');
        if (markAllBtn) {
            markAllBtn.addEventListener('click', function(e) {
                e.preventDefault();
                fetch('/Notification/MarkAllAsRead', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        location.reload();
                    }
                });
            });
        }
    }

    // Profile image preview
    function initProfileImagePreview() {
        const imageInput = document.getElementById('ProfileImage');
        const preview = document.getElementById('profileImagePreview');

        if (imageInput && preview) {
            imageInput.addEventListener('change', function() {
                if (this.files[0]) {
                    const reader = new FileReader();
                    reader.onload = function(e) {
                        preview.src = e.target.result;
                        preview.style.display = 'block';
                    };
                    reader.readAsDataURL(this.files[0]);
                }
            });
        }
    }

    // Password strength for change password
    function initPasswordStrength() {
        const newPasswordInput = document.getElementById('NewPassword');
        const strengthContainer = document.getElementById('passwordStrength');

        if (newPasswordInput && strengthContainer) {
            newPasswordInput.addEventListener('input', function() {
                const password = this.value;
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

                const percentage = (strength / 5) * 100;
                let colorClass = 'bg-danger';
                if (strength >= 4) colorClass = 'bg-success';
                else if (strength >= 3) colorClass = 'bg-warning';
                else if (strength >= 2) colorClass = 'bg-info';

                strengthContainer.innerHTML = `
                    <div class="progress" style="height: 6px;">
                        <div class="progress-bar ${colorClass}" role="progressbar" style="width: ${percentage}%"></div>
                    </div>
                    <small class="text-muted">${feedback.length ? 'Missing: ' + feedback.join(', ') : 'Strong password!'}</small>
                `;
            });
        }
    }

    // Quick navigation shortcuts
    function initStudentShortcuts() {
        document.addEventListener('keydown', function(e) {
            // Alt + 1-6 for quick navigation
            if (e.altKey && e.key >= '1' && e.key <= '6') {
                e.preventDefault();
                const links = {
                    '1': '/Student/Dashboard',
                    '2': '/Student/Profile',
                    '3': '/Student/Class',
                    '4': '/Student/Subjects',
                    '5': '/Student/Marks',
                    '6': '/Student/Results'
                };
                if (links[e.key]) {
                    window.location.href = links[e.key];
                }
            }

            // Escape to go back
            if (e.key === 'Escape') {
                if (window.history.length > 1) {
                    history.back();
                }
            }
        });
    }

    // Download result as PDF
    function initDownloadResult() {
        const downloadBtn = document.getElementById('downloadResult');
        if (downloadBtn) {
            downloadBtn.addEventListener('click', function() {
                const resultContent = document.querySelector('.result-card');
                if (resultContent) {
                    const printWindow = window.open('', '_blank');
                    printWindow.document.write(`
                        <html>
                        <head>
                            <title>Result Card</title>
                            <link href="/css/site.css" rel="stylesheet">
                            <link href="/css/student.css" rel="stylesheet">
                            <link href="/css/dashboard.css" rel="stylesheet">
                            <style>
                                @media print { 
                                    .no-print { display: none !important; }
                                    body { padding: 20px; }
                                }
                                @page { margin: 20mm; }
                            </style>
                        </head>
                        <body>${resultContent.outerHTML}</body>
                        </html>
                    `);
                    printWindow.document.close();
                    printWindow.focus();
                    setTimeout(() => printWindow.print(), 500);
                }
            });
        }
    }

    // Attendance calendar (if available)
    function initAttendanceCalendar() {
        const calendarEl = document.getElementById('attendanceCalendar');
        if (calendarEl && typeof FullCalendar !== 'undefined') {
            const calendar = new FullCalendar.Calendar(calendarEl, {
                initialView: 'dayGridMonth',
                headerToolbar: {
                    left: 'prev,next today',
                    center: 'title',
                    right: 'dayGridMonth,timeGridWeek'
                },
                events: '/Student/AttendanceEvents',
                eventColor: '#2563eb',
                eventDidMount: function(info) {
                    if (info.event.extendedProps.status === 'absent') {
                        info.el.style.backgroundColor = '#ef4444';
                    } else if (info.event.extendedProps.status === 'late') {
                        info.el.style.backgroundColor = '#f59e0b';
                    }
                }
            });
            calendar.render();
        }
    }

    // Timetable view
    function initTimetable() {
        const timetableContainer = document.getElementById('studentTimetable');
        if (!timetableContainer) return;

        // Fetch timetable data
        fetch('/Student/GetTimetable')
            .then(res => res.json())
            .then(data => {
                renderTimetable(timetableContainer, data);
            })
            .catch(err => console.error('Timetable load error:', err));
    }

    function renderTimetable(container, data) {
        const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        const periods = ['1st', '2nd', '3rd', '4th', '5th', '6th', '7th'];

        let html = '<table class="table table-bordered timetable-table">';
        html += '<thead class="table-light"><tr><th>Period</th>';
        days.forEach(day => html += `<th>${day}</th>`);
        html += '</tr></thead><tbody>';

        periods.forEach((period, pIndex) => {
            html += `<tr><td class="fw-bold">${period}</td>`;
            days.forEach((day, dIndex) => {
                const classData = data.find(d => d.day === dIndex && d.period === pIndex);
                if (classData) {
                    html += `<td class="has-class">
                        <div class="subject-name">${classData.subject}</div>
                        <div class="teacher-name">${classData.teacher}</div>
                        <div class="room">${classData.room}</div>
                    </td>`;
                } else {
                    html += '<td class="text-muted text-center">Free</td>';
                }
            });
            html += '</tr>';
        });

        html += '</tbody></table>';
        container.innerHTML = html;
    }

    // Grade calculator
    function initGradeCalculator() {
        const calculatorForm = document.getElementById('gradeCalculator');
        if (!calculatorForm) return;

        calculatorForm.addEventListener('submit', function(e) {
            e.preventDefault();

            const obtained = parseFloat(this.querySelector('[name="obtained"]').value);
            const full = parseFloat(this.querySelector('[name="full"]').value);
            const pass = parseFloat(this.querySelector('[name="pass"]').value) || 40;

            if (isNaN(obtained) || isNaN(full) || full <= 0) {
                BibekSchool.showAlert('Please enter valid marks.', 'warning');
                return;
            }

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

            const resultDiv = document.getElementById('calculatorResult');
            if (resultDiv) {
                resultDiv.innerHTML = `
                    <div class="card mt-3">
                        <div class="card-body text-center">
                            <div class="grade-badge ${getGradeColorClass(grade).replace('bg-', '')}">${grade}</div>
                            <h4 class="mt-2">${percentage.toFixed(2)}%</h4>
                            <p class="text-muted">${obtained} / ${full}</p>
                            <span class="badge ${obtained >= pass ? 'bg-success' : 'bg-danger'} fs-6">
                                ${obtained >= pass ? 'PASS' : 'FAIL'}
                            </span>
                        </div>
                    </div>
                `;
            }
        });
    }

    // Initialize all student features
    document.addEventListener('DOMContentLoaded', function() {
        initSidebarToggle();
        initMarksVisualization();
        initResultVisualization();
        initAcademicYearFilter();
        initStudentNotifications();
        initProfileImagePreview();
        initPasswordStrength();
        initStudentShortcuts();
        initDownloadResult();
        initAttendanceCalendar();
        initTimetable();
        initGradeCalculator();
    });

    // Global student functions
    window.Student = {
        refreshMarks: function() {
            location.reload();
        },
        calculateGPA: function() {
            // Implementation for GPA calculation
        }
    };

})();