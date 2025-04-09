import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { FileItem } from "../../models/file-item";
import { Conversion } from "../../models/dashboard-data";
import { environment } from "../../../environments/environment";

@Injectable({
    providedIn: 'root'
})
export class ConvertService {

    constructor(private http: HttpClient) { }

    convertFile(fileItem: FileItem) {
        const formData = new FormData();
        formData.append('file', fileItem.file);
        formData.append('outputFormat', fileItem.selectedFormat.toLowerCase());
        formData.append('filename', fileItem.newFilename ?? "");
    

        return this.http.post<{ conversion: Conversion }>(`${environment.apiBaseUrl}/convert`, formData)
    }

    convertFiles(selectedFiles: FileItem[]) {
        const formData = new FormData();

        const metadata = selectedFiles.map((fileItem, index) => ({
          fileName: fileItem.newFilename,
          outputFormat: fileItem.selectedFormat,
          id: fileItem.id
        }));
    
        selectedFiles.forEach(fileItem => {
          formData.append('files', fileItem.file, fileItem.file.name);
        });
    
        formData.append('metadata', JSON.stringify(metadata));
    
        return this.http.post<{ id: string, conversion: Conversion }[]>(`${environment.apiBaseUrl}//convert/all`, formData)
    }

    downloadConvertedFile(conversionId: string) {
        return this.http.post(`${environment.apiBaseUrl}/convert/download/${conversionId}`, {}, { responseType: 'blob', observe: 'response' })
    }

}