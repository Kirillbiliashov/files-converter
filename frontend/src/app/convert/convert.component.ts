import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-convert',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './convert.component.html',
  styleUrl: './convert.component.css'
})
export class ConvertComponent {
  selectedFile: File | undefined;
  fileBlob: Blob | null = null;
  selectedFormat: string = "Format";
  convertFormats = ["PDF", "DOCX", "CSV", "XLSX", "TXT", "RTF", "HTML", "EPUB", "PNG", "JPG"];
  convertingFile = false;
  selectedFileName: string | undefined;

  constructor(private http: HttpClient) { }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.selectedFile = input.files[0];
      this.selectedFileName = this.selectedFile.name.split(".").shift();
      this.convertFormats = this.convertFormats.filter(f => f.toLowerCase() != this.getFileExtension(this.selectedFile!.name));
    }
  }

  private getFileExtension(filename: string): string {
    const parts = filename.split('.');
    return parts.length > 1 ? parts.pop()!.toLowerCase() : '';
  }

  convertFiles() {
    const formData = new FormData();
    formData.append('file', this.selectedFile!);
    formData.append('outputFormat', this.selectedFormat.toLowerCase());
    this.convertingFile = true;

    this.http.post(`https://localhost:7099/api/convert`, formData, { responseType: 'blob' })
      .pipe(finalize(() => {
        this.convertingFile = false;
      }))
      .subscribe({
        next: (response) => {
          this.fileBlob = response;
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });
  }

  downloadFile(): void {
    if (this.fileBlob) {
      const format = this.fileBlob.type == "application/zip" ? "zip" : this.selectedFormat.toLowerCase();
      const fileName = `${this.selectedFileName}.${format}`;
      const link = document.createElement('a');
      const url = window.URL.createObjectURL(this.fileBlob);
      link.href = url;
      link.download = fileName;
      link.click();
      window.URL.revokeObjectURL(url);
    }
  }

}
