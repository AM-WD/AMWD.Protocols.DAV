using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Ical.Net;

namespace AMWD.Protocols.CalDAV.Xml
{
	internal static class Parser
	{
		public static string? ExtractPathValue(string xmlString, string path)
		{
			var xDoc = new XmlDocument();
			xDoc.LoadXml(xmlString);

			var namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
			namespaceManager.AddNamespace("d", "DAV:");
			namespaceManager.AddNamespace("cal", "urn:ietf:params:xml:ns:caldav");

			var node = xDoc.SelectSingleNode(path, namespaceManager);
			return node?.InnerText;
		}

		public static IReadOnlyCollection<DavCalendar> CalendarList(string xmlString, Uri? principalUri = null)
		{
			var calendars = new List<DavCalendar>();

			var xDoc = new XmlDocument();
			xDoc.LoadXml(xmlString);

			var namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
			namespaceManager.AddNamespace("d", "DAV:");
			namespaceManager.AddNamespace("cal", "urn:ietf:params:xml:ns:caldav");

			var responses = xDoc.SelectNodes("//d:response", namespaceManager);
			if (responses == null)
				return calendars;

			foreach (XmlNode response in responses)
			{
				var resourceType = response.SelectSingleNode(".//d:resourcetype/cal:calendar", namespaceManager);
				if (resourceType == null)
					continue;

				var calendar = new DavCalendar();

				var href = response.SelectSingleNode("d:href", namespaceManager);
				if (!string.IsNullOrWhiteSpace(href?.InnerText))
				{
					calendar.Name = href!.InnerText.Trim('/').Split('/').LastOrDefault();
					calendar.Uri = principalUri == null
						? new Uri(href!.InnerText, UriKind.RelativeOrAbsolute)
						: new Uri(principalUri, href!.InnerText);
				}

				var displayName = response.SelectSingleNode(".//d:displayname", namespaceManager);
				if (!string.IsNullOrWhiteSpace(displayName?.InnerText))
					calendar.DisplayName = displayName!.InnerText.Trim();

				var description = response.SelectSingleNode(".//cal:calendar-description", namespaceManager);
				if (!string.IsNullOrWhiteSpace(description?.InnerText))
					calendar.Description = description!.InnerText.Trim();

				var ctag = response.SelectSingleNode(".//d:getctag", namespaceManager);
				if (!string.IsNullOrWhiteSpace(ctag?.InnerText))
					calendar.CTag = ctag!.InnerText.Trim().Trim('"');

				var etag = response.SelectSingleNode(".//d:getetag", namespaceManager);
				if (!string.IsNullOrWhiteSpace(etag?.InnerText))
					calendar.ETag = etag!.InnerText.Trim().Trim('"');

				calendars.Add(calendar);
			}

			return calendars;
		}

		public static IReadOnlyCollection<DavEvent> EventList(string xmlString, Uri? calendarUri = null)
		{
			var events = new List<DavEvent>();

			var xDoc = new XmlDocument();
			xDoc.LoadXml(xmlString);

			var namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
			namespaceManager.AddNamespace("d", "DAV:");
			namespaceManager.AddNamespace("cal", "urn:ietf:params:xml:ns:caldav");

			var responses = xDoc.SelectNodes("//d:response", namespaceManager);
			if (responses == null)
				return events;

			foreach (XmlNode response in responses)
			{
				var calendarData = response.SelectSingleNode(".//cal:calendar-data", namespaceManager);
				if (string.IsNullOrWhiteSpace(calendarData?.InnerText))
					continue;

				var calendarEvent = new DavEvent();

				var href = response.SelectSingleNode("d:href", namespaceManager);
				if (!string.IsNullOrWhiteSpace(href?.InnerText))
				{
					calendarEvent.Href = calendarUri == null
						? new Uri(href!.InnerText, UriKind.RelativeOrAbsolute)
						: new Uri(calendarUri, href!.InnerText);
				}

				var etag = response.SelectSingleNode(".//d:getetag", namespaceManager);
				if (!string.IsNullOrWhiteSpace(etag?.InnerText))
					calendarEvent.ETag = etag!.InnerText.Trim().Trim('"');

				calendarEvent.RawEvent = calendarData!.InnerText;
				ParseICal(calendarEvent);

				events.Add(calendarEvent);
			}

			return events;
		}

		private static void ParseICal(DavEvent calendarEvent)
		{
			try
			{
				var calendar = Calendar.Load(calendarEvent.RawEvent!);
				calendarEvent.ICalEvent = calendar?.Events?.FirstOrDefault();
				if (calendarEvent.ICalEvent == null)
					return;

				calendarEvent.UID = calendarEvent.ICalEvent.Uid ?? string.Empty;
				calendarEvent.Summary = calendarEvent.ICalEvent.Summary;
				calendarEvent.Description = calendarEvent.ICalEvent.Description;

				calendarEvent.Location = calendarEvent.ICalEvent.Location;
				calendarEvent.Organizer = calendarEvent.ICalEvent.Organizer?.CommonName ?? calendarEvent.ICalEvent.Organizer?.Value?.ToString();

				if (calendarEvent.ICalEvent.Attendees?.Any() == true)
				{
					calendarEvent.Attendees = calendarEvent.ICalEvent.Attendees
						.Select(a => a.CommonName ?? a.Value?.ToString())
						.Where(a => !string.IsNullOrWhiteSpace(a))
						.Select(a => a!.Trim())
						.ToList();
				}

				if (calendarEvent.ICalEvent.Start != null)
					calendarEvent.Begin = calendarEvent.ICalEvent.Start.AsUtc;

				if (calendarEvent.ICalEvent.End != null)
					calendarEvent.End = calendarEvent.ICalEvent.End.AsUtc;
			}
			catch
			{
				// Keep it quiet.
				// There will be the raw event data as fallback.
			}
		}
	}
}
