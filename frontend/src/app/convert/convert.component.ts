import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-convert',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './convert.component.html',
  styleUrl: './convert.component.css'
})
export class ConvertComponent {
  selectedFiles: File[] = [];
  fileBlob: Blob | null = null;
  selectedFormat: string = "Format";

  constructor(private http: HttpClient) {}

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      for (let i = 0; i < input.files.length; i++) {
        this.selectedFiles.push(input.files[i]);
      }
    }
  }

  convertFiles() {
    const formData = new FormData();
    formData.append('file', this.selectedFiles[0]); 
    formData.append('outputFormat', this.selectedFormat.toLowerCase());

    this.http.post(`https://localhost:7099/api/convert`, formData, {responseType: 'blob'})
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
      const fileName = `converted.${this.selectedFormat}`; 
      const link = document.createElement('a');
      const url = window.URL.createObjectURL(this.fileBlob);
      link.href = url;
      link.download = fileName;
      link.click();
      window.URL.revokeObjectURL(url);
    } 
  }

}
