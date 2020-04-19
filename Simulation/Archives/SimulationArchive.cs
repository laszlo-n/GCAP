using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using JSONSerializer;

using EFOP.WorldElements;

using static EFOP.JSONStructure;

namespace EFOP.Archives
{
    /// <summary>
    /// This class is responsible for loading simulations that were previously saved to disk.
    /// </summary>
    [Obsolete("Please use the stateless Simulation.GetChanges(int, int, int, int) method instead!")]
    public class SimulationArchive
    {
        private static string _baseDir;
        private string _dirName;
        private int _currentRound = 0;

        /// <summary>
        /// Gets the number of rounds done in this simulation.
        /// </summary>
        /// <value>The total number of rounds.</value>
        public int RoundCount { get; } = 0;

        private Dictionary<Point, Dictionary<Point, ICellContent>> contents;
        private Dictionary<int, Point> automatonUIDs;

        static SimulationArchive()
        {
            _baseDir = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GCAP/");
        }

        /// <summary>
        /// Creates a ne instance of the SimulationArchive class.
        /// </summary>
        /// <param name="id">The unique numerical id of the simulation to load.</param>
        public SimulationArchive(int id)
        {
            _dirName = Path.Combine(_baseDir, $"sim_{id}/");

            if(!Directory.Exists(_dirName))
            {
                throw new ArgumentException("No such simulation on disk.");
            }

            for(int i = 1; File.Exists($"{_dirName}/round{i}.gcasim"); i ++, RoundCount ++);

            InitializeState();
        }

        /// <summary>
        /// Gets the contents of the simulation at a given round and chunk.
        /// </summary>
        /// <param name="round">
        ///     The round to calculate.
        ///     Rounds start from 1.
        ///     Round 0 means the initial state of the simulation.
        /// </param>
        /// <param name="chunkX">
        ///     The X coordinate of the chunk.
        ///     This is a number dividable by WorldChunk.ChunkSize.
        /// </param>
        /// <param name="chunkY">
        ///     The Y coordinate of the chunk.
        ///     This is a number dividable by WorldChunk.ChunkSize.
        /// </param>
        /// <returns>The contents of the simulation at a given round in a given chunk.</returns>
        public string GetRoundJSON(int round, int chunkX, int chunkY)
        {
            if(round < 0 || round > RoundCount)
                throw new ArgumentOutOfRangeException(nameof(round), "The given round doesn't exist.");

            if(round != _currentRound)
                GoToRound(round);

            return RoundToJSON(new Point(chunkX, chunkY));
        }

        private string RoundToJSON(Point chunk)
        {
            JSONArray array = new JSONArray();
            foreach(KeyValuePair<Point, ICellContent> content in contents[chunk])
            {
                JSONObject obj = new JSONObject();
                obj.AddIntChild(XKey, content.Key.X);
                obj.AddIntChild(YKey, content.Key.Y);
                obj.AddStringChild(TypeKey, content.Value.CharCode.ToString());
                obj.AddIntChild(UIDKey, content.Value.UID);

                array.AddObjectItem(obj);
            }

            return array.ToString();
        }

        private void GoToRound(int round)
        {
            if(round < 0 || round > this.RoundCount)
            {
                throw new ArgumentOutOfRangeException("The given round doesn't exist.");
            }
            if(round == 0)
            {
                InitializeState();
                return;
            }

            int change = this._currentRound < round ? 1 : -1;

            while(this._currentRound != round)
            {
                this._currentRound += change;
                using(StreamReader be = new StreamReader($"{_dirName}/round{_currentRound}.gcasim"))
                {
                    while(!be.EndOfStream)
                    {
                        JSONObject chunkData = new JSONObject(be.ReadLine());
                        string[] chunkCoordinates = chunkData.GetStringChild(ChunkXYKey).Split(",");
                        Point chunkCoord = new Point(int.Parse(chunkCoordinates[0]), int.Parse(chunkCoordinates[1]));
                        
                        // remove dead automata
                        JSONArray deaths = chunkData.GetArrayChild(DeathKey);
                        for(int i = 0; i < deaths.Count; i ++)
                        {
                            Point coord = automatonUIDs[(int)deaths[i]];

                            contents[GetChunkFromPoint(coord)].Remove(coord);
                            automatonUIDs.Remove((int)deaths[i]);
                        }

                        // place newly spawned automata
                        JSONArray spawns = chunkData.GetArrayChild(SpawnKey);
                        for(int i = 0; i < spawns.Count; i ++)
                        {
                            JSONObject spawn = (JSONObject)spawns[i];
                            Automaton a = new Automaton(spawn.GetIntChild(ChildUIDKey));
                            Point coord = new Point(spawn.GetIntChild(childXKey), spawn.GetIntChild(childYKey));

                            contents[GetChunkFromPoint(coord)].Add(coord, a);
                            automatonUIDs.Add(a.UID, coord);
                        }
                        
                        // move existing automata
                        JSONObject movements = chunkData.GetObjectChild(MovementKey);
                        
                        foreach(KeyValuePair<string, object> movement in movements.GetChildren())
                        {
                            try
                            {
                                int uid = int.Parse(movement.Key);
                                string[] coordinates = ((string)movement.Value).Split(",");
                                Point newCoord = new Point(int.Parse(coordinates[0]), int.Parse(coordinates[1]));

                                Point oldCoord = automatonUIDs[uid];
                                Automaton a = (Automaton)contents[GetChunkFromPoint(oldCoord)][oldCoord];
                                contents[GetChunkFromPoint(oldCoord)].Remove(oldCoord);

                                // TODO: detect overwrites
                                automatonUIDs[uid] = newCoord;
                                contents[GetChunkFromPoint(newCoord)][newCoord] = a;
                            }
                            catch(KeyNotFoundException)
                            {
                                // TODO
                            }
                        }
                    }
                }
            }
        }

        private Point GetChunkFromPoint(Point p)
        {
            int chunkX = p.X % WorldChunk.ChunkSize;
            int chunkY = p.Y % WorldChunk.ChunkSize;

            if(chunkX < 0)
                chunkX += WorldChunk.ChunkSize;
            if(chunkY < 0)
                chunkY += WorldChunk.ChunkSize;

            return new Point(p.X - chunkX, p.Y - chunkY);
        }

        private void InitializeState()
        {
            contents = new Dictionary<Point, Dictionary<Point, ICellContent>>();
            automatonUIDs = new Dictionary<int, Point>();

            using(StreamReader be = new StreamReader($"{_dirName}/initial.gcasim"))
            {
                while(!be.EndOfStream)
                {
                    JSONObject chunkObject = new JSONObject(be.ReadLine());
                    Point coordinates = new Point(chunkObject.GetIntChild(ChunkXKey), chunkObject.GetIntChild(ChunkYKey));
                    JSONArray data = chunkObject.GetArrayChild(DataKey);
                    Dictionary<Point, ICellContent> tmpContents = new Dictionary<Point, ICellContent>();

                    for(int i = 0; i < data.Count; i ++)
                    {
                        JSONObject cellContent = (JSONObject)data[i];
                        Point relLoc = new Point(cellContent.GetIntChild(XKey), cellContent.GetIntChild(YKey));
                        Point absLoc = new Point(coordinates.X + relLoc.X, coordinates.Y + relLoc.Y);
                        
                        switch(cellContent.GetStringChild(TypeKey))
                        {
                            case "t":
                                Tree t = new Tree(0);
                                tmpContents.Add(absLoc, t);
                                break;
                            case "a":
                                Automaton a = new Automaton(cellContent.GetIntChild(UIDKey));
                                tmpContents.Add(absLoc, a);
                                automatonUIDs.Add(a.UID, absLoc);
                                break;
                            case "l":
                                Lion l = new Lion(0);
                                tmpContents.Add(absLoc, l);
                                break;
                        }
                    }

                    contents.Add(coordinates, tmpContents);
                }
            }
        }
    }
}
