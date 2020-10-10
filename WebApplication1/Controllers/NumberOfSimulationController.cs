using System;
using Microsoft.AspNetCore.Mvc;

using EFOP;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NumberOfSimulationController : ControllerBase
    {

        //POST: api/NumberOfSimulation
        // megmondja hogy hány szimuláció van lefuttatva
        [HttpGet]
        public String Get()
        {
            return $"{{\"simulationNumber\":\"{SimulationHandler.GetSimulationCount() }\"}}"; // "{"simulationNumber":"3"}
        }
    }
}