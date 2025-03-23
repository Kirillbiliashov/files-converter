export class FileItem {
    public selectedFormat: string = "Format";
    public status = "Pending";
    public conversionId: string | null = null;
    constructor(
        public file: File,
        public supportedFormats: string[]
    ) {}
}