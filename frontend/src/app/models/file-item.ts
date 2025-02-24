export class FileItem {
    public selectedFormat: string = "Format";
    public status = "Pending";
    public convertedBlob: Blob | null = null;
    constructor(
        public file: File,
        public supportedFormats: string[]
    ) {}
}