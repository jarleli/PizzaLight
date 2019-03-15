using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace PizzaLight.Controllers
{
    [Route("api")]
    [Route("")]
    public class ApiController: Controller
    {
        //private readonly PizzaCore _core;

        public ApiController()
        {
        }

        [HttpGet]
        public IActionResult Get()
        {
            //var connection = _core.SlackConnection.IsConnected;
            return Ok(new
            {
                application = "slack bot",
                version = Assembly.GetCallingAssembly().GetName().Version.ToString(),
                //activeConnection = connection
            });
        }
    }
}