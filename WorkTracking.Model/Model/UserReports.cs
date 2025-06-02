namespace WorkTracking.Model.Model
{
    public enum Status
    {
        Seçiniz = 2,
        Hata = 0,
        Onay = 1,
        İzinli = 3,
        Raporlu = 4,
    }

    public class UserReports
    {
        public int UserReportsId { get; set; }
        public int UserId { get; set; }
        public DateTime? WorkStart { get; set; }
        public DateTime? WorkEnd { get; set; }
        public DateTime? LunchStart { get; set; }
        public DateTime? LunchEnd { get; set; }
        public TimeSpan? IdleTime { get; set; }
        public Status Status { get; set; }
        public DateTime ReportDate { get; set; }

        public User User { get; set; }
    }
}