import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { FormatBytesPipe } from '../pipes/format-bytes-pipe';
import { FileItem } from '../models/file-item';

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

  removeInputFile() {
    // this.selectedFile = null;
  }

  private getFileExtension(filename: string): string {
    const parts = filename.split('.');
    return parts.length > 1 ? parts.pop()!.toLowerCase() : '';
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

}
