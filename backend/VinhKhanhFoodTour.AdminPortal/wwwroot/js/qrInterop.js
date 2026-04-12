/**
 * qrInterop.js
 * Helper JS cho tính năng tải file QR Code từ Blazor.
 * Được gọi qua IJSRuntime.InvokeVoidAsync("downloadBase64File", ...)
 */

/**
 * Tạo một <a> element ẩn và trigger download file từ Base64 string.
 * @param {string} base64Data  - Chuỗi Base64 không có prefix "data:..."
 * @param {string} fileName    - Tên file khi tải về (VD: "QR_Ten_Quan_1.png")
 * @param {string} mimeType    - MIME type (VD: "image/png")
 */
function downloadBase64File(base64Data, fileName, mimeType) {
    try {
        const byteChars = atob(base64Data);
        const byteNums = new Array(byteChars.length);
        for (let i = 0; i < byteChars.length; i++) {
            byteNums[i] = byteChars.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNums);
        const blob = new Blob([byteArray], { type: mimeType });

        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        // Giải phóng bộ nhớ sau 60 giây
        setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch (e) {
        console.error('[qrInterop] Download failed:', e);
    }
}
