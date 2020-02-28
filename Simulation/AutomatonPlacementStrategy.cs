namespace EFOP
{
	/// <summary>
	/// Lists the available options for how to generate the location of automatons when a simulation is initialized.
	/// </summary>
	public enum AutomatonPlacementStrategy
	{
		/// <summary>Place automatons randomly.</summary>
		Random,
		/// <summary>Place automatons around a single point.</summary>
		
		SingleGroup,
		/// <summary>Place automatons in smaller groups around multiple points.</summary>
		MultiGroup,
		/// <summary>Place automatons as far from each other as the starting area allows.</summary>
		Even
	}
}