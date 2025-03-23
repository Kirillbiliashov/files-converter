import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule, HttpHeaders } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { FormatBytesPipe } from '../pipes/format-bytes-pipe';
import { FileItem } from '../models/file-item';
import * as JSZip from 'jszip';
import { GooglePickerService } from '../services/google-picker-service';
import { FileAdapterService } from '../services/file-adapter-service';

@Component({
  selector: 'app-convert',
  standalone: true,
  imports: [CommonModule, FormsModule, FormatBytesPipe],
  templateUrl: './convert.component.html',
  styleUrl: './convert.component.css'
})
export class ConvertComponent implements OnInit {
  selectedFiles: FileItem[] = [];
  fileBlob: Blob | null = null;
  private allConvertFormats = ["PDF", "DOCX", "CSV", "XLSX", "TXT", "RTF", "HTML", "EPUB", "PNG", "JPG"];
  selectedFileName: string | undefined;
  private accessToken!: string;

  constructor(
    private http: HttpClient,
    private googlePickerService: GooglePickerService,
    private fileAdapterService: FileAdapterService,
    private cdr: ChangeDetectorRef) { }

  async ngOnInit() {

    this.http.get<{ accessToken: string }>(`https://localhost:7099/api/access-token?provider=Google`)
      .subscribe({
        next: async (response) => {
          this.accessToken = response.accessToken;
          await this.googlePickerService.loadPicker();
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });

    this.googlePickerService.fileSelected$.subscribe((googleFile) => {
      console.log('File selected in component:', googleFile);

      this.fileAdapterService.adaptFile(googleFile, this.accessToken)
        .subscribe({
          next: blobFile => {
            this.selectedFiles.push(...Array.from([blobFile]).map(f =>
              new FileItem(f, this.getSupportedFormats(f.name))
            ));
            this.cdr.detectChanges();
            console.log(`selected files, `, this.selectedFiles);
          },
          error: err => {
            console.error('Error fetching file:', err);
          }
        });

    });
  }

  selectGoogleFile() {
    this.googlePickerService.createPicker(this.accessToken);
  }

  onFileSelected(event: Event) {
    console.log(`on file selected`)
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.selectedFiles.push(...Array.from(input.files).map(f =>
        new FileItem(f, this.getSupportedFormats(f.name))
      ));
      console.log(`selected file`)

      const body = Array.from(input.files).map(f => ({
        type: "upload",
        name: f.name,
        size: f.size
      }));
      this.http.post(`https://localhost:7099/api/stats/add`, body).subscribe();
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

    const body = [{
      name: fileItem.file.name,
      size: fileItem.file.size,
      type: "download"
    }
    ];
    this.http.post(`https://localhost:7099/api/stats/add`, body).subscribe();
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
