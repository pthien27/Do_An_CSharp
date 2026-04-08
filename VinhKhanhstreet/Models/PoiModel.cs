using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using SQLite;

namespace VinhKhanhstreet.Models
{
    public class PoiModel : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } // Khóa chính cho SQLite

        public string Name { get; set; }        // Tên quán (vd: Ốc Oanh)
        public double Latitude { get; set; }   // Kinh độ
        public double Longitude { get; set; }  // Vĩ độ
        public double Radius { get; set; }     // Bán kính kích hoạt (mét) - vd: 20m

        public string Description { get; set; } // Nội dung thuyết minh (Tiếng Việt)

        // --- DÒNG QUAN TRỌNG: Thêm dòng này để hết lỗi đỏ ---
        public string DescriptionEn { get; set; } // Nội dung thuyết minh (Tiếng Anh)
        public string DescriptionJa { get; set; } // Nội dung thuyết minh (Tiếng Nhật)
        public string DescriptionZh { get; set; } // Nội dung thuyết minh (Tiếng Trung)
        
        // Chuỗi hiển thị ở danh sách UI
        [Ignore]
        public string CurrentDisplayDescription 
        { 
            get => _currentDisplayDescription ?? Description; 
            set
            {
                if (_currentDisplayDescription != value)
                {
                    _currentDisplayDescription = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _currentDisplayDescription;

        public string AudioFile { get; set; }  // Tên file audio thu sẵn
        public int Priority { get; set; }      // Mức ưu tiên

        // Thuộc tính để chống spam
        public DateTime LastActivated { get; set; }
        
        [Ignore]
        public bool HasAutoPlayed { get; set; } = false;

        // --- CÁC THUỘC TÍNH MỚI CHO GIAO DIỆN GOOGLE MAPS CARD ---
        public double Rating { get; set; } = 4.5;
        public int ReviewCount { get; set; } = 100;
        public string ClosingTime { get; set; } = "22:30";
        public string PhoneNumber { get; set; } = "0901234567";
        
        // 5 Slot hình ảnh cho mỗi quán
        public string ImageUrl1 { get; set; } = "dotnet_bot.png";
        public string ImageUrl2 { get; set; } = "";
        public string ImageUrl3 { get; set; } = "";
        public string ImageUrl4 { get; set; } = "";
        public string ImageUrl5 { get; set; } = "";
        
        public string CategoryVi { get; set; } = "Nhà hàng Việt Nam";
        
        // Trạng thái yêu thích
        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set { 
                if (_isFavorite != value) {
                    _isFavorite = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(SaveBgColor));
                    OnPropertyChanged(nameof(SaveTextColor));
                }
            }
        }

        [Ignore]
        public string SaveBgColor => IsFavorite ? "#FFEBEE" : "#E3F2FD";
        [Ignore]
        public string SaveTextColor => IsFavorite ? "#D32F2F" : "#0D47A1";

        // --- CÁC THUỘC TÍNH CHỨA CHỮ ĐƯỢC DỊCH SẴN ---
        private double _distanceInMeters;
        [Ignore]
        public double DistanceInMeters
        {
            get => _distanceInMeters;
            set {
                if (_distanceInMeters != value) {
                    _distanceInMeters = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StrCategoryAndDistance));
                }
            }
        }

        private string _translatedCategory = "Quán ăn";
        [Ignore]
        public string TranslatedCategory
        {
            get => _translatedCategory;
            set {
                if (_translatedCategory != value) {
                    _translatedCategory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StrCategoryAndDistance));
                }
            }
        }

        [Ignore]
        public string StrCategoryAndDistance 
        { 
            get 
            {
                if (DistanceInMeters <= 0) return $"{TranslatedCategory}";
                return DistanceInMeters >= 1000 
                    ? $"{TranslatedCategory} · {(DistanceInMeters / 1000.0):F1} km" 
                    : $"{TranslatedCategory} · {DistanceInMeters:F0} m";
            }
        }

        private string _strStatusOpen = "Đang mở cửa";
        [Ignore]
        public string StrStatusOpen { get => _strStatusOpen; set { _strStatusOpen = value; OnPropertyChanged(); } }

        private string _strClosingTime = "Đóng cửa vào 22:30";
        [Ignore]
        public string StrClosingTime { get => _strClosingTime; set { _strClosingTime = value; OnPropertyChanged(); } }

        private string _strPlay = "Phát";
        [Ignore]
        public string StrPlay { get => _strPlay; set { _strPlay = value; OnPropertyChanged(); } }

        private string _strCall = "Gọi";
        [Ignore]
        public string StrCall { get => _strCall; set { _strCall = value; OnPropertyChanged(); } }

        private string _strSave = "Lưu";
        [Ignore]
        public string StrSave { get => _strSave; set { _strSave = value; OnPropertyChanged(); } }

        [Ignore]
        public string QrImageUrl => $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={Uri.EscapeDataString(Name ?? "")}";

        // Implement INotifyPropertyChanged để cập nhật tự động lên giao diện
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
