import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient, HttpClientModule, HttpHeaders } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { FormatBytesPipe } from '../pipes/format-bytes-pipe';
import { ConversionStatus, FileItem } from '../models/file-item';
import { GooglePickerService } from '../services/google-picker-service';
import { FileAdapterService } from '../services/file-adapter-service';
import { ConversionResult } from '../models/conversion-result';
import { Conversion } from '../models/dashboard-data';
import { AuthService } from '../services/auth-service';
import { formatDate } from '@angular/common';
import { environment } from '../../environments/environment';
import { ConvertService } from '../services/http/convert-service';
import { StatsService } from '../services/http/stats-service';
import { downloadBlob } from '../utils/files';

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
  accessToken: string | null = null;
  displayRenameInfo = false;
  renamePattern = "converted_{name}_{index}";
  Status = ConversionStatus;

  constructor(
    private statsService: StatsService,
    private convertService: ConvertService,
    private googlePickerService: GooglePickerService,
    private fileAdapterService: FileAdapterService,
    private cdr: ChangeDetectorRef,
    private authService: AuthService) { }

  async ngOnInit() {
    this.loadGoogleAccessToken();
    this.listenGooglePicker();
  }

  listenGooglePicker() {
    this.googlePickerService.fileSelected$.subscribe((googleFile) => {
      console.log('File selected in component:', googleFile);

      this.fileAdapterService.adaptFile(googleFile, this.accessToken!)
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

  loadGoogleAccessToken() {
    this.authService.getGoogleAccessToken()
      .subscribe({
        next: async (response) => {
          this.accessToken = response.accessToken;
          await this.googlePickerService.loadPicker();
        },
        error: (error) => {
          console.log(`error, ${error}`)
          this.accessToken = null;
        }
      });
  }

  selectGoogleFile() {
    this.googlePickerService.createPicker(this.accessToken!);
  }

  loginWithGoogle() {
    window.location.href = environment.apiBaseUrl + '/auth/login/oauth?provider=Google';
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


    if (this.authService.getCurrentUser()) {
      this.statsService.addUploadFilesStats(files).subscribe();
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

  convertFile(fileItem: FileItem) {
    fileItem.status = ConversionStatus.Converting;
    this.convertService.convertFile(fileItem)
      .subscribe({
        next: (response) => {
          fileItem.status = response.conversion.status == "success" ? ConversionStatus.Completed : ConversionStatus.Failed;
          fileItem.conversion = response.conversion;
          console.log(`conversion,  `, response.conversion);
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });
  }

  downloadFile(fileItem: FileItem): void {
    if (!fileItem.conversion?.idInternal) return;

    this.convertService.downloadConvertedFile(fileItem.conversion.idInternal)
      .subscribe({
        next: (response) => {
          const blob: Blob = response.body as Blob;
          const filename = fileItem.conversion?.outputUrl.split('/').pop();
          downloadBlob(blob, filename);

          if (this.authService.getCurrentUser()) {
            this.statsService.addDownloadFileStats(fileItem).subscribe();
          }

        },
        error: () => { }
      })
  }

  deleteFile(fileItem: FileItem) {
    this.selectedFiles = this.selectedFiles.filter(f => f != fileItem);
  }

  convertAllFiles() {
    this.selectedFiles.forEach(f => f.status = ConversionStatus.Converting);
    this.convertService.convertFiles(this.selectedFiles)
      .subscribe({
        next: (response) => {
          response.forEach(r => {
            const file = this.selectedFiles.find(f => f.id == r.id);
            if (file) {
              file.conversion = r.conversion;
            }
          })
           
          this.selectedFiles.forEach(f => f.status = ConversionStatus.Completed);
        },
        error: (error) => {
          console.error('Error:', error);
        }
      });

  }

  updateRenamePattern(pattern: string) {
    this.renamePattern += pattern;
  }

  applyRename() {
    const currentDate = new Date();
    const formattedDate = formatDate(currentDate, 'yyyy-MM-dd', 'en-US');
    const formattedTime = formatDate(currentDate, 'HH:mm:ss', 'en-US');

    this.selectedFiles.forEach((f, idx) => {
      const parts = f.file.name.split('.');
      const ext = parts.pop();
      const filename = parts.join('.');

      f.newFilename = this.renamePattern
        .replace(/{index}/g, (idx + 1).toString())
        .replace(/{date}/g, formattedDate)
        .replace(/{time}/g, formattedTime)
        .replace(/{ext}/g, ext!)
        .replace(/{name}/g, filename);
    });
  }


}
