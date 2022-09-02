using System;
using System.Collections.Generic;

namespace services.Dtos
{
    public class BillingDto
    {
        public DateTime Date { get; set; }
        public string SubscriptionId { get; set; }
        public double Value { get; set; }
        public bool IsUpdate { get; set; }

        public double PercentChanged { get; set; }
    }

    public class WeeklyBillingDto
    {
        public double Total { get; set; }
        public DateTime Date { get; set; }
    }

    public class MonthToDateDto
    {
        public string SubscriptionId { get; set; }
        public double Value { get; set; }
        public string Month { get; set; }
    }

    public class DashboardViewModel
    {
        public IEnumerable<WeeklyBillingDto> WeeklyBillingList { get; set; }
        public IEnumerable<MonthToDateDto> MonthToDateDtoList { get; set; }
    }

    public class BillingLogDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string SubscriptionId { get; set; }
        public double Value { get; set; }
        public double ValueChangePercent { get; set; }
        public bool IsEmailSent { get; set; }
    }
}
