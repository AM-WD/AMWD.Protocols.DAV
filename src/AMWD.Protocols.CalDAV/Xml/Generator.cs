using System;
using System.Xml;

namespace AMWD.Protocols.CalDAV.Xml
{
	internal static class Generator
	{
		public static string CurrentUserPrincipal()
		{
			var xmlDocument = new XmlDocument();
			var namespaceManager = CreateNamespaceManager(xmlDocument, ("d", "DAV:"));
			xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

			var propFind = CreateElement(xmlDocument, namespaceManager, "d", "propfind");
			xmlDocument.AppendChild(propFind);

			var prop = CreateElement(xmlDocument, namespaceManager, "d", "prop");
			propFind.AppendChild(prop);

			var currentUserPrincipal = CreateElement(xmlDocument, namespaceManager, "d", "current-user-principal");
			prop.AppendChild(currentUserPrincipal);

			return xmlDocument.OuterXml;
		}

		public static string FindCalendars()
		{
			var xmlDocument = new XmlDocument();
			var namespaceManager = CreateNamespaceManager(
				xmlDocument,
				("d", "DAV:"),
				("cal", "urn:ietf:params:xml:ns:caldav"));
			xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

			var propFind = CreateElement(xmlDocument, namespaceManager, "d", "propfind");
			xmlDocument.AppendChild(propFind);

			var prop = CreateElement(xmlDocument, namespaceManager, "d", "prop");
			propFind.AppendChild(prop);

			var displayName = CreateElement(xmlDocument, namespaceManager, "d", "displayname");
			prop.AppendChild(displayName);

			var resourceType = CreateElement(xmlDocument, namespaceManager, "d", "resourcetype");
			prop.AppendChild(resourceType);

			var calendarDescription = CreateElement(xmlDocument, namespaceManager, "cal", "calendar-description");
			prop.AppendChild(calendarDescription);

			var supportedCalendarComponentSet = CreateElement(xmlDocument, namespaceManager, "cal", "supported-calendar-component-set");
			prop.AppendChild(supportedCalendarComponentSet);

			var getCtag = CreateElement(xmlDocument, namespaceManager, "d", "getctag");
			prop.AppendChild(getCtag);

			var getEtag = CreateElement(xmlDocument, namespaceManager, "d", "getetag");
			prop.AppendChild(getEtag);

			return xmlDocument.OuterXml;
		}

		public static string CreateCalendar(string? displayName, string? description)
		{
			var xmlDocument = new XmlDocument();
			var namespaceManager = CreateNamespaceManager(
				xmlDocument,
				("d", "DAV:"),
				("cal", "urn:ietf:params:xml:ns:caldav"));
			xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

			var mkCalendar = CreateElement(xmlDocument, namespaceManager, "cal", "mkcalendar");
			xmlDocument.AppendChild(mkCalendar);

			var set = CreateElement(xmlDocument, namespaceManager, "d", "set");
			mkCalendar.AppendChild(set);

			var prop = CreateElement(xmlDocument, namespaceManager, "d", "prop");
			set.AppendChild(prop);

			var resourceType = CreateElement(xmlDocument, namespaceManager, "d", "resourcetype");
			prop.AppendChild(resourceType);

			resourceType.AppendChild(CreateElement(xmlDocument, namespaceManager, "d", "collection"));
			resourceType.AppendChild(CreateElement(xmlDocument, namespaceManager, "cal", "calendar"));

			var componentSet = CreateElement(xmlDocument, namespaceManager, "cal", "supported-calendar-component-set");
			prop.AppendChild(componentSet);

			var eventElement = CreateElement(xmlDocument, namespaceManager, "cal", "comp");
			eventElement.SetAttribute("name", "VEVENT");
			componentSet.AppendChild(eventElement);

			var journalElement = CreateElement(xmlDocument, namespaceManager, "cal", "comp");
			journalElement.SetAttribute("name", "VJOURNAL");
			componentSet.AppendChild(journalElement);

			var todoElement = CreateElement(xmlDocument, namespaceManager, "cal", "comp");
			todoElement.SetAttribute("name", "VTODO");
			componentSet.AppendChild(todoElement);

			if (!string.IsNullOrWhiteSpace(displayName))
			{
				var displayNameElement = CreateElement(xmlDocument, namespaceManager, "d", "displayname");
				displayNameElement.InnerText = displayName!.Trim();
				prop.AppendChild(displayNameElement);
			}

			if (!string.IsNullOrWhiteSpace(description))
			{
				var descriptionElement = CreateElement(xmlDocument, namespaceManager, "cal", "calendar-description");
				descriptionElement.InnerText = description!.Trim();
				prop.AppendChild(descriptionElement);
			}

			return xmlDocument.OuterXml;
		}

		public static string FindEvents(DateTimeOffset? start, DateTimeOffset? end)
		{
			var xmlDocument = new XmlDocument();
			var namespaceManager = CreateNamespaceManager(
				xmlDocument,
				("d", "DAV:"),
				("cal", "urn:ietf:params:xml:ns:caldav"));
			xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

			var calendarQuery = CreateElement(xmlDocument, namespaceManager, "cal", "calendar-query");
			xmlDocument.AppendChild(calendarQuery);

			var prop = CreateElement(xmlDocument, namespaceManager, "d", "prop");
			calendarQuery.AppendChild(prop);

			var getEtag = CreateElement(xmlDocument, namespaceManager, "d", "getetag");
			prop.AppendChild(getEtag);

			var calendarData = CreateElement(xmlDocument, namespaceManager, "cal", "calendar-data");
			prop.AppendChild(calendarData);

			var filter = CreateElement(xmlDocument, namespaceManager, "cal", "filter");
			calendarQuery.AppendChild(filter);

			var compFilterCalendar = CreateElement(xmlDocument, namespaceManager, "cal", "comp-filter");
			compFilterCalendar.SetAttribute("name", "VCALENDAR");
			filter.AppendChild(compFilterCalendar);

			var compFilterEvent = CreateElement(xmlDocument, namespaceManager, "cal", "comp-filter");
			compFilterEvent.SetAttribute("name", "VEVENT");
			compFilterCalendar.AppendChild(compFilterEvent);

			if (start.HasValue || end.HasValue)
			{
				var timeRangeFilter = CreateElement(xmlDocument, namespaceManager, "cal", "time-range");

				var startValue = start ?? end!.Value.AddMonths(-12); // Default to 12 months before the end date if begin is not provided
				var endValue = end ?? start!.Value.AddMonths(12); // Default to 12 months after the begin date if end is not provided

				timeRangeFilter.SetAttribute("start", startValue.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
				timeRangeFilter.SetAttribute("end", endValue.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'"));
				compFilterEvent.AppendChild(timeRangeFilter);
			}

			return xmlDocument.OuterXml;
		}

		private static XmlNamespaceManager CreateNamespaceManager(XmlDocument xmlDocument, params (string Prefix, string Namespace)[] namespaces)
		{
			var namespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			foreach (var (prefix, ns) in namespaces)
				namespaceManager.AddNamespace(prefix, ns);

			return namespaceManager;
		}

		private static XmlElement CreateElement(XmlDocument xmlDocument, XmlNamespaceManager namespaceManager, string prefix, string localName)
		{
			string ns = namespaceManager.LookupNamespace(prefix);
			if (string.IsNullOrEmpty(ns))
				throw new InvalidOperationException($"Namespace prefix '{prefix}' is not configured.");

			return xmlDocument.CreateElement(prefix, localName, ns);
		}
	}
}
