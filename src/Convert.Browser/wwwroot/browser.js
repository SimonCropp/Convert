// The Browser pack's JavaScript, loaded as an ES module by CanvasBridge. Images are drawn on a canvas and
// re-encoded with the browser's own codecs. Bytes cross from .NET as a Uint8Array and back the same way.

// An SVG drawn through <img> needs explicit dimensions: take them from width/height, else the viewBox.
// A <foreignObject> taints the canvas (toBlob then throws), so SVG that embeds HTML cannot be rasterised.
async function loadSvg(bytes) {
    const text = new TextDecoder().decode(bytes);
    const parsed = new DOMParser().parseFromString(text, 'image/svg+xml');
    const svg = parsed.documentElement;
    if (svg.nodeName !== 'svg') {
        throw new Error('The file is not a valid SVG image.');
    }

    let width = parseFloat(svg.getAttribute('width'));
    let height = parseFloat(svg.getAttribute('height'));
    const viewBox = svg.getAttribute('viewBox');
    if ((!width || !height) && viewBox) {
        const parts = viewBox.split(/[\s,]+/).map(Number);
        width = parts[2];
        height = parts[3];
    }
    width = Math.max(1, Math.round(width || 300));
    height = Math.max(1, Math.round(height || 150));
    svg.setAttribute('width', width);
    svg.setAttribute('height', height);

    const serialized = new XMLSerializer().serializeToString(svg);
    const url = URL.createObjectURL(new Blob([serialized], { type: 'image/svg+xml;charset=utf-8' }));
    try {
        const image = new Image();
        image.width = width;
        image.height = height;
        await new Promise((resolve, reject) => {
            image.onload = () => resolve();
            image.onerror = () => reject(new Error('The browser could not draw this SVG.'));
            image.src = url;
        });
        return { source: image, width, height };
    } finally {
        URL.revokeObjectURL(url);
    }
}

async function loadRaster(bytes, contentType) {
    try {
        const bitmap = await createImageBitmap(new Blob([bytes], { type: contentType }));
        return { source: bitmap, width: bitmap.width, height: bitmap.height };
    } catch {
        throw new Error('The browser could not decode this image.');
    }
}

export async function rasterize(bytes, sourceType, targetType, scale, background, quality) {
    const image = sourceType === 'image/svg+xml'
        ? await loadSvg(bytes)
        : await loadRaster(bytes, sourceType);

    const width = Math.max(1, Math.round(image.width * scale));
    const height = Math.max(1, Math.round(image.height * scale));
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext('2d');
    if (background) {
        context.fillStyle = background;
        context.fillRect(0, 0, width, height);
    }
    context.drawImage(image.source, 0, 0, width, height);
    image.source.close?.();

    const blob = await new Promise((resolve, reject) => {
        try {
            canvas.toBlob(
                result => result ? resolve(result) : reject(new Error(`This browser cannot encode ${targetType}.`)),
                targetType,
                quality);
        } catch (error) {
            reject(error);
        }
    });

    // Safari silently falls back to PNG for a type it cannot encode (WebP before Safari 17).
    if (blob.type !== targetType) {
        throw new Error(`This browser cannot encode ${targetType}; it produced ${blob.type} instead.`);
    }

    return new Uint8Array(await blob.arrayBuffer());
}
