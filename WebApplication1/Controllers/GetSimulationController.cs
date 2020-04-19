using System;
using Microsoft.AspNetCore.Mvc;

using EFOP;
using EFOP.Archives;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetSimulationController : ControllerBase
    {
        /*
        /// <summary>
        /// First you need to start a simulation to use it. If there is a simulation, you can get the 0. chunk of the 0. simulation.
        /// GET: api/GetSimulation
        /// </summary>
        [HttpGet]
        public string Get()
        {
            try
            {
                return EFOP.JSONSerializer.SerializeChunk(0, 0, 0);
            }
            catch (ArgumentOutOfRangeException e)
            {
                return "{\"message\":\"There is no simulation\"}"; // "{"simulationNumber":"3"};
            }

        }


        /// <summary>
        /// First you need to start quantity of id simulation to use it. If there is the idth simulation, you can get the 0. chunk of the idth simulation.
        /// GET: api/GetSimulation/5
        /// </summary>
        
        [HttpGet("{id}")]
        public string GetById(int id)

        {
            try
            {
                return EFOP.JSONSerializer.SerializeChunk(id, 0, 0);

            }
            catch (ArgumentOutOfRangeException e)
            {
                return "\"message\":\"There is no simulation with this number\"";
            }
        }

        /// <summary>
        /// First you need to start quantity of id simulation to use it. If there is the idth simulation, you can get the x,y  chunk of the idth simulation.
        /// The x and y coordinates must be divisible by 200, they can also be negative.
        /// GET: api/GetSimulation/id="2"&x="200"&y="400"
        /// https://localhost:44339/api/GetSimulation?id=1086531851&round=2&x=200&y=200
        /// </summary>
        */
        [HttpGet]
        public string Get([FromQuery]int id,[FromQuery] int round, [FromQuery] int x, [FromQuery] int y)
        {
            try
            {
                return Simulation.GetChanges(id, round, x, y);
                //SimulationArchive simulation = new SimulationArchive(id);
                //return simulation.GetRoundJSON(round, x, y);
            }
            catch (ArgumentOutOfRangeException)
            {
                return "\"message\":\"There is no simulation with this number\"";
            }
        }
    }
}