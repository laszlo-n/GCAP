using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using EFOP.WorldElements;

//using Newtonsoft.Json;

namespace EFOP
{
    /// <summary>
    /// This class is responsible for serializing chunks into JSON data.
    /// </summary>
    public class JSONSerializer
    {
        /// <summary>
        /// This method is responsible for serializing chunks into JSON data.
        /// </summary>
        /// <param name="simulation">The simulation to use.</param>
        /// <param name="x">The X coordinate of the chunk to serialize.</param>
        /// <param name="y">The y coordinate of the chunk to serialize.</param>
        /// <returns>The JSON array created, as a string.</returns>
        public static string SerializeChunk(int simulation, int x, int y)
        {
            Simulation s = SimulationHandler.GetSimulationById(simulation);
            List<(Point, ICellContent)> chunkContents = s.GetContentsOfChunk(new Point(x, y));

            StringBuilder json = new StringBuilder();
            json.Append("[");
            for (int i = 0, max = chunkContents.Count; i < max; i++)
            {
                json.Append($"{{\"X\":\"{chunkContents[i].Item1.X}\", \"Y\":\"{chunkContents[i].Item1.Y}\", \"Type\":\"{chunkContents[i].Item2.CharCode}\", \"UID\":\"{chunkContents[i].Item2.UID}\"}}");
                if (i != max - 1)
                {
                    json.Append(", ");
                }
            }
            json.Append("]");
            string output = json.ToString();

            #if DEBUG
            using (StreamWriter ki = new StreamWriter("json.txt"))
            {
                ki.Write(output);
            }
            #endif

            return output;
        }
    }
}
