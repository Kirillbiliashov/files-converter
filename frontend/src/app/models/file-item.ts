import { Conversion } from "./dashboard-data";
import { v4 as uuidv4 } from 'uuid';

export class FileItem {
    public selectedFormat: string = "Format";
    public status = ConversionStatus.Pending;
    public newFilename: string | null = null;
    public conversion: Conversion | null = null;
    public id: string = uuidv4();
    constructor(
        public file: File,
        public supportedFormats: string[]
    ) {}
}

export enum ConversionStatus {
    Pending = 'Pending',
    Converting = 'Converting',
    Completed = 'Completed',
    Failed = 'Failed'
}