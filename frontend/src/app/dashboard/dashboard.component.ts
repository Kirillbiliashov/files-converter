import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AfterViewInit, Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Chart, ArcElement, DoughnutController, Tooltip, Legend } from 'chart.js';
import { DashboardData } from '../models/dashboard-data';
import { FormatBytesPipe } from '../pipes/format-bytes-pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormatBytesPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {

  dashboardData: DashboardData | null = null;
  mostConvertedFormat!: string;
  storageUsed!: number;
  averageFileSize!: number;
  mostActiveDay!: string;
  mostActiveTime!: string;
  chart!: Chart;
  rowsPerPage = 10;
  page: number = 0;

  constructor(private http: HttpClient) {

  }


  ngOnInit(): void {
    this.http.get<DashboardData>(`https://localhost:7099/api/dashboard/stats`)
      .subscribe({
        next: (response) => {
          console.log(`response:, `, response);
          this.dashboardData = response;
          this.mostConvertedFormat = this.getKeyWithMaxValue(this.dashboardData.analytics);
          this.storageUsed = this.dashboardData.activity.map(a => a.fileSize).reduce((a, b) => a + b);
          this.averageFileSize = this.storageUsed / this.dashboardData.activity.length;
          const activityDates = this.dashboardData.activity.map(a => a.date);
          this.mostActiveDay = this.getMostActiveDay(activityDates);
          this.mostActiveTime = this.getMostActiveTime(activityDates);

          setTimeout(() => {
            this.loadChart();
          }, 500);
        },
        error: (error) => {
          console.error('Error:', error);
        }
      })
  }

  getKeyWithMaxValue(dict: Record<string, number>) {
    return Object.entries(dict)
      .reduce((max, entry) =>
        entry[1] > dict[max] ? entry[0] : max, Object.keys(dict)[0]);
  }

  getMostActiveDay(dates: Date[]): string {
    const dayCounts = dates.reduce((acc, dateStr) => {
      const date = new Date(dateStr);
      const day = date.toLocaleDateString("en-US", { weekday: "long" }); 
      acc[day] = (acc[day] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return Object.keys(dayCounts).reduce((a, b) => (dayCounts[a] > dayCounts[b] ? a : b));
  }

  getMostActiveTime(dates: Date[]): string {
    const timeRanges = dates.reduce((acc, dateStr) => {
      const date = new Date(dateStr);
      const hour = date.getHours(); 

      const startHour = hour - (hour % 2); 
      const endHour = startHour + 2;

      const formatTime = (h: number) => {
        const suffix = h >= 12 ? "PM" : "AM";
        const hour12 = h % 12 || 12; 
        return `${hour12} ${suffix}`;
      };

      const range = `${formatTime(startHour)} - ${formatTime(endHour)}`;

      acc[range] = (acc[range] || 0) + 1; 
      return acc;
    }, {} as Record<string, number>);

    return Object.keys(timeRanges).reduce((a, b) => (timeRanges[a] > timeRanges[b] ? a : b));
  }


  loadChart() {
    Chart.register(ArcElement, DoughnutController, Tooltip, Legend);
    const ctx = document.getElementById('myChart') as HTMLCanvasElement;
    if (ctx) {
      this.chart = new Chart(ctx, {
        type: 'doughnut', 
        data: {
          labels: Object.keys(this.dashboardData!.analytics),
          datasets: [{
            data: Object.values(this.dashboardData!.analytics)
              .map(n => Number((n / this.dashboardData!.activity.length * 100).toFixed(1))),
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
    } else {
      console.error("Canvas element not found!");
    }
  }

    downloadFile(conversionId: string): void {
  
      this.http.post(`https://localhost:7099/api/convert/download/${conversionId}`, {}, { responseType: 'blob', observe: 'response' })
        .subscribe({
          next: (response) => {
            const blob: Blob = response.body as Blob;
  
            const contentDisposition = response.headers.get('Content-Disposition');
  
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = this.getDownloadFilename(contentDisposition ?? "");
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
          },
          error: () => { }
        })
    }

    private getDownloadFilename(contentDisposition: string): string {
      // Split the header into parts using ';' as a delimiter.
      const parts = contentDisposition.split(';').map(part => part.trim());
      // Look for the part that starts with 'filename=' but not 'filename*='
      const filenamePart = parts.find(part => part.startsWith('filename=') && !part.startsWith('filename*='));
  
      if (filenamePart) {
        // Remove the "filename=" part and strip any surrounding quotes.
        return filenamePart.replace(/^filename="?/, '').replace(/"?$/, '');
      }
  
      // Default filename if not found.
      return 'downloaded_file';
    }

    goToNextPage() {
      console.log(`going to next page`)
      this.page++;
    }

    goToPrevPage() {
      console.log(`going to prev page`)
      this.page--;
    }

}
