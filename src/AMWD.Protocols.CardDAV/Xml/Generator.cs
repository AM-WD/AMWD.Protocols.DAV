using System;
using System.Xml;

namespace AMWD.Protocols.CardDAV.Xml
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

		public static string FindAddressBooks()
		{
			var xmlDocument = new XmlDocument();
			var namespaceManager = CreateNamespaceManager(
				xmlDocument,
				("d", "DAV:"),
				("card", "urn:ietf:params:xml:ns:carddav"));
			xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

			var propFind = CreateElement(xmlDocument, namespaceManager, "d", "propfind");
			xmlDocument.AppendChild(propFind);

			var prop = CreateElement(xmlDocument, namespaceManager, "d", "prop");
			propFind.AppendChild(prop);

			var resourceType = CreateElement(xmlDocument, namespaceManager, "d", "resourcetype");
			prop.AppendChild(resourceType);

			var displayName = CreateElement(xmlDocument, namespaceManager, "d", "displayname");
			prop.AppendChild(displayName);

			var getCtag = CreateElement(xmlDocument, namespaceManager, "d", "getctag");
			prop.AppendChild(getCtag);

			var getEtag = CreateElement(xmlDocument, namespaceManager, "d", "getetag");
			prop.AppendChild(getEtag);

			var addressBookDescription = CreateElement(xmlDocument, namespaceManager, "card", "addressbook-description");
			prop.AppendChild(addressBookDescription);

			var supportedAddressBookComponentSet = CreateElement(xmlDocument, namespaceManager, "card", "supported-address-data");
			prop.AppendChild(supportedAddressBookComponentSet);

			return xmlDocument.OuterXml;
		}

		public static string CreateAddressBook(string? displayName, string? description)
		{
			var xmlDocument = new XmlDocument();
			var namespaceManager = CreateNamespaceManager(
				xmlDocument,
				("d", "DAV:"),
				("card", "urn:ietf:params:xml:ns:carddav"));
			xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

			var mkCol = CreateElement(xmlDocument, namespaceManager, "d", "mkcol");
			xmlDocument.AppendChild(mkCol);

			var set = CreateElement(xmlDocument, namespaceManager, "d", "set");
			mkCol.AppendChild(set);

			var prop = CreateElement(xmlDocument, namespaceManager, "d", "prop");
			set.AppendChild(prop);

			var resourceType = CreateElement(xmlDocument, namespaceManager, "d", "resourcetype");
			prop.AppendChild(resourceType);

			resourceType.AppendChild(CreateElement(xmlDocument, namespaceManager, "d", "collection"));
			resourceType.AppendChild(CreateElement(xmlDocument, namespaceManager, "card", "addressbook"));

			if (!string.IsNullOrWhiteSpace(displayName))
			{
				var displayNameElement = CreateElement(xmlDocument, namespaceManager, "d", "displayname");
				displayNameElement.InnerText = displayName!.Trim();
				prop.AppendChild(displayNameElement);
			}

			if (!string.IsNullOrWhiteSpace(description))
			{
				var descriptionElement = CreateElement(xmlDocument, namespaceManager, "card", "addressbook-description");
				descriptionElement.InnerText = description!.Trim();
				prop.AppendChild(descriptionElement);
			}

			return xmlDocument.OuterXml;
		}

		public static string FindCards(string? name)
		{
			var xmlDocument = new XmlDocument();
			var namespaceManager = CreateNamespaceManager(
				xmlDocument,
				("d", "DAV:"),
				("card", "urn:ietf:params:xml:ns:carddav"));
			xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

			var addressBookQuery = CreateElement(xmlDocument, namespaceManager, "card", "addressbook-query");
			xmlDocument.AppendChild(addressBookQuery);

			var prop = CreateElement(xmlDocument, namespaceManager, "d", "prop");
			addressBookQuery.AppendChild(prop);

			var getEtag = CreateElement(xmlDocument, namespaceManager, "d", "getetag");
			prop.AppendChild(getEtag);

			var addressData = CreateElement(xmlDocument, namespaceManager, "card", "address-data");
			prop.AppendChild(addressData);

			var filter = CreateElement(xmlDocument, namespaceManager, "card", "filter");
			addressBookQuery.AppendChild(filter);

			var propFilterName = CreateElement(xmlDocument, namespaceManager, "card", "prop-filter");
			propFilterName.SetAttribute("name", "N");
			filter.AppendChild(propFilterName);

			if (!string.IsNullOrWhiteSpace(name))
			{
				var textMatch = CreateElement(xmlDocument, namespaceManager, "card", "text-match");
				textMatch.SetAttribute("collation", "i;unicode-casemap");
				textMatch.InnerText = name!.Trim();
				propFilterName.AppendChild(textMatch);
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
