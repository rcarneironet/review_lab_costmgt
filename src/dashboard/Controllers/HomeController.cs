using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using services.APIs.CostManagement;
using services.Dtos;

namespace dashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel();
            model.WeeklyBillingList = CostManagementService.GetWeeklyBilling().Result;
            ViewData["WeeklyBilling"] = model.WeeklyBillingList;

            model.MonthToDateDtoList = CostManagementService.GetMonthToDateBilling().Result;
            ViewData["MonthToDate"] = model.MonthToDateDtoList;

            return View();
        }
    }
}
