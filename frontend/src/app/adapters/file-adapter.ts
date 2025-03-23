import { ICustomFile } from "../models/interfaces/custom-file";

export class FileAdapter {
    constructor(private customFile: ICustomFile, private blob: Blob) {}
  
    public toFile(): File {
      return new File([this.blob], this.customFile.name, {
        type: this.customFile.mimeType
      });
    }
  }