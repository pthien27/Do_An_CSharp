using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;
using Plugin.CloudFirestore.Attributes;

namespace VinhKhanhstreet.Models
{
    public class PoiModel : INotifyPropertyChanged
    {
        [Id]
        public string DocumentId { get; set; } // ID dùng cho Firebase (R01, R02...)

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } // Giữ lại cho tương thích SQLite nếu cần

        [MapTo("name")]
        public string Name { get; set; }        // Tên quán

        [MapTo("Latitude")]
        public double Latitude { get; set; }   // Kinh độ
        
        [MapTo("Longitude")]
        public double Longitude { get; set; }  // Vĩ độ
        
        [MapTo("Radius")]
        public double Radius { get; set; }     // Bán kính

        [MapTo("DescriptionVI")]
        public string Description { get; set; } 

        [MapTo("DescriptionEN")]
        public string DescriptionEn { get; set; } 

        [MapTo("DescriptionJA")]
        public string DescriptionJa { get; set; } 

        [MapTo("DescriptionZH")]
        public string DescriptionZh { get; set; } 
        
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

        [MapTo("AudioFile")]
        public string AudioFile { get; set; }  // Tên file audio thu sẵn
        
        [MapTo("Priority")]
        public int Priority { get; set; }      // Mức ưu tiên

        // Thuộc tính để chống spam
        [MapTo("LastActivated")]
        public DateTime LastActivated { get; set; } = DateTime.Now;
        
        [Ignore]
        public bool HasAutoPlayed { get; set; } = false;

        // --- CÁC THUỘC TÍNH MỚI CHO GIAO DIỆN GOOGLE MAPS CARD ---
        [MapTo("Rating")]
        public double Rating { get; set; } = 4.5;
        
        [MapTo("ReviewCount")]
        public int ReviewCount { get; set; } = 100;
        
        [MapTo("ClosingTime")]
        public string ClosingTime { get; set; } = "22:30";
        
        [MapTo("PhoneNumber")]
        public string PhoneNumber { get; set; } = "0901234567";
        
        // 5 Slot hình ảnh cho mỗi quán
        [MapTo("ImageUrl1")]
        public string ImageUrl1 { get; set; } = "dotnet_bot.png";
        
        [MapTo("ImageUrl2")]
        public string ImageUrl2 { get; set; } = "";
        
        [MapTo("ImageUrl3")]
        public string ImageUrl3 { get; set; } = "";
        
        [MapTo("ImageUrl4")]
        public string ImageUrl4 { get; set; } = "";
        
        [MapTo("ImageUrl5")]
        public string ImageUrl5 { get; set; } = "";
        
        [MapTo("CategoryVi")]
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
