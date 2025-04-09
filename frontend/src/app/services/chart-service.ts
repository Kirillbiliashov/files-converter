import { Injectable } from "@angular/core";
import { DashboardData } from "../models/dashboard-data";
import { ArcElement, Chart, DoughnutController, Legend, Tooltip } from "chart.js";


@Injectable({
    providedIn: 'root'
})
export class ChartService {

    createChart(dashboardData: DashboardData) {
        Chart.register(ArcElement, DoughnutController, Tooltip, Legend);
        const ctx = document.getElementById('myChart') as HTMLCanvasElement;
        if (ctx) {
            return new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: Object.keys(dashboardData!.analytics),
                    datasets: [{
                        data: Object.values(dashboardData!.analytics)
                            .map(n => Number((n / dashboardData!.activity.length * 100).toFixed(1))),
                        backgroundColor: ['#4E79A7', '#F28E2B', '#E15759', '#76B7B2', '#59A14F']
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    },
                    cutout: '50%'
                }
            });
        }

        console.error("Canvas element not found!");
        return null;
    }

}