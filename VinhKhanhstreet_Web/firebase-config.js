// Import các hàm cần thiết từ Firebase SDK (Modular v10)
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.1/firebase-app.js";
import { getFirestore } from "https://www.gstatic.com/firebasejs/10.8.1/firebase-firestore.js";

// Cấu hình Firebase của bạn
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
const app = initializeApp(firebaseConfig);

// Khởi tạo Firestore (Cơ sở dữ liệu đám mây)
const db = getFirestore(app);

// Xuất ra để các file khác (như app.js) có thể dùng
export { db };
