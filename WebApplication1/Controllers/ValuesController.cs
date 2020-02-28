using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using EFOP;
namespace WebApplication1.Controllers
{
    // DEPRECATED

    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        
        /*
        // GET api/values
        public string Get() //IEnumerable<string>
        {
            return JSONSerializer.SerializeChunk(0, 0, 0);
        }

        // GET api/values/5
        public string Get(int id)
        {
            return JSONSerializer.SerializeChunk(id, 0, 0); ;
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
        */
        
        //a nulladik szimulációt adja vissza, aktuális körrel
        // GET api/values
        [HttpGet]

        public string Get()
        {
            try
            {
                return EFOP.JSONSerializer.SerializeChunk(0, 0, 0);
            }
            catch(ArgumentOutOfRangeException)
            {
                return "{\"message\":\"There is no simulation\"}";
            }
        
        }
        //a ötötdik szimulációt adja vissza 200 körrel
        // GET api/values/5
        public string Get(int id)
        {
            try
            {
                return EFOP.JSONSerializer.SerializeChunk(id, 0, 0); ;
            }
            catch(ArgumentOutOfRangeException)
            {
                return "{\"message\":\"There is no simulation with this number\"}";
            }
        }
        /*
        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }*/
        

        //api/getstate
        //visszaadná a jelenlegi állapot sorszámát 

        public String PostStartSimulation()
        {
            SimulationHandler.StartNew(200);
            return $"{{\"simulationNumber\":\"{SimulationHandler.GetSimulationCount() - 1}\"}}"; // "{"simulationNumber":"3"}
        }
        public long GetState(int simulation)
        {
            try
            {
                return SimulationHandler.GetSimulation(simulation).Round;
            }
            catch(IndexOutOfRangeException)
            {
                return 0;
            }
        }

        public void GetNext()
        {

        }
        //worldchunk.GetContentList

        //api/switchstate? val = -1
        //post request
        //ez 1-gyel visszaléptetné a szimulációs állapotot
        public ActionResult<string> PostSwitchState(int val, int simulation)
        {
            try
            {
                SimulationHandler.StartNew(200);
                //SimulationHandler.GetSimulation(simulation)
                long state = SimulationHandler.GetSimulation(simulation).Round - 1;
                return "value";
            }
            catch (IndexOutOfRangeException)
            {
                return " ";
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
        
    }
}



