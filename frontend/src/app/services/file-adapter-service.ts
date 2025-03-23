import { Injectable } from "@angular/core";
import { ICustomFile } from "../models/interfaces/custom-file";
import { map, Observable } from "rxjs";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { FileAdapter } from "../adapters/file-adapter";

@Injectable({
    providedIn: 'root'
})
export class FileAdapterService {

    constructor(private http: HttpClient) { }

    adaptFile(customFile: ICustomFile, accessToken: string): Observable<File> {
        let url: string;

        if (this.isNativeGoogleFile(customFile.mimeType)) {
            const exportMimeType = this.getExportMimeType(customFile.mimeType);
            customFile.mimeType = exportMimeType;
            url = `https://www.googleapis.com/drive/v3/files/${customFile.id}/export?mimeType=${encodeURIComponent(exportMimeType)}`;
        } else {
            url = `https://www.googleapis.com/drive/v3/files/${customFile.id}?alt=media`;
        }

        const headers = new HttpHeaders({
            'Authorization': `Bearer ${accessToken}`
        });

        return this.http.get(url, { headers, responseType: 'blob' }).pipe(
            map(blob => {
                const adapter = new FileAdapter(customFile, blob);
                return adapter.toFile();
            })
        );
    }

    private isNativeGoogleFile(mimeType: string): boolean {
        const nativeMimeTypes = [
            'application/vnd.google-apps.document',
            'application/vnd.google-apps.spreadsheet',
            'application/vnd.google-apps.presentation',
            'application/vnd.google-apps.drawing'
        ];
        return nativeMimeTypes.includes(mimeType);
    }


    private getExportMimeType(nativeMimeType: string): string {
        switch (nativeMimeType) {
          case 'application/vnd.google-apps.document':
            return 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
          case 'application/vnd.google-apps.spreadsheet':
            return 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
          case 'application/vnd.google-apps.presentation':
            return 'application/vnd.openxmlformats-officedocument.presentationml.presentation';
          case 'application/vnd.google-apps.drawing':
            return 'image/png';
          default:
            return 'application/pdf';
        }
      }
}