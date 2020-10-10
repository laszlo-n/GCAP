using System;

namespace EFOP.WorldElements
{
	// TODO: use an own clock instead of datetime so the simulation doesn't have to run in real time
	// this clock should preferably be the rounds that have passed in the simulation
	
	/// <summary>
	/// Represents a tree in a simulation, which is a food source for automatons.
	/// </summary>
	public class Tree : ICellContent
	{
		private const long HarvestRate = 30;
		
		/// <see cref="EFOP.WorldElements.ICellContent.UID" />
		public int UID { get; }
		
		/// <see cref="EFOP.WorldElements.ICellContent.CharCode" />
		public char CharCode { get { return 't'; } }
		
		private long _lastHarvested;
		
		/// <summary>
		/// A boolean value which tells whether or not this tree object can be used as a food source currently.
		/// </summary>
		/// <value><code>true</code> if this tree can be used as a food source, <code>false</code> otherwise.</value>
		public bool CanBeHarvested { get; private set; }
		
		/// <summary>
		/// Resets the time this tree needs until it can be harvested next time.
		/// </summary>
		public void Harvest(long round)
		{
			this._lastHarvested = round;
			this.CanBeHarvested = false;
		}
		
		/// <summary>
		/// Initializes a new <see cref="Tree" /> instance with the given UID and the maximum time until next harvesting.
		/// </summary>
		/// <param name="uid">The unique identifier of this <see cref="ICellContent" /> implementer.</param>
		public Tree(int uid)
		{
			this.UID			= uid;
			this._lastHarvested	= 0;
		}
		
		/// <summary>
		/// This method receives a round passed event from its parent simulation and updates the boolean value representing whether this tree can be harvested in this round.
		/// </summary>
		/// <param name="sender">The parent simulation.</param>
		/// <param name="e">An eventargs object containing the new round number.</param>
		public void UpdateHarvest(object sender, RoundPassedEventArgs e)
		{
			if(e.Round - this._lastHarvested > Tree.HarvestRate)
			{
				this.CanBeHarvested = true;
			}
		}
	}
}