using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;

using JSONSerializer;

using EFOP.WorldElements;

using static EFOP.WorldElements.MovementDirection;

#pragma warning disable 1591
namespace EFOP
{
	/// <summary>
	/// A class for running simulations with surviving automatons in them.
	/// </summary>
	public class Simulation
	{
		private static string _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GCAP/");

		/// <summary>
		/// This event happens when a round has been computed.
		/// </summary>
		public event EventHandler<RoundPassedEventArgs> RoundPassed;
		
		internal List<FamilyTree> Families { get; }

		/// <summary>
		/// The unique id of this simulation.
		/// </summary>
		/// <value>A unique integer value used to identify this simulation.</value>
		public int ID { get; private set; }
		
		/// <summary>
		/// This method is used to invoke the round passed event.
		/// </summary>
		/// <param name="round">The number of the round tha just passed.</param>
		protected void OnRoundPassed(long round)
		{
			this.RoundPassed?.Invoke(this, new RoundPassedEventArgs(round));
		}
		
		public SimConfiguration Configuration { get; private set; }
		private Size _size;
		
		private volatile bool isRunning = false;
		
		public long Round { get; private set; }
		
		private Dictionary<Point, WorldChunk> _chunks;
		
		/// <summary>
		/// Initializes a simulation with the default configuration.
		/// </summary>
		/// <param name="id">The unique ID of this simulation.</param>
		/// <param name="logToFile">Whether or not to log this simulation to disk.</param>
		public Simulation(int id, bool logToFile = true)
		{
			this.Configuration	= new SimConfiguration()
			{
				LogToFile		= logToFile
			};
			this.Round			= 0;
			this.Families		= new List<FamilyTree>();
			this.ID				= id;
			if(this.Configuration.LogToFile)
			{
				Directory.CreateDirectory(Simulation.GetDataDirectory(this.ID));
			}
			this.GenerateInitialWorldState();
		}

		/// <summary>
		/// Initializes a simulation with the given configuration.
		/// </summary>
		/// <param name="id">The unique ID of this simulation.</param>
		/// <param name="configuration">The configuration to use in this simulation.</param>
		/// <param name="logToFile">Whether or not to log this simulation to disk.</param>
		public Simulation(int id, SimConfiguration configuration, bool logToFile = true)
		{
			this.Configuration = configuration;
			this.Round = 0;
			this.Families = new List<FamilyTree>();
			this.ID = id;
			if(this.Configuration.LogToFile)
			{
				Directory.CreateDirectory(Simulation.GetDataDirectory(this.ID));
			}
			this.GenerateInitialWorldState();
		}

		public static string GetChanges(int simID, int round, int chunkX, int chunkY)
		{
			string	dir		= Simulation.GetDataDirectory(simID),
					file	= round == 0 ? "initial" : $"round{round}";
			
			if(!Directory.Exists(dir))
			{
				throw new ArgumentException("No simulation found with given id.");
			}

			try
			{
				using(StreamReader be = new StreamReader($"{dir}/{file}.gcasim"))
				{
					while(!be.EndOfStream)
					{
						string line = be.ReadLine();
						JSONObject chunk = new JSONObject(line);
						if(chunk.GetIntChild("chunkX") == chunkX && chunk.GetIntChild("chunkY") == chunkY)
						{
							return line;
						}
					}

					throw new ArgumentOutOfRangeException(nameof(chunkX), "No chunk found with provided X/Y values.");
				}
			}
			catch(FileNotFoundException ex)
			{
				throw new ArgumentException("Given round doesn't exist in the given simulation", nameof(round), ex);
			}
		}
		
		/// <summary>
		/// Starts the simulation to run a given number of rounds or indefinitely if no maximum number is given.
		/// If the simulation is already running, this method sets the number of maximum rounds to the new value and then returns.
		/// </summary>
		/// <param name="maxRounds">The number of rounds after which to stop.</param>
		public void Start(int maxRounds = int.MaxValue)
		{
			this.Configuration.StopAtRound = maxRounds;
			
			if(this.isRunning)
				return;
			
			this.isRunning = true;
			
			Thread t = new Thread(new ThreadStart(this.SimulationLoop));
			t.Start();
		}
		
		/// <summary>
		/// Stops a running simulation. If the simulation isn't running, this method does nothing.
		/// </summary>
		public void Stop()
		{
			this.isRunning = false;
		}
		
		private void SimulationLoop()
		{
			while(this.isRunning)
			{
				Console.WriteLine($"Round {this.Round}:");
				List<JSONObject> roundData = new List<JSONObject>();
				foreach(KeyValuePair<Point, WorldChunk> c in this._chunks)
				{
					JSONObject o = c.Value.ComputeRound();
					o.AddIntChild(JSONStructure.ChunkXKey, c.Key.X);
					o.AddIntChild(JSONStructure.ChunkYKey, c.Key.Y);
					roundData.Add(o);
				}
				
				foreach(KeyValuePair<Point, WorldChunk> c in this._chunks)
				{
					c.Value.FinalizeRound();
				}
				Console.WriteLine();
				
				++ this.Round;
				this.OnRoundPassed(this.Round);
				
				if(this.Configuration.LogToFile)
				{
					SaveWorldState($"{Simulation.GetDataDirectory(this.ID)}/round{this.Round}.gcasim", roundData);
				}

				if(this.Round >= this.Configuration.StopAtRound)
				{
					this.Stop();
					int ones = 0;
					foreach(FamilyTree f in Families)
					{
						if(f.Root.ItemCount() == 1)
						{
							ones ++;
						}
						else
						{
							Console.WriteLine($"Automaton #{f.Root.Item.UID} has a family tree of {f.Root.ItemCount()} automatons.");
						}
					}
					Console.WriteLine($"{ones} automatons didn't multiply at all.");
				}
			}
		}

		public static String GetDataDirectory(int simulationID)
			=> Path.Combine(_baseDir, $"sim_{simulationID}/");

		/// <summary>
		/// Gets the ID of a simulation from the name of its data directory.
		/// </summary>
		/// <returns>An int value which is the id of the simulation, or -1 if the path isn't a valid path.</returns>
		public static int GetIdFromDataDirectory(string path)
		{
			if(path.EndsWith("/") || path.EndsWith("\\"))
			{
				path = path.Substring(0, path.Length - 1);
			}

			path = Path.GetFileName(path);
			
			int id = -1;
			// short cicuit evaluation prevents id from being set if the directory name doesn't start with "sim"
			bool valid = path.StartsWith("sim_") && int.TryParse(path.Substring(4), out id);
			return id;
		}
		
		private void SaveWorldState(string saveFile, List<JSONObject> chunkData)
		{
			using(StreamWriter ki = new StreamWriter(saveFile, true))
			{
				foreach(JSONObject chunk in chunkData)
				{
					ki.WriteLine(chunk.ToString());
				}

				/*foreach(KeyValuePair<Point, WorldChunk> c in this._chunks)
				{
					ki.WriteLine($"{this.Round}. kör:");
					ki.WriteLine($"Chunk ({c.Key.X}, {c.Key.Y}):");
					for(int i = 0; i < WorldChunk.ChunkSize; ++ i)
					{
						if(i % 2 == 0)
								ki.Write("  ");
						
						for(int j = 0; j < WorldChunk.ChunkSize; ++ j)
						{
							if(c.Value.GetContentAt(new Point(i, j)) == null)
							{
								ki.Write("u   ");
							}
							else
							{
								ki.Write($"{c.Value.GetContentAt(new Point(i, j)).CharCode}   ");
							}
						}
						
						ki.WriteLine();
					}
					ki.WriteLine();
				}*/
			}
		}
		
		#region world generation - initial world state
		private void GenerateInitialWorldState()
		{
			this.InitializeChunks();
			
			// place automatons
			
			switch(this.Configuration.PlacementStrategy)
			{
				case AutomatonPlacementStrategy.Random:
					this.PlaceWithRandomStrategy();
					break;
				case AutomatonPlacementStrategy.Even:
					this.PlaceWithEvenStrategy();
					break;
				case AutomatonPlacementStrategy.MultiGroup:
					this.PlaceWithMultiGroupStrategy();
					break;
				case AutomatonPlacementStrategy.SingleGroup:
					this.PlaceWithSingleGroupStrategy();
					break;
			}

			if(this.Configuration.LogToFile)
			{
				string dir = Simulation.GetDataDirectory(this.ID);

				using(StreamWriter ki = new StreamWriter(Path.Combine(dir, "initial.gcasim")))
				using(StreamWriter log = new StreamWriter(Path.Combine(dir, "initial.log")))
				{
					log.WriteLine("Started...");
					foreach(KeyValuePair<Point, WorldChunk> chunk in this._chunks)
					{
						JSONArray contents = new JSONArray();
						List<(Point, ICellContent)> contentList = chunk.Value.GetContentList(log);

						log.WriteLine($"Number of contents in chunk {chunk.Key.X},{chunk.Key.Y}: {contentList.Count}");
						foreach((Point, ICellContent) content in contentList)
						{
							JSONObject contentObject = new JSONObject();
							contentObject.AddIntChild(JSONStructure.XKey, content.Item1.X);
							contentObject.AddIntChild(JSONStructure.YKey, content.Item1.Y);
							contentObject.AddStringChild(JSONStructure.TypeKey, content.Item2.CharCode.ToString());
							contentObject.AddIntChild(JSONStructure.UIDKey, content.Item2.UID);
							if(content.Item2.CharCode == 'a')
							{
								Automaton a = (Automaton)content.Item2;
								contentObject.AddIntChild(JSONStructure.StartState, a.StartingState);
								JSONArray stateTransitions = new JSONArray();
								Dictionary<(int, char), int> wiring = a.GetWiring();
								foreach(KeyValuePair<(int, char), int> transition in wiring)
								{
									JSONObject transitionObject = new JSONObject();
									transitionObject.AddIntChild(JSONStructure.WiringFromKey, transition.Key.Item1);
									transitionObject.AddStringChild(JSONStructure.WiringThroughKey, transition.Key.Item2.ToString());
									transitionObject.AddIntChild(JSONStructure.WiringToKey, transition.Value);
									stateTransitions.AddObjectItem(transitionObject);
								}
								contentObject.AddArrayChild(JSONStructure.WiringKey, stateTransitions);
							}

							contents.AddObjectItem(contentObject);
						}
						log.WriteLine($"Added contents to JSON: {contents.Count}");

						JSONObject chunkData = new JSONObject();
						chunkData.AddIntChild(JSONStructure.ChunkXKey, chunk.Key.X);
						chunkData.AddIntChild(JSONStructure.ChunkYKey, chunk.Key.Y);
						chunkData.AddArrayChild(JSONStructure.DataKey, contents);

						ki.WriteLine(chunkData.ToString());
					}
				}
			}
		}
		
		private void PlaceWithRandomStrategy()
		{
			Random locationGenerator = new Random();
			for(int i = 0; i < this.Configuration.InitialAutomatonNumber; ++ i)
			{
				Point newLoc;
				do
				{
					newLoc = new Point(locationGenerator.Next(-this.Configuration.StartingArea, this.Configuration.StartingArea), locationGenerator.Next(-this.Configuration.StartingArea, this.Configuration.StartingArea));
				}
				while(this.GetContentAt(newLoc) != null);
				this.PlaceAutomaton(newLoc);
			}
		}
		
		private void PlaceWithEvenStrategy()
		{
			Random locationGenerator = new Random();
			/* Algorithm for evenly placing automatons to their initial location:
			 * - compute the percent of cells that will be occupied by automatons
			 * - compute how many cells will contain one automaton, and the area this means
			 * - place one automaton into every area we got in the last step
			 * - place every remaining automaton randomly
			 */
			double	occupyPercent = (double)this.Configuration.InitialAutomatonNumber / Math.Pow(this.Configuration.StartingArea, 2);
			double	cellCount = 1d / occupyPercent; // hány cellánként helyezzünk el egy automatát
			int		areaSize = (int)Math.Floor(Math.Sqrt(cellCount));
			
			int		areaX = -this.Configuration.StartingArea,
					areaY = areaX,
					automatonCount = 0;
			
			for(; areaX < this.Configuration.StartingArea; ++ areaX)
			{
				for(; areaY < this.Configuration.StartingArea; ++ areaY)
				{
					int x = locationGenerator.Next(areaSize),
						y = locationGenerator.Next(areaSize);
					this.PlaceAutomaton(new Point(areaSize * areaX + x, areaSize * areaY + y));
					++ automatonCount;
				}
			}
			
			// mivel nem feltétlenül egész számokkal dolgozunk, előfordulhat kis hiba, pár automata kimaradhat
			while(automatonCount < this.Configuration.InitialAutomatonNumber)
			{
				Point newLoc;
				do
				{
					newLoc = new Point(locationGenerator.Next(-this.Configuration.StartingArea, this.Configuration.StartingArea));
				}
				while(this.GetContentAt(newLoc) != null);
				this.PlaceAutomaton(newLoc);
			}
		}
		
		private void PlaceWithSingleGroupStrategy()
		{
			Random locationGenerator = new Random();
			Point groupCentre = new Point(locationGenerator.Next(-this.Configuration.StartingArea, +this.Configuration.StartingArea),
										  locationGenerator.Next(-this.Configuration.StartingArea, +this.Configuration.StartingArea));
			
			this.PlaceAutomaton(groupCentre);
			
			// TODO: create a plan on what the end result of this strategy should be
			
			throw new NotImplementedException();
		}
		
		private void PlaceWithMultiGroupStrategy()
		{
			throw new NotImplementedException();
		}
		
		private void InitializeChunks()
		{
			// initialize chunks so every possible starting point for an automaton is in a loaded area
			this._chunks = new Dictionary<Point, WorldChunk>();
			int	rem,
				div = Math.DivRem(this.Configuration.StartingArea, WorldChunk.ChunkSize, out rem);
			int	x = -div * WorldChunk.ChunkSize,
				y = -div * WorldChunk.ChunkSize;
			while(x + WorldChunk.ChunkSize <= this.Configuration.StartingArea)
			{
				while(y + WorldChunk.ChunkSize <= this.Configuration.StartingArea)
				{
					Point location = new Point(x, y);
					this._chunks.Add(location, new WorldChunk(this, location));
					y += WorldChunk.ChunkSize;
					Console.WriteLine($"{location.X}, {location.Y}");
				}
				
				y = -div * WorldChunk.ChunkSize;
				x += WorldChunk.ChunkSize;
			}
		}
		#endregion
		
		/// <summary>
		/// Generates a new child automaton for the given automaton and places it in its surroundings if possible.
		/// </summary>
		/// <param name="location">The location of the parent automaton.</param>
		/// <param name="parent">The parent automaton.</param>
		/// <returns>
		/// A (bool, Point, Automaton) tuple.
		/// The first value indicates success.
		/// The second value is the new location of the child automaton, if successful, (0, 0) otherwise.
		/// The third value is the child automaton.
		/// </returns>
		public (bool success, Point newLoc, Automaton child) PlaceChildAutomaton(Point location, Automaton parent)
		{
			Automaton child = new Automaton(this.GenerateNewUID(), parent);
			foreach(MovementDirection direction in Automaton.Surroundings)
			{
				Point newLoc = Simulation.GetDestination(location, direction);
				if(this.GetContentAt(newLoc) == null && this.GetContentAt(newLoc, true) == null)
				{
					this.PlaceAutomaton(newLoc, child, true);
					return (true, newLoc, child);
				}
			}
			
			return (false, Point.Empty, null);
		}
		
		int deadCount = 0;
		HashSet<int> deadAutos = new HashSet<int>();
		
		/// <summary>
		/// Places the given automaton to the given place. If no automaton is given, this method will create one and place that down.
		/// </summary>
		/// <param name="location">The location to put the given automaton to.</param>
		/// <param name="a">The automaton to put the given place. If null, a new randomly wired automaton will be created.</param>
		/// <param name="temporarily">A flag for placing the automaton without modifying the current valid state.</param>
		/// <returns>True if the automaton replaced something else, false if the given place was empty before.</returns>
		public bool PlaceAutomaton(Point location, Automaton a = null, bool temporarily = false)
		{
			if(location.X == this.Configuration.StartingArea)
			{
				location = new Point(-this.Configuration.StartingArea + 1, location.Y);
			}
			if(location.X == -this.Configuration.StartingArea)
			{
				location = new Point(this.Configuration.StartingArea - 1, location.Y);
			}
			if(location.Y == this.Configuration.StartingArea)
			{
				location = new Point(location.X, -this.Configuration.StartingArea + 1);
			}
			if(location.Y == -this.Configuration.StartingArea)
			{
				location = new Point(location.X, this.Configuration.StartingArea - 1);
			}
			
			int remX = location.X % WorldChunk.ChunkSize;
			int remY = location.Y % WorldChunk.ChunkSize;
			if(remX < 0)
			{
				remX += WorldChunk.ChunkSize;
			}
			if(remY < 0)
			{
				remY += WorldChunk.ChunkSize;
			}
			Point chunkpoint = new Point(location.X - remX, location.Y - remY);
			WorldChunk chunk = this._chunks[chunkpoint];
			Point automatonPoint = new Point(remX, remY);
			
			if(a == null)
			{
				a = new Automaton(this.GenerateNewUID());
				this.Families.Add(new FamilyTree(a));
				/*a.Die += (sender, e) =>
				{
					deadAutos.Add(a.UID);
					Console.WriteLine($"Halott: {++ deadCount}, {deadAutos.Count} unique, sent by {a.UID}");
				};*/
			}
			
			return chunk.SetContent(a, automatonPoint, temporarily);
		}
		
		public (string surroundings, int harvestableTrees) GetSurroundings(Point p, bool harvestTrees = false)
		{
			StringBuilder sb = new StringBuilder();
			int trees = 0;
			
			foreach(MovementDirection direction in Automaton.Surroundings)
			{
				Point loc = Simulation.GetDestination(p, direction);
				ICellContent content = this.GetContentAt(loc);
				
				if((content as Tree)?.CanBeHarvested ?? false)
				{
					trees ++;
					if(harvestTrees)
						(content as Tree)?.Harvest(this.Round);
				}
				sb.Append(content?.CharCode ?? 'u');
			}

			return (sb.ToString(), trees);
		}

		public ICellContent GetContentAt(Point location, bool temporarily = false)
		{
			if(location.X == this.Configuration.StartingArea)
			{
				location = new Point(-this.Configuration.StartingArea + 1, location.Y);
			}
			if(location.X == -this.Configuration.StartingArea)
			{
				location = new Point(this.Configuration.StartingArea - 1, location.Y);
			}
			if(location.Y == this.Configuration.StartingArea)
			{
				location = new Point(location.X, -this.Configuration.StartingArea + 1);
			}
			if(location.Y == -this.Configuration.StartingArea)
			{
				location = new Point(location.X, this.Configuration.StartingArea - 1);
			}
			
			
			
			int remX = location.X % WorldChunk.ChunkSize;
			int remY = location.Y % WorldChunk.ChunkSize;
			if(remX < 0)
			{
				remX += WorldChunk.ChunkSize;
			}
			if(remY < 0)
			{
				remY += WorldChunk.ChunkSize;
			}
			Point chunkpoint = new Point(location.X - remX, location.Y - remY);
			WorldChunk chunk = this._chunks[chunkpoint];
			Point automatonPoint = new Point(remX, remY);

			return chunk.GetContentAt(automatonPoint, temporarily);
		}

		#region UID handling
		private HashSet<int>	_usedIDs = new HashSet<int>();
		private Random			_uidGenerator = new Random();
		public int GenerateNewUID()
		{
			int uid;
			do
			{
				uid = _uidGenerator.Next();
			}
			while(_usedIDs.Contains(uid));
			
			_usedIDs.Add(uid);
			return uid;
		}
		
		private void ReuseUID(int uid)
		{
			_usedIDs.Remove(uid);
		}
		#endregion
		
		public List<(Point, ICellContent)> GetContentsOfChunk(Point chunkCoordinates)
		{
			return this._chunks[new Point(chunkCoordinates.X * WorldChunk.ChunkSize, chunkCoordinates.Y * WorldChunk.ChunkSize)].GetContentList();
		}
		
		private static Point GetDestination(Point p, MovementDirection move)
		{
			switch(move)
			{
				case Left:
					return new Point(p.X - 1, p.Y); // checked
				case UpperLeft:
					return p.Y % 2 == 0 ? new Point(p.X, p.Y - 1) : new Point(p.X - 1, p.Y - 1);
				case UpperRight:
					return p.Y % 2 == 0 ? new Point(p.X + 1, p.Y - 1) : new Point(p.X, p.Y - 1);
				case Right:
					return new Point(p.X + 1, p.Y); // checked
				case BottomRight:
					return p.Y % 2 == 0 ? new Point(p.X + 1, p.Y + 1) : new Point(p.X, p.Y + 1);
				case BottomLeft:
					return p.Y % 2 == 0 ? new Point(p.X, p.Y + 1) : new Point(p.X - 1, p.Y + 1);
				default:
					throw new ArgumentOutOfRangeException(nameof(move), "Ismeretlen haladási irány.");
			}
		}
		
		public class SimConfiguration
		{
			public static readonly Size InifiniteSize = new Size(int.MaxValue, int.MaxValue);
			public Size	Size { get; private set; }
			public int	InitialAutomatonNumber { get; private set; }
			public int	StartingArea { get; private set; }
			public AutomatonPlacementStrategy PlacementStrategy { get; }
			
			public int StopAtRound { get; set; }
			
			public bool LogToFile { get; set; }
			
			public SimConfiguration() : this(SimConfiguration.InifiniteSize)
			{
				
			}
			
			public SimConfiguration(Size size)
			{
				this.Size					= size;
				this.InitialAutomatonNumber	= 1000;
				this.StartingArea			= 400;
				this.PlacementStrategy		= AutomatonPlacementStrategy.Random;
				this.StopAtRound			= int.MaxValue;
				this.LogToFile				= true;
			}
		}
	}
	
	/// <summary>
	/// Stores the event data for the <see cref="Simulation.RoundPassed" /> event.
	/// </summary>
	public class RoundPassedEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the current round of the simulation.
		/// </summary>
		/// <value>The current round of the simulation.</value>
		public long Round { get; }
		
		/// <summary>
		/// Initializes a new RoundPassedEventArgs instance with the given round.
		/// </summary>
		/// <param name="roundNum">The given round.</param>
		public RoundPassedEventArgs(long roundNum) : base()
		{
			this.Round = roundNum;
		}

		static class Archives
		{
			
		}
	}
}
#pragma warning restore 1591