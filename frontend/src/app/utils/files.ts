

export function downloadBlob(blob: Blob, contentDisposition: any) {

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = getDownloadFilename(contentDisposition ?? "");
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
}

function getDownloadFilename(contentDisposition: string): string {
    const parts = contentDisposition.split(';').map(part => part.trim());
    const filenamePart = parts.find(part => part.startsWith('filename=') && !part.startsWith('filename*='));

    if (filenamePart) {
      return filenamePart.replace(/^filename="?/, '').replace(/"?$/, '');
    }

    return 'downloaded_file';
  }