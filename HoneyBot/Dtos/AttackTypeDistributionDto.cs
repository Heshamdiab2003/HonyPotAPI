namespace HoneyBot.Dtos
{
    // نموذج لعرض توزيع أنواع الهجمات
    public class AttackTypeDistributionDto
    {
        public string AttackType { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
