import { Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { FileItem } from "../../models/file-item";
import { DashboardData } from "../../models/dashboard-data";

@Injectable({
    providedIn: 'root'
})
export class StatsService {

    constructor (private http: HttpClient) {}

    addUploadFilesStats(files: File[]) {
        const body = Array.from(files).map(f => ({
            type: "upload",
            name: f.name,
            size: f.size
        }));
        return this.http.post(`${environment.apiBaseUrl}/stats/add`, body);
    }

    addDownloadFileStats(fileItem: FileItem) {
        const body = [{
            name: fileItem.file.name,
            size: fileItem.file.size,
            type: "download"
          }
          ];
        return this.http.post(`${environment.apiBaseUrl}/stats/add`, body);
    }

    getDashboardData() {
        return this.http.get<DashboardData>(`${environment.apiBaseUrl}/dashboard/stats`);
    }

}