namespace AMWD.Protocols.CalDAV
{
	/// <summary>
	/// Represents a request to create a new calendar on a CalDAV server.
	/// </summary>
	public class CreateCalendarRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CreateCalendarRequest"/> class with the specified calendar name.
		/// </summary>
		/// <param name="name">The name of the calendar to be created.</param>
		public CreateCalendarRequest(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets or sets the name of the calendar to be created.
		/// This is typically a unique identifier for the calendar resource on the CalDAV server
		/// and is part of the URL.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the display name of the calendar to be created.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the description of the calendar to be created.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the HEX color of the calendar to be created.
		/// </summary>
		public string? Color { get; set; }
	}
}
