// ===============================================
// 📦 共通：画像圧縮＋Supabaseアップロード関数
// ===============================================

//async function uploadImage(file) {
//    const maxWidth = 1200, maxHeight = 1200, quality = 0.8;
//    const img = await loadImage(file);
//    const canvas = document.createElement("canvas");
//    let { width, height } = img;

//    // サイズ調整
//    if (width > maxWidth || height > maxHeight) {
//        const scale = Math.min(maxWidth / width, maxHeight / height);
//        width = Math.round(width * scale);
//        height = Math.round(height * scale);
//    }

//    canvas.width = width;
//    canvas.height = height;
//    const ctx = canvas.getContext("2d");
//    ctx.drawImage(img, 0, 0, width, height);

//    const blob = await new Promise(r => canvas.toBlob(r, "image/jpeg", quality));
//    const formData = new FormData();
//    formData.append("file", new File([blob], file.name.replace(/\.[^.]+$/, ".jpg"), { type: "image/jpeg" }));

//    const response = await fetch("/Diary/UploadImage", { method: "POST", body: formData });
//    if (!response.ok) throw new Error(await response.text());
//    const result = await response.json();
//    return result.imageUrl;
//}

//function loadImage(file) {
//    return new Promise((resolve, reject) => {
//        const img = new Image();
//        img.onload = () => resolve(img);
//        img.onerror = reject;
//        img.src = URL.createObjectURL(file);
//    });
//}

// ✅ WebP対応＋リサイズ圧縮付きアップロード関数
async function uploadImage(file) {
    const maxWidth = 1200;
    const maxHeight = 1200;
    const quality = 0.8; // ← JPEG換算でほぼ劣化なし＆容量約60〜70%

    // 🔸 画像読み込み
    const img = await loadImage(file);
    let { width, height } = img;

    // 🔸 リサイズ（縦横どちらかが上限を超える場合）
    if (width > maxWidth || height > maxHeight) {
        const scale = Math.min(maxWidth / width, maxHeight / height);
        width = Math.round(width * scale);
        height = Math.round(height * scale);
    }

    // 🔸 Canvasに描画
    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(img, 0, 0, width, height);

    // 🔸 WebP形式でBlob生成
    const blob = await new Promise(resolve =>
        canvas.toBlob(resolve, "image/webp", quality)
    );

    // 🔸 拡張子を .webp に統一してFormData化
    const fileName = file.name.replace(/\.[^.]+$/, ".webp");
    const webpFile = new File([blob], fileName, { type: "image/webp" });

    const formData = new FormData();
    formData.append("file", webpFile);

    // 🔸 Supabase経由でアップロード
    const response = await fetch("/Diary/UploadImage", {
        method: "POST",
        body: formData
    });

    if (!response.ok) throw new Error(await response.text());

    const result = await response.json();
    return result.imageUrl;
}

// ✅ 画像読み込み補助関数（Promise化）
function loadImage(file) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = reject;
        img.src = URL.createObjectURL(file);
    });
}
