// Internal Marks Management System - Client Side Scripts

document.addEventListener("DOMContentLoaded", function () {
    // Mobile sidebar toggle
    const toggleBtn = document.getElementById("sidebarToggle");
    const sidebar = document.querySelector(".app-sidebar");
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener("click", function () {
            sidebar.classList.toggle("show");
        });
    }

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll(".alert-dismissible");
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 6000);
    });

    // Dynamic Marks Calculation in Marks Entry Table
    setupDynamicMarksCalculation();
});

function setupDynamicMarksCalculation() {
    const marksTable = document.getElementById("marksEntryTable");
    if (!marksTable) return;

    const maxInternalMarks = parseFloat(marksTable.dataset.maxInternal || "50");
    const passingMarks = parseFloat(marksTable.dataset.passingMarks || "20");

    // Gather component weights and max marks from header
    const componentHeaders = marksTable.querySelectorAll(".comp-header");
    const components = [];
    componentHeaders.forEach(th => {
        components.push({
            id: th.dataset.compId,
            maxMarks: parseFloat(th.dataset.maxMarks || "30"),
            weight: parseFloat(th.dataset.weight || "30")
        });
    });

    let totalWeight = components.reduce((sum, c) => sum + c.weight, 0);
    if (totalWeight <= 0) totalWeight = 100;

    // Attach listener to all marks input fields
    const inputs = marksTable.querySelectorAll(".mark-input");
    inputs.forEach(input => {
        input.addEventListener("input", function () {
            const row = input.closest("tr");
            calculateRow(row, components, maxInternalMarks, passingMarks, totalWeight);
        });
    });
}

function calculateRow(row, components, maxInternalMarks, passingMarks, totalWeight) {
    if (!row) return;

    let totalWeightedScore = 0;

    components.forEach(comp => {
        const input = row.querySelector(`.mark-input[data-comp-id="${comp.id}"]`);
        if (input) {
            let val = parseFloat(input.value);
            if (isNaN(val) || val < 0) val = 0;
            if (val > comp.maxMarks) {
                val = comp.maxMarks;
                input.value = comp.maxMarks;
            }

            const compRatio = comp.weight / totalWeight;
            const compContribution = (val / comp.maxMarks) * (maxInternalMarks * compRatio);
            totalWeightedScore += compContribution;
        }
    });

    totalWeightedScore = Math.min(totalWeightedScore, maxInternalMarks);
    const calculatedTotal = Math.round(totalWeightedScore * 100) / 100;

    const percentage = maxInternalMarks > 0 
        ? Math.round((calculatedTotal / maxInternalMarks) * 10000) / 100 
        : 0;

    const grade = getGrade(percentage);
    const isPass = calculatedTotal >= passingMarks;

    // Update table row cells
    const totalCell = row.querySelector(".calculated-total-cell");
    if (totalCell) totalCell.textContent = calculatedTotal.toFixed(2);

    const pctCell = row.querySelector(".percentage-cell");
    if (pctCell) pctCell.textContent = percentage.toFixed(2) + "%";

    const gradeCell = row.querySelector(".grade-cell");
    if (gradeCell) {
        gradeCell.innerHTML = `<span class="badge badge-grade grade-${grade.replace('+', 'plus')}">${grade}</span>`;
    }

    const statusCell = row.querySelector(".pass-status-cell");
    if (statusCell) {
        if (isPass) {
            statusCell.innerHTML = `<span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1"><i class="bi bi-check-circle me-1"></i>Pass</span>`;
        } else {
            statusCell.innerHTML = `<span class="badge bg-danger-subtle text-danger border border-danger-subtle px-2 py-1"><i class="bi bi-x-circle me-1"></i>Fail</span>`;
        }
    }
}

function getGrade(percentage) {
    if (percentage >= 90) return "O";
    if (percentage >= 80) return "A+";
    if (percentage >= 70) return "A";
    if (percentage >= 60) return "B+";
    if (percentage >= 50) return "B";
    if (percentage >= 40) return "C";
    return "F";
}
