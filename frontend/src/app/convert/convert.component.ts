import { CommonModule } from '@angular/common';
import { HttpClient, HttpClientModule, HttpHeaders } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { FormatBytesPipe } from '../pipes/format-bytes-pipe';
import { FileItem } from '../models/file-item';
import { GooglePickerService } from '../services/google-picker-service';
import { FileAdapterService } from '../services/file-adapter-service';
import { ConversionResult } from '../models/conversion-result';
import { Conversion } from '../models/dashboard-data';

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
            this.processSelectedFiles([blobFile]);
            this.cdr.detectChanges();
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
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.processSelectedFiles(Array.from(input.files));
    }
  }

  private processSelectedFiles(files: File[]) {
    this.selectedFiles.push(...files.map(f =>
      new FileItem(f, this.getSupportedFormats(f.name))
    ));
    console.log(`selected files (device), `, this.selectedFiles);

    const body = Array.from(files).map(f => ({
      type: "upload",
      name: f.name,
      size: f.size
    }));
    this.http.post(`https://localhost:7099/api/stats/add`, body).subscribe();
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

  convertFile(fileItem: FileItem) {
    const formData = new FormData();
    formData.append('file', fileItem.file);
    formData.append('outputFormat', fileItem.selectedFormat.toLowerCase());
    fileItem.status = "Converting";

    this.http.post<{conversion: Conversion}>(`https://localhost:7099/api/convert`, formData)
      .subscribe({
        next: (response) => {
          fileItem.status = response.conversion.status  == "success" ?  "Completed" : "Failed";
          fileItem.conversion = response.conversion;
          console.log(`conversion id,  `, response.conversion.idInternal);
          // fileItem.convertedBlob = response.body;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });
  }

  downloadFile(fileItem: FileItem): void {
    if (!fileItem.conversion?.idInternal) return;

    this.http.post(`https://localhost:7099/api/convert/download/${fileItem.conversion.idInternal}`, {}, { responseType: 'blob', observe: 'response'})
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

        const body = [{
          name: fileItem.file.name,
          size: fileItem.file.size,
          type: "download"
        }
        ];
        this.http.post(`https://localhost:7099/api/stats/add`, body).subscribe();
      },
      error: () => {}
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
  


  deleteFile(fileItem: FileItem) {
    this.selectedFiles = this.selectedFiles.filter(f => f != fileItem);
  }

  convertAllFiles() {
    const formData = new FormData();

    const metadata = this.selectedFiles.map((fileItem, index) => ({
      fileName: fileItem.file.name,
      outputFormat: fileItem.selectedFormat,
      id: fileItem.id
    }));

    this.selectedFiles.forEach(fileItem => {
      formData.append('files', fileItem.file, fileItem.file.name);
      fileItem.status = "Converting";
    });

    formData.append('metadata', JSON.stringify(metadata));

    this.http.post<{id: string, conversion: Conversion}[]>(`https://localhost:7099/api/convert/all`, formData)
      .subscribe({
        next: (response) => {
          console.log(`convert all response, `, response);
          response.forEach(r => {
            const file = this.selectedFiles.find(f => f.id == r.id);
            if (file) {
              file.conversion = r.conversion;
            }
          })
          this.selectedFiles.forEach(f => {
            f.status = "Completed";
          })
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });

  }


}
