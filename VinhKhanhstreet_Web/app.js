// Cấu hình Firebase
const firebaseConfig = {
  apiKey: "AIzaSyAj0Lm-RYZ0_9BIqjxqJ5ja-QV4z5DXrFE",
  authDomain: "vinhkhanhstreet-dda5d.firebaseapp.com",
  projectId: "vinhkhanhstreet-dda5d",
  storageBucket: "vinhkhanhstreet-dda5d.firebasestorage.app",
  messagingSenderId: "250371727417",
  appId: "1:250371727417:web:921e00a706d83b6379ade8",
  measurementId: "G-XWP7S1PCGK"
};

// Khởi tạo Firebase
firebase.initializeApp(firebaseConfig);
const db = firebase.firestore();

// UI Elements
const resNameEl = document.getElementById("resName");
const resDescEl = document.getElementById("resDesc");
const langBtns = document.querySelectorAll(".lang-btn");
const playBtn = document.getElementById("playBtn");

// Speech Setup
const audioPlayer = document.getElementById("audioPlayer");
let currentLang = "vi";
let currentRestaurant = null;
let isPlaying = false;

// Bản đồ mã ngôn ngữ cho Google TTS
const langMap = {
    "vi": "vi",
    "en": "en",
    "zh": "zh-CN",
    "ja": "ja"
};

// --- LOGIC XỬ LÝ DỮ LIỆU TỪ FIREBASE ---
const urlParams = new URLSearchParams(window.location.search);
const resId = urlParams.get("id") || "R01";

async function loadRestaurantData(id) {
    try {
        console.log("Đang lấy dữ liệu từ Firebase cho ID:", id);
        
        // Truy vấn đến document trong collection "restaurants" có ID tương ứng
        const docRef = db.collection("restaurants").doc(id);
        const docSnap = await docRef.get();

        if (docSnap.exists) {
            currentRestaurant = docSnap.data();
            console.log("Dữ liệu tìm thấy:", currentRestaurant);
            updateUI();
        } else {
            console.warn("Không tìm thấy quán trên Firebase!");
            resNameEl.innerText = "Không tìm thấy quán";
            resDescEl.innerText = "Mô tả quán chưa có trên hệ thống Cloud.";
            playBtn.style.display = "none";
        }
    } catch (error) {
        console.error("Lỗi kết nối Firebase:", error);
        resNameEl.innerText = "Lỗi kết nối";
        resDescEl.innerText = "Không thể tải dữ liệu. Vui lòng kiểm tra lại Rules trên Firebase Console.";
    }
}

function updateUI() {
    if (!currentRestaurant) return;
    
    // Cập nhật Tên quán
    resNameEl.innerText = currentRestaurant.name || "Tên quán chưa cập nhật";
    
    // Cập nhật Mô tả (Ưu tiên cấu trúc đa ngôn ngữ)
    // Hỗ trợ cả 2 định dạng: { descriptions: { vi: "..." } } HOẶC các field DescriptionVI, DescriptionEN...
    let desc = "";
    if (currentRestaurant.descriptions) {
        desc = currentRestaurant.descriptions[currentLang] || currentRestaurant.descriptions["vi"];
    } else {
        const fieldKey = `Description${currentLang.toUpperCase()}`;
        desc = currentRestaurant[fieldKey] || currentRestaurant["DescriptionVI"];
    }
    
    resDescEl.innerText = desc || "Chưa có thuyết minh cho ngôn ngữ này.";
    
    if (isPlaying) stopReading();
}

// Xử lý nút ngôn ngữ
langBtns.forEach(btn => {
    btn.addEventListener("click", () => {
        langBtns.forEach(b => b.classList.remove("active"));
        btn.classList.add("active");
        currentLang = btn.getAttribute("data-lang");
        updateUI();
    });
});

// --- LOGIC ĐỌC VĂN BẢN (WEB SPEECH API - Tối ưu cho di động) ---
let speechUtterance = null;

// Hàm lấy giọng đọc chuẩn (hỗ trợ load không đồng bộ trên Chrome/Android)
function getBestVoice(langCode) {
    const voices = window.speechSynthesis.getVoices();
    if (voices.length === 0) return null;

    const targetLang = {
        "vi": "vi-VN",
        "en": "en-US",
        "zh": "zh-CN",
        "ja": "ja-JP"
    }[langCode] || "vi-VN";

    // 1. Tìm khớp hoàn toàn (ví dụ: vi-VN)
    let voice = voices.find(v => v.lang === targetLang || v.lang.replace('_', '-') === targetLang);
    
    // 2. Nếu không thấy, tìm giọng của Google (thường rất chuẩn)
    if (!voice) {
        voice = voices.find(v => v.name.includes("Google") && v.lang.startsWith(langCode));
    }

    // 3. Cuối cùng, tìm bất kỳ giọng nào chứa mã ngôn ngữ
    if (!voice) {
        voice = voices.find(v => v.lang.startsWith(langCode));
    }

    return voice;
}

function startReading() {
    const textToRead = resDescEl.innerText;
    window.speechSynthesis.cancel();

    speechUtterance = new SpeechSynthesisUtterance(textToRead);
    
    const selectedVoice = getBestVoice(currentLang);
    if (selectedVoice) {
        speechUtterance.voice = selectedVoice;
        speechUtterance.lang = selectedVoice.lang;
    } else {
        // Fallback lang if no voice object found yet
        speechUtterance.lang = currentLang === "vi" ? "vi-VN" : currentLang;
    }

    speechUtterance.rate = 1.0;
    speechUtterance.pitch = 1.0;
    speechUtterance.volume = 1.0;

    speechUtterance.onend = () => stopReading();
    speechUtterance.onerror = () => stopReading();

    window.speechSynthesis.speak(speechUtterance);
    
    isPlaying = true;
    playBtn.classList.add("playing");
    playBtn.innerHTML = '<i class="fa-solid fa-pause"></i>';
}

// Kích hoạt nạp giọng đọc sớm (Fix lỗi danh sách trống trên một số trình duyệt)
window.speechSynthesis.getVoices();
window.speechSynthesis.onvoiceschanged = () => {
    window.speechSynthesis.getVoices();
};

function stopReading() {
    window.speechSynthesis.cancel();
    isPlaying = false;
    playBtn.classList.remove("playing");
    playBtn.innerHTML = '<i class="fa-solid fa-play"></i>';
}

playBtn.addEventListener("click", () => {
    if (isPlaying) stopReading();
    else startReading();
});

audioPlayer.addEventListener("ended", () => {
    stopReading();
});

// Chạy khởi tạo
loadRestaurantData(resId);
