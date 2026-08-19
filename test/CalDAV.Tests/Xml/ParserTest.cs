using System;
using System.Linq;
using AMWD.Protocols.CalDAV.Xml;

namespace CalDAV.Tests.Xml
{
	[TestClass]
	public class ParserTest
	{
		private string _calendarXml;
		private string _eventXml;

		[TestInitialize]
		public void Initialize()
		{
			_calendarXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"" xmlns:cs=""http://calendarserver.org/ns/"">
  <d:response>
	<d:href>/calendars/team/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>Team Calendar</d:displayname>
		<cal:calendar-description>Shared team calendar</cal:calendar-description>
		<cs:getctag>""123""</cs:getctag>
		<cs:getetag>""abc123""</cs:getetag>
		<cal:calendar-color>#FF0000</cal:calendar-color>
	  </d:prop>
	  <d:status>HTTP/1.1 200 OK</d:status>
	</d:propstat>
	<d:resourcetype>
	  <cal:calendar />
	</d:resourcetype>
  </d:response>
  <d:response>
	<d:href>/calendars/other/</d:href>
	<d:resourcetype>
	  <d:collection />
	</d:resourcetype>
  </d:response>
</d:multistatus>";

			_eventXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/team/meeting.ics</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""evt-1""</d:getetag>
		<cal:calendar-data>BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example//EN
BEGIN:VEVENT
UID:12345
DTSTART:20250110T080000Z
DTEND:20250110T090000Z
SUMMARY:Team Standup
DESCRIPTION:Discuss blockers
LOCATION:Room 101
ORGANIZER:CN=Alice:mailto:alice@example.com
ATTENDEE;CN=Bob:mailto:bob@example.com
END:VEVENT
END:VCALENDAR</cal:calendar-data>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
		}

		[TestMethod]
		public void ShouldExtractPathValueFromXml()
		{
			// Arrange
			const string path = "/d:multistatus/d:response[1]/d:href";

			// Act
			string value = Parser.ExtractPathValue(_calendarXml, path);

			// Assert
			Assert.AreEqual("/calendars/team/", value);
		}

		[TestMethod]
		public void ShouldParseCalendarsFromResponseXml()
		{
			// Arrange
			var principalUri = new Uri("https://example.com/" );

			// Act
			var calendars = Parser.CalendarList(_calendarXml, principalUri);

			// Assert
			Assert.HasCount(1, calendars);
			var calendar = calendars.First();

			Assert.AreEqual("team", calendar.Name);
			Assert.AreEqual("Team Calendar", calendar.DisplayName);
			Assert.AreEqual("Shared team calendar", calendar.Description);
			Assert.AreEqual("123", calendar.CTag);
			Assert.AreEqual("abc123", calendar.ETag);
			Assert.AreEqual("#FF0000", calendar.Color);
			Assert.AreEqual(new Uri("https://example.com/calendars/team/"), calendar.Uri);
		}

		[TestMethod]
		public void ShouldParseEventsFromResponseXml()
		{
			// Arrange
			var calendarUri = new Uri("https://example.com/calendars/team/");

			// Act
			var events = Parser.EventList(_eventXml, calendarUri);

			// Assert
			Assert.HasCount(1, events);
			var calendarEvent = events.First();
			Assert.AreEqual("12345", calendarEvent.UID);
			Assert.AreEqual("Team Standup", calendarEvent.Summary);
			Assert.AreEqual("Discuss blockers", calendarEvent.Description);
			Assert.AreEqual("Room 101", calendarEvent.Location);
			Assert.AreEqual("evt-1", calendarEvent.ETag);
			Assert.AreEqual(new Uri("https://example.com/calendars/team/meeting.ics"), calendarEvent.Href);
			Assert.HasCount(1, calendarEvent.Attendees);
			Assert.AreEqual("Bob", calendarEvent.Attendees.First());
		}

		#region ExtractPathValue Tests

		[TestMethod]
		public void ShouldReturnNullForNonExistentPath()
		{
			// Arrange
			const string path = "/d:multistatus/d:response[99]/d:href";

			// Act
			string value = Parser.ExtractPathValue(_calendarXml, path);

			// Assert
			Assert.IsNull(value);
		}

		[TestMethod]
		public void ShouldReturnNullForInvalidPath()
		{
			// Arrange
			const string path = "/invalid/path/that/does/not/exist";

			// Act
			string value = Parser.ExtractPathValue(_calendarXml, path);

			// Assert
			Assert.IsNull(value);
		}

		#endregion

		#region CalendarList Tests

		[TestMethod]
		public void ShouldReturnEmptyListForEmptyResponse()
		{
			// Arrange
			string emptyXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var calendars = Parser.CalendarList(emptyXml, principalUri);

			// Assert
			Assert.HasCount(0, calendars);
		}

		[TestMethod]
		public void ShouldSkipCalendarsWithoutResourceType()
		{
			// Arrange
			string xmlWithoutResourceType = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/invalid/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>Invalid Calendar</d:displayname>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var calendars = Parser.CalendarList(xmlWithoutResourceType, principalUri);

			// Assert
			Assert.HasCount(0, calendars);
		}

		[TestMethod]
		public void ShouldParseCalendarWithoutOptionalFields()
		{
			// Arrange
			string xmlMinimal = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/minimal/</d:href>
	<d:resourcetype>
	  <cal:calendar />
	</d:resourcetype>
  </d:response>
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var calendars = Parser.CalendarList(xmlMinimal, principalUri);

			// Assert
			Assert.HasCount(1, calendars);
			var calendar = calendars.First();
			Assert.AreEqual("minimal", calendar.Name);
			Assert.IsNull(calendar.DisplayName);
			Assert.IsNull(calendar.Description);
			Assert.IsNull(calendar.CTag);
			Assert.IsNull(calendar.ETag);
			Assert.IsNull(calendar.Color);
		}

		[TestMethod]
		public void ShouldParseMultipleCalendars()
		{
			// Arrange
			string xmlMultiple = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/first/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>First Calendar</d:displayname>
	  </d:prop>
	</d:propstat>
	<d:resourcetype>
	  <cal:calendar />
	</d:resourcetype>
  </d:response>
  <d:response>
	<d:href>/calendars/second/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>Second Calendar</d:displayname>
	  </d:prop>
	</d:propstat>
	<d:resourcetype>
	  <cal:calendar />
	</d:resourcetype>
  </d:response>
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var calendars = Parser.CalendarList(xmlMultiple, principalUri);

			// Assert
			Assert.HasCount(2, calendars);
			var calendarList = calendars.ToList();
			Assert.AreEqual("first", calendarList[0].Name);
			Assert.AreEqual("First Calendar", calendarList[0].DisplayName);
			Assert.AreEqual("second", calendarList[1].Name);
			Assert.AreEqual("Second Calendar", calendarList[1].DisplayName);
		}

		[TestMethod]
		public void ShouldParseCalendarsWithNullPrincipalUri()
		{
			// Act
			var calendars = Parser.CalendarList(_calendarXml, null);

			// Assert
			Assert.HasCount(1, calendars);
			var calendar = calendars.First();
			Assert.AreEqual("team", calendar.Name);
			Assert.IsNotNull(calendar.Uri);
		}

		[TestMethod]
		public void ShouldTrimQuotesFromETagAndCTag()
		{
			// Arrange
			var principalUri = new Uri("https://example.com/");

			// Act
			var calendars = Parser.CalendarList(_calendarXml, principalUri);

			// Assert
			var calendar = calendars.First();
			Assert.AreEqual("123", calendar.CTag);
			Assert.AreEqual("abc123", calendar.ETag);
			Assert.DoesNotContain("\"", calendar.CTag);
			Assert.DoesNotContain("\"", calendar.ETag);
		}

		#endregion

		#region EventList Tests

		[TestMethod]
		public void ShouldReturnEmptyListForEmptyEventResponse()
		{
			// Arrange
			string emptyXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
</d:multistatus>";
			var calendarUri = new Uri("https://example.com/calendars/team/");

			// Act
			var events = Parser.EventList(emptyXml, calendarUri);

			// Assert
			Assert.HasCount(0, events);
		}

		[TestMethod]
		public void ShouldSkipEventsWithoutCalendarData()
		{
			// Arrange
			string xmlWithoutCalendarData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/team/invalid.ics</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""evt-1""</d:getetag>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var calendarUri = new Uri("https://example.com/calendars/team/");

			// Act
			var events = Parser.EventList(xmlWithoutCalendarData, calendarUri);

			// Assert
			Assert.HasCount(0, events);
		}

		[TestMethod]
		public void ShouldParseMultipleEvents()
		{
			// Arrange
			string xmlMultipleEvents = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/team/event1.ics</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""evt-1""</d:getetag>
		<cal:calendar-data>BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example//EN
BEGIN:VEVENT
UID:event1
DTSTART:20250110T080000Z
DTEND:20250110T090000Z
SUMMARY:Event 1
END:VEVENT
END:VCALENDAR</cal:calendar-data>
	  </d:prop>
	</d:propstat>
  </d:response>
  <d:response>
	<d:href>/calendars/team/event2.ics</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""evt-2""</d:getetag>
		<cal:calendar-data>BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example//EN
BEGIN:VEVENT
UID:event2
DTSTART:20250111T100000Z
DTEND:20250111T110000Z
SUMMARY:Event 2
END:VEVENT
END:VCALENDAR</cal:calendar-data>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var calendarUri = new Uri("https://example.com/calendars/team/");

			// Act
			var events = Parser.EventList(xmlMultipleEvents, calendarUri);

			// Assert
			Assert.HasCount(2, events);
			var eventList = events.ToList();
			Assert.AreEqual("event1", eventList[0].UID);
			Assert.AreEqual("Event 1", eventList[0].Summary);
			Assert.AreEqual("event2", eventList[1].UID);
			Assert.AreEqual("Event 2", eventList[1].Summary);
		}

		[TestMethod]
		public void ShouldParseEventWithNullCalendarUri()
		{
			// Act
			var events = Parser.EventList(_eventXml, null);

			// Assert
			Assert.HasCount(1, events);
			var calendarEvent = events.First();
			Assert.AreEqual("12345", calendarEvent.UID);
			Assert.IsNotNull(calendarEvent.Href);
		}

		[TestMethod]
		public void ShouldParseEventWithoutOptionalFields()
		{
			// Arrange
			string xmlMinimalEvent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/team/minimal.ics</d:href>
	<d:propstat>
	  <d:prop>
		<cal:calendar-data>BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example//EN
BEGIN:VEVENT
UID:minimal-event
DTSTART:20250110T080000Z
DTEND:20250110T090000Z
SUMMARY:Minimal Event
END:VEVENT
END:VCALENDAR</cal:calendar-data>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var calendarUri = new Uri("https://example.com/calendars/team/");

			// Act
			var events = Parser.EventList(xmlMinimalEvent, calendarUri);

			// Assert
			Assert.HasCount(1, events);
			var calendarEvent = events.First();
			Assert.AreEqual("minimal-event", calendarEvent.UID);
			Assert.AreEqual("Minimal Event", calendarEvent.Summary);
			Assert.IsNull(calendarEvent.Description);
			Assert.IsNull(calendarEvent.Location);
			Assert.IsNull(calendarEvent.Organizer);
		}

		[TestMethod]
		public void ShouldParseEventWithMultipleAttendees()
		{
			// Arrange
			string xmlMultipleAttendees = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:cal=""urn:ietf:params:xml:ns:caldav"">
  <d:response>
	<d:href>/calendars/team/meeting.ics</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""evt-1""</d:getetag>
		<cal:calendar-data>BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example//EN
BEGIN:VEVENT
UID:12345
DTSTART:20250110T080000Z
DTEND:20250110T090000Z
SUMMARY:Team Meeting
ATTENDEE;CN=Alice:mailto:alice@example.com
ATTENDEE;CN=Bob:mailto:bob@example.com
ATTENDEE;CN=Charlie:mailto:charlie@example.com
END:VEVENT
END:VCALENDAR</cal:calendar-data>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var calendarUri = new Uri("https://example.com/calendars/team/");

			// Act
			var events = Parser.EventList(xmlMultipleAttendees, calendarUri);

			// Assert
			Assert.HasCount(1, events);
			var calendarEvent = events.First();
			Assert.HasCount(3, calendarEvent.Attendees);
			var attendeeList = calendarEvent.Attendees.ToList();
			Assert.AreEqual("Alice", attendeeList[0]);
			Assert.AreEqual("Bob", attendeeList[1]);
			Assert.AreEqual("Charlie", attendeeList[2]);
		}

		[TestMethod]
		public void ShouldTrimQuotesFromEventETag()
		{
			// Arrange
			var calendarUri = new Uri("https://example.com/calendars/team/");

			// Act
			var events = Parser.EventList(_eventXml, calendarUri);

			// Assert
			var calendarEvent = events.First();
			Assert.AreEqual("evt-1", calendarEvent.ETag);
			Assert.DoesNotContain("\"", calendarEvent.ETag);
		}

		#endregion
	}
}
