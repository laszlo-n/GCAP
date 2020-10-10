using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EFOP.WorldElements
{
	/// <summary>
	/// Represents a finite state automaton.
	/// The automaton's states are the directions it can move and its state changes are its surroundings.
	/// </summary>
	public class Automaton : ICellContent
	{
		/// <summary>
		/// Contains a list of all possible movement directions in left to bottomleft order.
		/// </summary>
		public static readonly ImmutableArray<MovementDirection> Surroundings;
		
		/// <summary>
		/// This event fires when the wellbeing of this automaton reaches 100% and it can multiply.
		/// </summary>
		public event EventHandler Multiply;
		
		/// <summary>
		/// Call this method when you want to fire the <see cref="Multiply" /> event.
		/// </summary>
		protected void OnMultiply()
		{
			this.Multiply?.Invoke(this, EventArgs.Empty);
		}
		
		/// <summary>
		/// This event fires when the wellbeing of this automaton reaches 0% and it should be deleted.
		/// </summary>
		public event EventHandler Die;
		
		/// <summary>
		/// Call this method when you want to fire the <see cref="Die" /> event.
		/// </summary>
		protected void OnDie()
		{
			this.Die?.Invoke(this, EventArgs.Empty);
		}
		
		private static Random			rnGenerator;

		private static readonly char[]	inputs;
		private static readonly int		stateCount;
		
		/// <summary>
		/// The number of the state this automaton starts in.
		/// </summary>
		/// <value>Randomly chosen in the constructor.</value>
		public int						StartingState { get; }
		
		/// <summary>
		/// Gets the wellbeing of this automaton. This is a byte value between 0 and 100.
		/// </summary>
		/// <value>The wellbeing of this automaton.</value>
		public byte						WellBeingPercent { get; private set; }
		
		/// <summary>
		/// Gets or sets the number of times this automaton can multiply based on its previous feedings and hurtings.
		/// </summary>
		/// <value>The number of times this automaton can multiply.</value>
		public byte						MultiplyCount { get; set; }
		
		/// <summary>
		/// Gets whether or not this automaton is dead, aka it's well being is at 0%.
		/// </summary>
		/// <value>A boolean value representing whether or not this automaton is dead.</value>
		public bool						IsDead { get { return WellBeingPercent == 0; }}

		/* static constructor for the Automaton class
		 * this constructor sets the default value for all static members of the class
		 * place value assignment here if you create a new static member
		 * we have a static random number generator so rapid automaton generation won't result in identical seeds
		 */
		static Automaton()
		{
			rnGenerator	= new Random();
			inputs		= new char[] { 'a', 't', 'l', 'u' }; // automaton, tree, lion, empty
			stateCount	= 6;
			
			var surrBuilder = ImmutableArray.CreateBuilder<MovementDirection>();
			surrBuilder.Add(MovementDirection.Left);
			surrBuilder.Add(MovementDirection.UpperLeft);
			surrBuilder.Add(MovementDirection.UpperRight);
			surrBuilder.Add(MovementDirection.Right);
			surrBuilder.Add(MovementDirection.BottomRight);
			surrBuilder.Add(MovementDirection.BottomLeft);
			
			Surroundings= surrBuilder.ToImmutableArray();
		}

		/// <see cref="EFOP.WorldElements.ICellContent.UID"/>
		public int UID { get; }
		
		/// <see cref="EFOP.WorldElements.ICellContent.CharCode" />
		public char CharCode
		{
			get { return 'a'; }
		}
		
		private Dictionary<(int, char), int> movements;
		
		/// <summary>
		/// Gets a copy of the wiring of this automaton.
		/// This is a dictionary consisting of an (int, char) pair (what state, what input) key and an int (new state) value.
		/// </summary>
		/// <returns>The wiring of this automaton, an ((int, char), int) dictionary.</returns>
		public Dictionary<(int, char), int> GetWiring()
		{
			return new Dictionary<(int, char), int>(movements);
		} 
		
		/// <summary>
		/// Computes the ending state of this automaton using the given input string.
		/// This method also counts wel-being changes based on the environment given to it.
		/// </summary>
		/// <param name="input">The input on which to base state changes.</param>
		/// <returns>An integer value representing one of the states of this automaton, which is also the direction the automaton decided to move in.</returns>
		public int ComputeState(string input)
		{
			int state = this.StartingState;
			foreach(char c in input)
			{
				state = this.movements[(state, c)];
			}
			return state;
		}
		
		/// <summary>
		/// This method updates the well being of this automaton
		/// It fires the <see cref="Multiply" /> event if it reaches 100%, and fires the <see cref="Die" /> event if it reaches 0%.
		/// </summary>
		/// <param name="trees">The number of trees to feed on.</param>
		/// <param name="lions">The number of lions to fight.</param>
		public void UpdateWellBeing(int trees, int lions)
		{
			int all = trees - lions;
			if(all > 0)
			{
				Console.WriteLine($"Feeding automaton #{this.UID} (new wellbeing: {this.WellBeingPercent})");
			}
			while(all > 0)
			{
				this.Feed();
				-- all;
			}
			while(all < 0 && this.WellBeingPercent != 0)
			{
				this.Hurt();
				++ all;
			}
		}
		
		/// <summary>
		/// This method increases the well being percentage of this automaton and fires the <see cref="Multiply" /> event if it reaches 100%.
		/// After firing the event, the well being is set to 50%.
		/// </summary>
		public void Feed()
		{
			this.WellBeingPercent += 10;
			if(this.WellBeingPercent == 100)
			{
				this.MultiplyCount ++;
				this.OnMultiply();
				this.WellBeingPercent = 50;
			}
		}
		
		/// <summary>
		/// This method decreases the well being percentage of this automaton and fires the <see cref="Die" /> event if it reaches 0%.
		/// </summary>
		public void Hurt()
		{
			this.WellBeingPercent -= 10;
			if(this.WellBeingPercent == 0)
			{
				this.OnDie();
			}
		}

		/// <summary>
		/// Initializes a new <code>Automaton</code> instance with the given UID and a random wiring.
		/// </summary>
		/// <param name="uid">The UID of this automaton within the simulation.</param>
		/// <param name="generateRandomWiring">
		/// 	An optional parameter that indicates whether a random wiring should be generated.
		/// 	If set to false, no wiring is generated. If set to true, the wiring will be generated randomly.
		/// 	This parameter is <code>true</code> by default.
		/// </param>
		public Automaton(int uid, bool generateRandomWiring = true)
		{
			this.UID				= uid;
			this.movements			= new Dictionary<(int, char), int>();
			this.StartingState		= Automaton.rnGenerator.Next(stateCount);
			this.WellBeingPercent	= 50;

			if(generateRandomWiring)
			{
				for(int state = 0; state < stateCount; ++ state) // go through every state
				{
					for(int input = 0; input < inputs.Length; ++ input) // go through every input
					{
						this.movements.Add((state, inputs[input]), Automaton.rnGenerator.Next(stateCount));
					}
				}
			}
		}
		
		/// <summary>
		/// Creates an automaton from the given parent automaton, using the given new UID.
		/// Optionally mutates the automaton to have a slightly different wiring from its parent.
		/// </summary>
		/// <param name="uid">The UID of this automaton within the simulation.</param>
		/// <param name="parent">The automaton to use as a base.</param>
		/// <param name="mutate">A flag indicating whether the wiring should be changed, true by default.</param>
		public Automaton(int uid, Automaton parent, bool mutate = true)
		{
			this.UID				= uid;
			this.StartingState		= parent.StartingState;
			this.WellBeingPercent	= 50;
			this.movements			= parent.GetWiring();
			
			if(mutate)
			{
				for(int i = 0, max = Automaton.rnGenerator.Next(6); i < max; ++ i)
				{
					this.movements[(Automaton.rnGenerator.Next(stateCount), inputs[Automaton.rnGenerator.Next(inputs.Length)])] = Automaton.rnGenerator.Next(stateCount);
				}
			}
		}
	}
	
	/// <summary>
	/// Represents a direction in which an automaton can move on a hexagonal plane.
	/// </summary>
	public enum MovementDirection
	{
		/// <summary>Movement to the left in this layer.</summary>
		Left,
		/// <summary>Movement to the left in the upper layer.</summary>
		UpperLeft,
		/// <summary>Movement to the right in the upper layer.</summary>
		UpperRight,
		/// <summary>Movement to the right in this layer.</summary>
		Right,
		/// <summary>Movement to the right in the bottom layer.</summary>
		BottomRight,
		/// <summary>Movement to the left in the bottom layer.</summary>
		BottomLeft
	}
}