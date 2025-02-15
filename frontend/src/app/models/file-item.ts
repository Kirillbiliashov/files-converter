export class FileItem {
    public selectedFormat: string = "";
    public status = "Pending";
    public convertedBlob: Blob | null = null;
    constructor(
        public file: File,
        public supportedFormats: string[]
    ) {}
}