import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { FormatBytesPipe } from '../pipes/format-bytes-pipe';
import { FileItem } from '../models/file-item';
import * as JSZip from 'jszip';

@Component({
  selector: 'app-convert',
  standalone: true,
  imports: [CommonModule, FormsModule, FormatBytesPipe],
  templateUrl: './convert.component.html',
  styleUrl: './convert.component.css'
})
export class ConvertComponent {
  selectedFiles: FileItem[] = [];
  fileBlob: Blob | null = null;
  private allConvertFormats = ["PDF", "DOCX", "CSV", "XLSX", "TXT", "RTF", "HTML", "EPUB", "PNG", "JPG"];
  selectedFileName: string | undefined;

  constructor(private http: HttpClient) { }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.selectedFiles.push(...Array.from(input.files).map(f => 
        new FileItem(f, this.getSupportedFormats(f.name))
      ));
    }
  }


  getSupportedFormats(filename: string) {
    const fileExt = this.getFileExtension(filename);
    let convertFormats: string[] = [];

    if (["CSV", "XLSX"].includes(fileExt.toUpperCase())) {
      convertFormats = this.allConvertFormats.filter(f => f === "CSV" || f === "XLSX");
    } else {
      convertFormats = this.allConvertFormats.filter(f => f !== "CSV" && f !== "XLSX");
    }
    convertFormats = convertFormats.filter(f => f.toLowerCase() != fileExt.toLowerCase());

    return convertFormats;
  }

  private getFileExtension(filename: string): string {
    const parts = filename.split('.');
    return parts.length > 1 ? parts.pop()!.toLowerCase() : '';
  }

  private getFileNameWithoutExt(filename: string): string {
    const parts = filename.split('.');
    return parts.length > 1 ? parts.shift()!.toLowerCase() : '';
  }

  convertFile(fileItem: FileItem) {
    const formData = new FormData();
    formData.append('file', fileItem.file);
    formData.append('outputFormat', fileItem.selectedFormat.toLowerCase());
    fileItem.status = "Converting";

    this.http.post(`https://localhost:7099/api/convert`, formData, { responseType: 'blob', observe: 'response' })
      .subscribe({
        next: (response) => {
          fileItem.status = "Completed";
          console.log(`content disposition:`, response.headers.get('Content-Disposition'));
          fileItem.convertedBlob = response.body;
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });
  }

  downloadFile(fileItem: FileItem): void {
    if (!fileItem.convertedBlob) return;

    const format = fileItem.convertedBlob.type == "application/zip" ? "zip" : fileItem.selectedFormat.toLowerCase();
    console.log(`file format: ${format}, file name: ${fileItem.file.name}`);
    const fileName = `${fileItem.file.name.split('.').shift()}.${format}`;
    const link = document.createElement('a');
    const url = window.URL.createObjectURL(fileItem.convertedBlob);
    link.href = url;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  deleteFile(fileItem: FileItem) {
    this.selectedFiles = this.selectedFiles.filter(f => f != fileItem);
  }

  convertAllFiles() {
    const formData = new FormData();

    const metadata = this.selectedFiles.map((fileItem, index) => ({
      fileName: fileItem.file.name,
      outputFormat: fileItem.selectedFormat
    }));
  
    this.selectedFiles.forEach(fileItem => {
      formData.append('files', fileItem.file, fileItem.file.name);
      fileItem.status = "Converting";
    });

    formData.append('metadata', JSON.stringify(metadata));

    this.http.post(`https://localhost:7099/api/convert/all`, formData, { responseType: 'blob', observe: 'response' })
      .subscribe({
        next: (response) => {
          if (response.body) {
            JSZip.loadAsync(response.body).then(zip => {
              Object.keys(zip.files).forEach(async fileName => {
                const fileItem = this.selectedFiles.find(f => 
                  this.getFileNameWithoutExt(f.file.name) == this.getFileNameWithoutExt(fileName));
                  if (fileItem) {
                    fileItem.convertedBlob = await zip.files[fileName].async('blob');
                    fileItem.status = "Converted";
                  }
              });
            }).catch(error => {
              console.error('Error reading ZIP:', error);
            });
          }
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });

  }


}
