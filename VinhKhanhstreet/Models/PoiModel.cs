using System;
using System.Collections.Generic;
using System.Text;

namespace VinhKhanhstreet.Models
{
    public class PoiModel
    {
        public string Name { get; set; }        // Tên quán (vd: Ốc Oanh)
        public double Latitude { get; set; }   // Kinh độ
        public double Longitude { get; set; }  // Vĩ độ
        public double Radius { get; set; }     // Bán kính kích hoạt (mét) - vd: 20m
        public string Description { get; set; } // Nội dung thuyết minh
        public string AudioFile { get; set; }  // Tên file audio thu sẵn
        public int Priority { get; set; }      // Mức ưu tiên

        // Thuộc tính để chống spam (Yêu cầu 2)
        public DateTime LastActivated { get; set; }
    }
}
