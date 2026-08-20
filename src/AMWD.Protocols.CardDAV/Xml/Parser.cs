using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using vCard.Net.CardComponents;
using vCard.Net.Serialization;

namespace AMWD.Protocols.CardDAV.Xml
{
	internal static class Parser
	{
		public static string? ExtractPathValue(string xmlString, string path)
		{
			var xDoc = new XmlDocument();
			xDoc.LoadXml(xmlString);

			var namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
			namespaceManager.AddNamespace("d", "DAV:");
			namespaceManager.AddNamespace("card", "urn:ietf:params:xml:ns:carddav");

			var node = xDoc.SelectSingleNode(path, namespaceManager);
			return node?.InnerText;
		}

		public static IReadOnlyCollection<DavAddressBook> AddressBookList(string xmlString, Uri? principalUri = null)
		{
			var addressBooks = new List<DavAddressBook>();

			var xDoc = new XmlDocument();
			xDoc.LoadXml(xmlString);

			var namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
			namespaceManager.AddNamespace("d", "DAV:");
			namespaceManager.AddNamespace("card", "urn:ietf:params:xml:ns:carddav");

			var responses = xDoc.SelectNodes("//d:response", namespaceManager);
			if (responses == null)
				return addressBooks;

			foreach (XmlNode response in responses)
			{
				var resourceType = response.SelectSingleNode(".//d:resourcetype/card:addressbook", namespaceManager);
				if (resourceType == null)
					continue;

				var addressBook = new DavAddressBook();

				var href = response.SelectSingleNode("d:href", namespaceManager);
				if (!string.IsNullOrWhiteSpace(href?.InnerText))
				{
					addressBook.Name = href!.InnerText.Trim('/').Split('/').LastOrDefault();
					addressBook.Uri = principalUri == null
						? new Uri(href!.InnerText, UriKind.RelativeOrAbsolute)
						: new Uri(principalUri, href!.InnerText);
				}

				var displayName = response.SelectSingleNode(".//d:displayname", namespaceManager);
				if (!string.IsNullOrWhiteSpace(displayName?.InnerText))
					addressBook.DisplayName = displayName!.InnerText.Trim();

				var description = response.SelectSingleNode(".//card:addressbook-description", namespaceManager);
				if (!string.IsNullOrWhiteSpace(description?.InnerText))
					addressBook.Description = description!.InnerText.Trim();

				var ctag = response.SelectSingleNode(".//d:getctag", namespaceManager);
				if (!string.IsNullOrWhiteSpace(ctag?.InnerText))
					addressBook.CTag = ctag!.InnerText.Trim().Trim('"');

				var etag = response.SelectSingleNode(".//d:getetag", namespaceManager);
				if (!string.IsNullOrWhiteSpace(etag?.InnerText))
					addressBook.ETag = etag!.InnerText.Trim().Trim('"');

				addressBooks.Add(addressBook);
			}

			return addressBooks;
		}

		public static IReadOnlyCollection<DavCard> CardList(string xmlString, Uri? addressBookUri = null)
		{
			var cards = new List<DavCard>();

			var xDoc = new XmlDocument();
			xDoc.LoadXml(xmlString);

			var namespaceManager = new XmlNamespaceManager(xDoc.NameTable);
			namespaceManager.AddNamespace("d", "DAV:");
			namespaceManager.AddNamespace("card", "urn:ietf:params:xml:ns:carddav");

			var responses = xDoc.SelectNodes("//d:response", namespaceManager);
			if (responses == null)
				return cards;

			foreach (XmlNode response in responses)
			{
				var cardData = response.SelectSingleNode(".//card:address-data", namespaceManager);
				if (string.IsNullOrWhiteSpace(cardData?.InnerText))
					continue;

				var card = new DavCard();
				var href = response.SelectSingleNode("d:href", namespaceManager);
				if (!string.IsNullOrWhiteSpace(href?.InnerText))
				{
					card.Href = addressBookUri == null
						? new Uri(href!.InnerText, UriKind.RelativeOrAbsolute)
						: new Uri(addressBookUri, href!.InnerText);
				}

				var etag = response.SelectSingleNode(".//d:getetag", namespaceManager);
				if (!string.IsNullOrWhiteSpace(etag?.InnerText))
					card.ETag = etag!.InnerText.Trim().Trim('"');

				card.RawCard = cardData!.InnerText;
				ParseVCard(card);

				cards.Add(card);
			}
			return cards;
		}

		private static void ParseVCard(DavCard card)
		{
			if (string.IsNullOrWhiteSpace(card.RawCard))
				return;

			using var ms = new MemoryStream(Encoding.UTF8.GetBytes(card.RawCard));
			using var sr = new StreamReader(ms);

			card.VCard = SimpleDeserializer.Default.Deserialize(sr)
				.Cast<VCard>()
				.FirstOrDefault();
			if (card.VCard == null)
				return;

			card.UID = card.VCard.Uid;
			card.DisplayName = card.VCard.FormattedName?.Trim();
			if (string.IsNullOrWhiteSpace(card.DisplayName))
				card.DisplayName = card.VCard.N.FormattedName;

			if (string.IsNullOrWhiteSpace(card.DisplayName))
				card.DisplayName = $"{card.VCard.N.GivenName} {card.VCard.N.FamilyName}".Trim();

			card.Birthday = DateTime.TryParseExact(
					card.VCard.Birthdate,
					"yyyyMMdd",
					CultureInfo.InvariantCulture,
					DateTimeStyles.None,
					out var birthday)
				? birthday
				: null;
		}
	}
}
