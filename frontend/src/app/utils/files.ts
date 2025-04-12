

export function downloadBlob(blob: Blob, filename: string | null | undefined) {

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download =  filename ?? "output";
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
}
