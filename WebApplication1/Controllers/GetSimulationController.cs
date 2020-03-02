using System;
using Microsoft.AspNetCore.Mvc;

using EFOP;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetSimulationController : ControllerBase
    {

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
                return EFOP.JSONSerializer.SerializeChunk(id, 0, 0); ;
            }
            catch (ArgumentOutOfRangeException e)
            {
                return "\"message\":\"There is no simulation with this number\"";
            }
        }

        /// <summary>
        /// First you need to start quantity of id simulation to use it. If there is the idth simulation, you can get the x,y  chunk of the idth simulation.
        /// GET: api/GetSimulation/id="2"&x="200"&y="300"
        /// </summary>
        /*[HttpGet("{id,x,y}", Name = "GetWhole")]
        public string Get(int id, int x, int y)
        {
            try
            {
                return EFOP.JSONSerializer.SerializeChunk(id, x, y); ;
            }
            catch (ArgumentOutOfRangeException e)
            {
                return "\"message\":\"There is no simulation with this number\"";
            }
        }*/
    }
}