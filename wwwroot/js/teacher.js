// Teacher-specific JavaScript for Bibek School

(function () {
    'use strict';

    // ===== Sidebar Toggle for Mobile/Tablet =====
    function initSidebarToggle() {
        const sidebarToggle = document.getElementById('sidebarToggle');
        const sidebar = document.getElementById('sidebar');
        const sidebarOverlay = document.getElementById('sidebarOverlay');

        if (!sidebarToggle || !sidebar || !sidebarOverlay) return;

        function openSidebar() {
            sidebar.classList.add('show');
            sidebarOverlay.classList.add('show');
            sidebarToggle.setAttribute('aria-expanded', 'true');
            document.body.style.overflow = 'hidden';
        }

        function closeSidebar() {
            sidebar.classList.remove('show');
            sidebarOverlay.classList.remove('show');
            sidebarToggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
        }

        // Toggle sidebar
        sidebarToggle.addEventListener('click', function () {
            if (sidebar.classList.contains('show')) {
                closeSidebar();
            } else {
                openSidebar();
            }
        });

        // Close sidebar when clicking overlay
        sidebarOverlay.addEventListener('click', closeSidebar);

        // Close sidebar on escape key
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && sidebar.classList.contains('show')) {
                closeSidebar();
                sidebarToggle.focus();
            }
        });

        // Close sidebar when clicking a nav link on mobile
        sidebar.querySelectorAll('.nav-link').forEach(link => {
            link.addEventListener('click', function () {
                if (window.innerWidth < 992) {
                    closeSidebar();
                }
            });
        });

        // Handle resize — auto-close if resized back to desktop
        let resizeTimer;
        window.addEventListener('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                if (window.innerWidth >= 992) {
                    closeSidebar();
                }
            }, 250);
        });
    }

    // Mark entry form handling
    function initMarkEntry() {
        const markForm = document.getElementById('markEntryForm');
        if (!markForm) return;

        const subjectSelect = markForm.querySelector('[name="SubjectId"]');
        const fullMarksInput = markForm.querySelector('[name="FullMarks"]');
        const passMarksInput = markForm.querySelector('[name="PassMarks"]');
        const obtainedMarksInput = markForm.querySelector('[name="ObtainedMarks"]');
        const gradeDisplay = markForm.querySelector('[data-grade-display]');
        const percentageDisplay = markForm.querySelector('[data-percentage-display]');

        // Load subject details when subject changes
        if (subjectSelect) {
            subjectSelect.addEventListener('change', function () {
                const selectedOption = this.options[this.selectedIndex];
                const fullMarks = selectedOption.getAttribute('data-full-marks');
                const passMarks = selectedOption.getAttribute('data-pass-marks');

                if (fullMarks) fullMarksInput.value = fullMarks;
                if (passMarks) passMarksInput.value = passMarks;
                calculateGrade();
            });
        }

        // Calculate grade in real-time
        function calculateGrade() {
            const obtained = parseFloat(obtainedMarksInput?.value) || 0;
            const full = parseFloat(fullMarksInput?.value) || 100;
            const pass = parseFloat(passMarksInput?.value) || 40;

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

                if (gradeDisplay) gradeDisplay.textContent = grade;
                if (percentageDisplay) percentageDisplay.textContent = percentage.toFixed(2) + '%';

                if (gradeDisplay) {
                    gradeDisplay.className = 'badge fs-6 ' + (obtained >= pass ? 'bg-success' : 'bg-danger');
                }
            }
        }

        if (obtainedMarksInput) obtainedMarksInput.addEventListener('input', calculateGrade);
        if (fullMarksInput) fullMarksInput.addEventListener('input', calculateGrade);
        if (passMarksInput) passMarksInput.addEventListener('input', calculateGrade);

        // Initial calculation
        calculateGrade();
    }

    // Bulk mark entry
    function initBulkMarkEntry() {
        const bulkForm = document.getElementById('bulkMarkEntryForm');
        if (!bulkForm) return;

        const addRowBtn = bulkForm.querySelector('[data-add-row]');
        const tableBody = bulkForm.querySelector('tbody[data-rows]');
        const template = bulkForm.querySelector('template[data-row-template]');

        if (addRowBtn && tableBody && template) {
            addRowBtn.addEventListener('click', function () {
                const clone = template.content.cloneNode(true);
                const row = clone.querySelector('tr');
                const index = tableBody.querySelectorAll('tr').length;

                row.querySelectorAll('input, select').forEach(input => {
                    input.name = input.name.replace('__INDEX__', index);
                });

                tableBody.appendChild(clone);

                if (typeof $ !== 'undefined' && $.fn.valid) {
                    $(bulkForm).data('validator', null);
                    $.validator.unobtrusive.parse(bulkForm);
                }
            });
        }

        // Remove row
        document.addEventListener('click', function (e) {
            const removeBtn = e.target.closest('[data-remove-row]');
            if (removeBtn && tableBody) {
                const row = removeBtn.closest('tr');
                if (tableBody.querySelectorAll('tr').length > 1) {
                    row.remove();
                } else {
                    row.querySelectorAll('input, select').forEach(input => input.value = '');
                }
            }
        });
    }

    // Quick mark entry from class view
    function initQuickMarkEntry() {
        document.querySelectorAll('[data-quick-mark]').forEach(btn => {
            btn.addEventListener('click', function () {
                const studentId = this.getAttribute('data-student-id');
                const subjectId = this.getAttribute('data-subject-id');
                const classId = this.getAttribute('data-class-id');

                const url = `/Teacher/CreateMark?studentId=${studentId}&subjectId=${subjectId}&classId=${classId}`;
                window.location.href = url;
            });
        });
    }

    // Marks table inline editing
    function initInlineMarkEditing() {
        document.querySelectorAll('.marks-table .editable-cell').forEach(cell => {
            cell.addEventListener('dblclick', function () {
                if (this.querySelector('input')) return;

                const originalValue = this.textContent.trim();
                const field = this.getAttribute('data-field');
                const markId = this.closest('tr').getAttribute('data-mark-id');

                const input = document.createElement('input');
                input.type = field === 'ObtainedMarks' ? 'number' : 'text';
                input.value = originalValue;
                input.className = 'form-control form-control-sm';
                input.style.width = '100%';

                this.innerHTML = '';
                this.appendChild(input);
                input.focus();
                input.select();

                const cellEl = this;

                function save() {
                    const newValue = input.value;
                    if (newValue !== originalValue) {
                        updateMark(markId, field, newValue, cellEl);
                    } else {
                        cellEl.textContent = originalValue;
                    }
                }

                input.addEventListener('blur', save);
                input.addEventListener('keydown', function (e) {
                    if (e.key === 'Enter') save();
                    if (e.key === 'Escape') cellEl.textContent = originalValue;
                });
            });
        });
    }

    function updateMark(markId, field, value, cell) {
        fetch(`/Teacher/UpdateMarkField`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ markId, field, value })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    cell.textContent = data.displayValue;
                    BibekSchool.showAlert('Mark updated successfully.', 'success');

                    if (data.updatedFields) {
                        Object.keys(data.updatedFields).forEach(key => {
                            const targetCell = cell.closest('tr').querySelector(`[data-field="${key}"]`);
                            if (targetCell) targetCell.textContent = data.updatedFields[key];
                        });
                    }
                } else {
                    BibekSchool.showAlert(data.message || 'Failed to update mark.', 'danger');
                    cell.textContent = cell.getAttribute('data-original') || '';
                }
            })
            .catch(err => {
                console.error('Mark update error:', err);
                BibekSchool.showAlert('An error occurred.', 'danger');
                cell.textContent = cell.getAttribute('data-original') || '';
            });
    }

    // Student search/filter in marks view
    function initStudentFilter() {
        const searchInput = document.getElementById('studentSearch');
        const tableBody = document.querySelector('.marks-table tbody');

        if (searchInput && tableBody) {
            searchInput.addEventListener('input', BibekSchool.debounce(function () {
                const query = this.value.toLowerCase();
                const rows = tableBody.querySelectorAll('tr');

                rows.forEach(row => {
                    const studentName = row.querySelector('[data-student-name]')?.textContent.toLowerCase() || '';
                    const admissionNo = row.querySelector('[data-admission-no]')?.textContent.toLowerCase() || '';
                    row.style.display = (studentName.includes(query) || admissionNo.includes(query)) ? '' : 'none';
                });
            }, 300));
        }
    }

    // Exam filter
    function initExamFilter() {
        const examFilter = document.getElementById('examFilter');
        const tableBody = document.querySelector('.marks-table tbody');

        if (examFilter && tableBody) {
            examFilter.addEventListener('change', function () {
                const examName = this.value;
                const rows = tableBody.querySelectorAll('tr');

                rows.forEach(row => {
                    const exam = row.querySelector('[data-exam-name]')?.textContent;
                    row.style.display = (!examName || exam === examName) ? '' : 'none';
                });
            });
        }
    }

    // Class performance summary
    function initClassPerformance() {
        const summaryContainer = document.getElementById('classPerformanceSummary');
        if (!summaryContainer) return;

        const rows = document.querySelectorAll('.marks-table tbody tr:not([style*="display: none"])');
        const subjects = new Map();

        rows.forEach(row => {
            const subject = row.querySelector('[data-subject-name]')?.textContent;
            const obtained = parseFloat(row.querySelector('[data-obtained]')?.textContent) || 0;
            const full = parseFloat(row.querySelector('[data-full]')?.textContent) || 100;
            const passed = row.querySelector('[data-status]')?.textContent?.includes('Pass');

            if (!subjects.has(subject)) {
                subjects.set(subject, { total: 0, obtained: 0, passed: 0, count: 0 });
            }

            const subj = subjects.get(subject);
            subj.total += full;
            subj.obtained += obtained;
            subj.count++;
            if (passed) subj.passed++;
        });

        let html = '<table class="table table-sm"><thead><tr><th>Subject</th><th>Avg %</th><th>Pass Rate</th></tr></thead><tbody>';
        subjects.forEach((data, subject) => {
            const avgPercent = data.total > 0 ? (data.obtained / data.total * 100).toFixed(1) : 0;
            const passRate = data.count > 0 ? (data.passed / data.count * 100).toFixed(1) : 0;
            html += `<tr><td>${subject}</td><td>${avgPercent}%</td><td>${passRate}%</td></tr>`;
        });
        html += '</tbody></table>';

        summaryContainer.innerHTML = html;
    }

    // Result generation
    function initResultGeneration() {
        document.querySelectorAll('[data-generate-result]').forEach(btn => {
            btn.addEventListener('click', function () {
                const studentId = this.getAttribute('data-student-id');
                const academicYear = this.getAttribute('data-academic-year');
                const term = this.getAttribute('data-term');

                if (confirm('Generate result for this student?')) {
                    BibekSchool.showLoading(this);

                    fetch('/Result/Generate', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                        },
                        body: JSON.stringify({ studentId, academicYear, term })
                    })
                        .then(res => res.json())
                        .then(data => {
                            if (data.success) {
                                BibekSchool.showAlert('Result generated successfully.', 'success');
                                setTimeout(() => location.reload(), 1000);
                            } else {
                                BibekSchool.showAlert(data.message || 'Failed to generate result.', 'danger');
                            }
                        })
                        .catch(err => {
                            console.error('Result generation error:', err);
                            BibekSchool.showAlert('An error occurred.', 'danger');
                        })
                        .finally(() => BibekSchool.hideLoading(this));
                }
            });
        });
    }

    // Initialize all teacher features — ONE listener, runs once
    document.addEventListener('DOMContentLoaded', function () {
        initSidebarToggle();
        initMarkEntry();
        initBulkMarkEntry();
        initQuickMarkEntry();
        initInlineMarkEditing();
        initStudentFilter();
        initExamFilter();
        initClassPerformance();
        initResultGeneration();
    });

    // Global teacher functions
    window.Teacher = {
        refreshMarksTable: function () {
            location.reload();
        },
        calculateClassAverage: function (subjectId) {
            // Implementation for calculating class average
        }
    };

})();