namespace EFOP.WorldElements
{
	// TODO: implement logic for the lion's movement
	class Lion : ICellContent
	{
		public int UID { get; }
		
		public char CharCode { get { return 'l'; } }
		
		public Lion(int uid)
		{
			this.UID = uid;
		}
	}
}