using System;
using Microsoft.AspNetCore.Mvc;
using PizzaLight.Infrastructure;
using PizzaLight.Resources;

namespace PizzaLight.Controllers
{
    public class ActivityController:Controller
    {
        private readonly IActivityLog _log;

        public ActivityController(IActivityLog log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));

        }

        [Route("activity")]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                _log.Activities
            });
        }

    }
}