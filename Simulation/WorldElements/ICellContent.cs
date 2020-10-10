namespace EFOP.WorldElements
{
	/// <summary>
	/// An interface for contents of cells in a genetic cellular automaton simulation.
	/// </summary>
	public interface ICellContent
	{
		/// <summary>
		/// A unique identifier for this object.
		/// </summary>
		/// <value>An integer value which is bigger than 0 and unique among all ICellContent objects in a simulation. 0 represents an empty cell and is not used.</value>
		int UID { get; }
		
		/// <summary>
		/// A character representing the class this object belongs to.
		/// This is needed for creating input for automatons.
		/// </summary>
		/// <value>A character that is constant inside a class and represents that class in automaton inputs.</value>
		char CharCode { get; }
	}
}