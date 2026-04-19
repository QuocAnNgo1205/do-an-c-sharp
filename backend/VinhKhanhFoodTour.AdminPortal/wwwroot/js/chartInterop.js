window.chartInterop = {
    chart: null,
    
    renderTrendChart: function (elementId, labels, data) {
        var ctx = document.getElementById(elementId).getContext('2d');
        
        if (this.chart) {
            this.chart.destroy();
        }

        // Gradient cho line chart
        var gradient = ctx.createLinearGradient(0, 0, 0, 400);
        gradient.addColorStop(0, 'rgba(59, 130, 246, 0.5)'); // Tailwind blue-500
        gradient.addColorStop(1, 'rgba(59, 130, 246, 0.05)');

        this.chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Lượt nghe (Plays)',
                    data: data,
                    borderColor: 'rgb(59, 130, 246)',
                    backgroundColor: gradient,
                    borderWidth: 2,
                    tension: 0.4, // Tạo đường cong mềm mại
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: 'rgb(59, 130, 246)',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(17, 24, 39, 0.9)',
                        titleFont: { size: 14, family: "'Inter', sans-serif" },
                        bodyFont: { size: 14, family: "'Inter', sans-serif" },
                        padding: 12,
                        cornerRadius: 8,
                        displayColors: false
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false,
                            drawBorder: false
                        },
                        ticks: {
                            font: { family: "'Inter', sans-serif" },
                            color: '#6b7280'
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: '#f3f4f6',
                            drawBorder: false
                        },
                        ticks: {
                            precision: 0,
                            font: { family: "'Inter', sans-serif" },
                            color: '#6b7280'
                        }
                    }
                },
                interaction: {
                    intersect: false,
                    mode: 'index',
                },
            }
        });
    }
};

/* ── POI Analytics monthly bar chart ── */
window.poiAnalyticsChart = {
    _chart: null,
    render: function (elementId, labels, data) {
        var el = document.getElementById(elementId);
        if (!el) return;
        if (this._chart) { this._chart.destroy(); this._chart = null; }

        var ctx = el.getContext('2d');
        var grad = ctx.createLinearGradient(0, 0, 0, 300);
        grad.addColorStop(0, 'rgba(37,99,235,0.85)');
        grad.addColorStop(1, 'rgba(124,58,237,0.4)');

        this._chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Lượt nghe',
                    data: data,
                    backgroundColor: grad,
                    borderRadius: 6,
                    borderSkipped: false,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: 'rgba(15,23,42,0.92)',
                        titleFont: { size: 13, family: "'Inter',sans-serif" },
                        bodyFont:  { size: 13, family: "'Inter',sans-serif" },
                        padding: 10, cornerRadius: 8, displayColors: false,
                        callbacks: { label: ctx => ctx.parsed.y + ' lượt nghe' }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { family:"'Inter',sans-serif", size:11 }, color:'#64748b' }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color:'#f1f5f9' },
                        ticks: {
                            precision: 0,
                            font: { family:"'Inter',sans-serif", size:11 },
                            color:'#64748b'
                        }
                    }
                }
            }
        });
    }
};

/* ── Duration Analytics Bar Chart ── */
window.durationAnalyticsChart = {
    _chart: null,
    render: function (elementId, labels, data) {
        var el = document.getElementById(elementId);
        if (!el) return;
        if (this._chart) { this._chart.destroy(); this._chart = null; }

        var ctx = el.getContext('2d');
        var grad = ctx.createLinearGradient(0, 0, 0, 300);
        grad.addColorStop(0, 'rgba(16,185,129,0.85)'); // Emerald-500
        grad.addColorStop(1, 'rgba(5,150,105,0.4)');

        this._chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'TG nghe trung bình',
                    data: data,
                    backgroundColor: grad,
                    borderRadius: 6,
                }]
            },
            options: {
                indexAxis: 'y', // Biểu đồ ngang cho dễ đọc tên quán
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: 'rgba(15,23,42,0.92)',
                        callbacks: {
                            label: function(context) {
                                var val = context.parsed.x;
                                var m = Math.floor(val / 60);
                                var s = Math.floor(val % 60);
                                return 'Trung bình: ' + m + ':' + (s < 10 ? '0' + s : s);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { color:'#f1f5f9' },
                        ticks: {
                            callback: function(value) {
                                var m = Math.floor(value / 60);
                                return m + 'ph';
                            }
                        }
                    },
                    y: {
                        grid: { display: false }
                    }
                }
            }
        });
    }
};
