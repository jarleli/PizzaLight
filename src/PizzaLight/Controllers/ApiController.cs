using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using PizzaLight.Resources;

namespace PizzaLight.Controllers
{
    [Route("")]
    [Route("api")]
    public class ApiController : Controller
    {
        private readonly IPizzaCore _core;
        private readonly PizzaPlanner _planner;

        public ApiController(IPizzaCore core, PizzaPlanner planner)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core)); ;
            _planner = planner ?? throw new ArgumentNullException(nameof(planner)); ;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                application = "pizzalight",
                version = Assembly.GetCallingAssembly().GetName().Version.ToString(),
                activeConnection = _core.IsConnected,
                ActivePlans = _planner.PizzaPlans
            });
        }
    }
}