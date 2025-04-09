import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AfterViewInit, Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Chart, ArcElement, DoughnutController, Tooltip, Legend } from 'chart.js';
import { DashboardData } from '../models/dashboard-data';
import { FormatBytesPipe } from '../pipes/format-bytes-pipe';
import { StatsService } from '../services/http/stats-service';
import { ConvertService } from '../services/http/convert-service';
import { ChartService } from '../services/chart-service';
import { downloadBlob } from '../utils/files';

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
  rowsPerPage = 10;
  page: number = 0;

  constructor(
    private statsService: StatsService, 
    private convertService: ConvertService, 
    private chartService: ChartService) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.statsService.getDashboardData()
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
            this.chartService.createChart(this.dashboardData!);
          }, 500);
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });
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

    downloadFile(conversionId: string): void {
      this.convertService.downloadConvertedFile(conversionId)
        .subscribe({
          next: (response) => {
            const blob: Blob = response.body as Blob;
            const contentDisposition = response.headers.get('Content-Disposition');
            downloadBlob(blob, contentDisposition);
          },
          error: () => { }
        })
    }

    goToNextPage() {
      this.page++;
    }

    goToPrevPage() {
      this.page--;
    }

}
