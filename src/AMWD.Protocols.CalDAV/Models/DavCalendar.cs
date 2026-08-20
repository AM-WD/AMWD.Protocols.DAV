using System;

namespace AMWD.Protocols.CalDAV
{
	/// <summary>
	/// Represents a calendar resource in a CalDAV server.
	/// </summary>
	public class DavCalendar
	{
		/// <summary>
		/// Gets or sets the name of the calendar, typically derived from the last segment of the calendar's URI.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Gets or sets the display name of the calendar, which is a human-readable name for the calendar.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the description of the calendar, providing additional information about the calendar's purpose or content.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the URI of the calendar, which is the unique identifier for the calendar resource on the CalDAV server.
		/// </summary>
		public Uri? Uri { get; set; }

		/// <summary>
		/// Gets or sets the CTag of the calendar, which is a unique identifier that changes whenever the calendar's content changes, allowing clients to detect updates.
		/// </summary>
		public string? CTag { get; set; }

		/// <summary>
		/// Gets or sets the ETag of the calendar, which is a unique identifier that changes whenever the calendar's content changes, allowing clients to detect updates.
		/// </summary>
		public string? ETag { get; set; }
	}
}
