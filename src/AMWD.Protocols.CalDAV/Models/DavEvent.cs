using System;
using System.Collections.Generic;
using Ical.Net.CalendarComponents;

namespace AMWD.Protocols.CalDAV
{
	/// <summary>
	/// Represents a calendar event in a CalDAV server.
	/// </summary>
	public class DavEvent
	{
		/// <summary>
		/// Gets or sets the unique identifier (UID) of the calendar event,
		/// which is used to uniquely identify the event across different calendars and systems.
		/// </summary>
		public string UID { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the summary of the calendar event, which is a brief description or title of the event.
		/// </summary>
		public string? Summary { get; set; }

		/// <summary>
		/// Gets or sets the description of the calendar event, providing additional details about the event's purpose or content.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the start time of the calendar event, indicating when the event begins.
		/// </summary>
		public DateTimeOffset Begin { get; set; }

		/// <summary>
		/// Gets or sets the end time of the calendar event, indicating when the event concludes.
		/// </summary>
		public DateTimeOffset End { get; set; }

		/// <summary>
		/// Gets or sets the location of the calendar event, specifying where the event takes place.
		/// </summary>
		public string? Location { get; set; }

		/// <summary>
		/// Gets or sets the organizer of the calendar event, which is typically the person responsible for the event or the one who created it.
		/// </summary>
		public string? Organizer { get; set; }

		/// <summary>
		/// Gets or sets the attendees of the calendar event, representing the individuals invited to or participating in the event.
		/// </summary>
		public IReadOnlyCollection<string> Attendees { get; set; } = [];

		/// <summary>
		/// Gets or sets the entity tag (ETag) of the calendar event, which is used for concurrency control and caching.
		/// </summary>
		public string ETag { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the URI of the calendar event, providing a reference to the event's location on the server.
		/// </summary>
		public Uri? Href { get; set; }

		/// <summary>
		/// Gets or sets the raw iCalendar data of the calendar event.
		/// </summary>
		public string? RawEvent { get; set; }

		/// <summary>
		/// Gets or sets the parsed iCalendar event object parsed from the <see cref="RawEvent"/>.
		/// </summary>
		public CalendarEvent? ICalEvent { get; set; }
	}
}
