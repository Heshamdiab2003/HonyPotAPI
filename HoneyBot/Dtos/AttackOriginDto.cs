namespace HoneyBot.Dtos
{

    public class AttackOriginDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string AttackType { get; set; } // لمعرفة نوع الهجوم على الخريطة
        public string City { get; set; }
    }
}
