using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;

using EFOP.WorldElements;

using JSONSerializer;

namespace EFOP
{
	class WorldChunk
	{
		public const int ChunkSize = 200;
		
		private Simulation parent;
		
		public Point Location { get; private set; }
		
		private Dictionary<Point, ICellContent> ChunkContents { get; set; }
		private Dictionary<Point, ICellContent> tmpContents = new Dictionary<Point, ICellContent>();
		
		public ICellContent GetContentAt(Point location, bool temporarily = false)
		{
			return temporarily ? this.tmpContents.GetValueOrDefault(location, null) : this.ChunkContents.GetValueOrDefault(location, null);
		}
		
		public void FinalizeRound()
		{
			// TODO: copy only references to speed up the system
			this.ChunkContents = new Dictionary<Point, ICellContent>(tmpContents); // copy the elements
			tmpContents.Clear();
		}
		
		public JSONObject ComputeRound()
		{
			JSONObject finalMovements = new JSONObject();
			JSONArray spawns = new JSONArray();
			JSONArray deaths = new JSONArray();

			foreach(KeyValuePair<Point, ICellContent> content in this.ChunkContents)
			{
				if(content.Value.CharCode == 'a')
				{
					Automaton a = content.Value as Automaton;
					Point absoluteOldLocation = new Point(this.Location.X + content.Key.X, this.Location.Y + content.Key.Y);
					(string automatonInput, int harvestableTrees) = this.parent.GetSurroundings(absoluteOldLocation, true);
					int movement = a.ComputeState(automatonInput);
					a.UpdateWellBeing(harvestableTrees, automatonInput.Count(e => e == 'l'));
					
					if(!a.IsDead)
					{
						Point newLocation;
						switch(movement)
						{
							case 0: // balra megyünk
								newLocation = new Point(content.Key.X - 1, content.Key.Y);
								break;
							case 1: // bal fel megyünk
								newLocation = new Point(content.Key.Y % 2 == 0 ? content.Key.X - 1 : content.Key.X, content.Key.Y - 1);
								break;
							case 2: // jobb fel megyünk
								newLocation = new Point(content.Key.Y % 2 == 0 ? content.Key.X : content.Key.X + 1, content.Key.Y - 1);
								break;
							case 3: // jobbra megyünk
								newLocation = new Point(content.Key.X + 1, content.Key.Y);
								break;
							case 4: // jobb lentre megyünk
								newLocation = new Point(content.Key.Y % 2 == 0 ? content.Key.X : content.Key.X + 1, content.Key.Y + 1);
								break;
							case 5: // bal lentre megyünk
								newLocation = new Point(content.Key.Y % 2 == 0 ? content.Key.X - 1 : content.Key.X, content.Key.Y + 1);
								break;
							default:
								throw new IndexOutOfRangeException("Unknown automaton state reached.");
						}
						
						Point absoluteNewLocation = new Point(this.Location.X + newLocation.X, this.Location.Y + newLocation.Y);
						if(this.parent.GetContentAt(absoluteNewLocation) == null)
						{
							if(this.parent.PlaceAutomaton(absoluteNewLocation, (Automaton)content.Value, true))
							{
								this.parent.PlaceAutomaton(absoluteOldLocation, (Automaton)content.Value, true);
							}
							else
							{
								finalMovements.AddStringChild(a.UID.ToString(), $"{absoluteNewLocation.X},{absoluteNewLocation.Y}");
							}
						}
						else
						{
							this.parent.PlaceAutomaton(absoluteOldLocation, (Automaton)content.Value, true);
						}
						
						while(a.MultiplyCount != 0)
						{
							(bool success, Point childLoc, Automaton child) =
								this.parent.PlaceChildAutomaton(content.Key, a);
							if(success)
							{
								Console.WriteLine($"Automaton #{a.UID} multiplied successfully.");
								JSONObject spawn = new JSONObject();
								spawn.AddIntChild("parentUID", a.UID);
								spawn.AddIntChild("childUID", child.UID);
								spawn.AddIntChild("childX", childLoc.X);
								spawn.AddIntChild("childY", childLoc.Y);
								spawn.AddIntChild("childStartState", child.StartingState);

								JSONArray stateTransitions = new JSONArray();
								Dictionary<(int, char), int> wiring = child.GetWiring();
								foreach(KeyValuePair<(int, char), int> transition in wiring)
								{
									JSONObject transitionObject = new JSONObject();
									transitionObject.AddIntChild("from", transition.Key.Item1);
									transitionObject.AddStringChild("through", transition.Key.Item2.ToString());
									transitionObject.AddIntChild("to", transition.Value);
									stateTransitions.AddObjectItem(transitionObject);
								}
								spawn.AddArrayChild("childWiring", stateTransitions);

								spawns.AddObjectItem(spawn);
								foreach(FamilyTree f in this.parent.Families)
								{
									if(f.ContainsAutomaton(a))
									{
										f.AddToTree(a, child);
									}
								}
							}
							else
							{
								Console.WriteLine($"Automaton #{a.UID} tried to multiply.");
							}
							
							a.MultiplyCount --;
						}
					}
					else
					{
						Console.WriteLine($"Automaton #{a.UID} died.");
						deaths.AddIntItem(a.UID);
					}
				}
				else // nem automatáról van szó, adjuk hozzá változatlanul a tmphez
				{
					tmpContents[content.Key] = content.Value;
				}
			}

			JSONObject result = new JSONObject();
			result.AddObjectChild("movements", finalMovements);
			result.AddArrayChild("spawns", spawns);
			result.AddArrayChild("deaths", deaths);
			return result;
		}
		
		// TODO: update documentation so it accurately represents what this method does
		
		/// <summary>
		/// Replaces the content of the given cell with new content.
		/// </summary>
		/// <param name="content">The content to put in the given location.</param>
		/// <param name="relativeLocation">The location of the cell where the new content will be put.</param>
		/// <param name="temporarily">A flag whether to make this change in the temporary storage or the permanent one.</param>
		/// <returns>A value representing whether the cell at the given location was already occupied before placing new content there.</returns>
		public bool SetContent(ICellContent content, Point relativeLocation, bool temporarily = false)
		{
			if(	relativeLocation.X < 0 ||
				relativeLocation.X >= WorldChunk.ChunkSize ||
				relativeLocation.Y < 0 ||
				relativeLocation.Y >= WorldChunk.ChunkSize)
			{
				throw new ArgumentOutOfRangeException(nameof(relativeLocation), "The relative location must be inside the chunk - non negative and less than the chunk size.");
			}
			
			bool alreadyOccupied = false;
			if(temporarily)
			{
				alreadyOccupied = this.tmpContents.ContainsKey(relativeLocation);
				if(!alreadyOccupied)
					this.tmpContents[relativeLocation] = content;
			}
			else
			{
				alreadyOccupied = this.ChunkContents.ContainsKey(relativeLocation);
				if(!alreadyOccupied)
					this.ChunkContents[relativeLocation] = content;
			}
			return alreadyOccupied;
		}
		
		private void GenerateTerain()
		{
			Random rn = new Random();
			int treeNumber = rn.Next((int)(WorldChunk.ChunkSize * WorldChunk.ChunkSize * 0.1));
			int lionNumber = rn.Next((int)(WorldChunk.ChunkSize * WorldChunk.ChunkSize * 0.05));
			Console.WriteLine($"({this.Location.X}, {this.Location.Y}): {treeNumber} trees, {lionNumber} lions");
			Func<Point> getRandomLocation =
				() =>
				{
					Point loc;
					do
					{
						loc = new Point(rn.Next(WorldChunk.ChunkSize), rn.Next(WorldChunk.ChunkSize));
					}
					while(this.ChunkContents.ContainsKey(loc));
					return loc;
				};
			
			for(int i = 0; i < treeNumber; ++ i)
			{
				Tree t = new Tree(this.parent.GenerateNewUID());
				this.parent.RoundPassed += t.UpdateHarvest;
				this.SetContent(t, getRandomLocation());
			}
			
			for(int i = 0; i < lionNumber; ++ i)
			{
				this.SetContent(new Lion(this.parent.GenerateNewUID()), getRandomLocation());
			}
		}
		
		public List<(Point, ICellContent)> GetContentList(System.IO.StreamWriter logger = null)
		{
			if(logger != null)
			{
				logger.WriteLine($"Chunk {this.Location.X},{this.Location.Y} has {this.ChunkContents.Count} contents...");
			}
			List<(Point, ICellContent)> result = new List<(Point, ICellContent)>();
			foreach(KeyValuePair<Point, ICellContent> content in this.ChunkContents)
			{
				result.Add((content.Key, content.Value));
			}
			return result;
		}
		
		public WorldChunk(Simulation parent, Point location)
		{
			this.parent			= parent;
			this.Location		= location;
			this.ChunkContents	= new Dictionary<Point, ICellContent>();
			
			this.GenerateTerain();
		}
	}
}