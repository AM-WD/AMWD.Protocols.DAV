using System;
using System.Linq;
using AMWD.Protocols.CardDAV.Xml;

namespace CardDAV.Tests.Xml
{
	[TestClass]
	public class ParserTest
	{
		private string _addressBookXml;
		private string _cardXml;

		[TestInitialize]
		public void Initialize()
		{
			_addressBookXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/personal/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>Contacts</d:displayname>
		<card:addressbook-description>Shared contacts address book</card:addressbook-description>
		<d:getctag>""456""</d:getctag>
		<d:getetag>""def456""</d:getetag>
	  </d:prop>
	  <d:status>HTTP/1.1 200 OK</d:status>
	</d:propstat>
	<d:resourcetype>
	  <card:addressbook />
	</d:resourcetype>
  </d:response>
  <d:response>
	<d:href>/addressbooks/other/</d:href>
	<d:resourcetype>
	  <d:collection />
	</d:resourcetype>
  </d:response>
</d:multistatus>";

			_cardXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/personal/contact.vcf</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""card-1""</d:getetag>
		<card:address-data>BEGIN:VCARD
VERSION:3.0
UID:john-doe-123
FN:John Doe
N:Doe;John;;;
EMAIL:john.doe@example.com
TEL:+1-555-0100
BDAY:19900315
END:VCARD</card:address-data>
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
			string value = Parser.ExtractPathValue(_addressBookXml, path);

			// Assert
			Assert.AreEqual("/addressbooks/personal/", value);
		}

		[TestMethod]
		public void ShouldParseAddressBooksFromResponseXml()
		{
			// Arrange
			var principalUri = new Uri("https://example.com/");

			// Act
			var addressBooks = Parser.AddressBookList(_addressBookXml, principalUri);

			// Assert
			Assert.HasCount(1, addressBooks);
			var addressBook = addressBooks.First();

			Assert.AreEqual("personal", addressBook.Name);
			Assert.AreEqual("Contacts", addressBook.DisplayName);
			Assert.AreEqual("Shared contacts address book", addressBook.Description);
			Assert.AreEqual("456", addressBook.CTag);
			Assert.AreEqual("def456", addressBook.ETag);
			Assert.AreEqual(new Uri("https://example.com/addressbooks/personal/"), addressBook.Uri);
		}

		[TestMethod]
		public void ShouldParseCardsFromResponseXml()
		{
			// Arrange
			var addressBookUri = new Uri("https://example.com/addressbooks/personal/");

			// Act
			var cards = Parser.CardList(_cardXml, addressBookUri);

			// Assert
			Assert.HasCount(1, cards);
			var card = cards.First();
			Assert.AreEqual("john-doe-123", card.UID);
			Assert.AreEqual("John Doe", card.DisplayName);
			Assert.AreEqual("card-1", card.ETag);
			Assert.AreEqual(new Uri("https://example.com/addressbooks/personal/contact.vcf"), card.Href);
			Assert.IsNotNull(card.VCard);
			Assert.IsNotNull(card.RawCard);
		}

		#region ExtractPathValue Tests

		[TestMethod]
		public void ShouldReturnNullForNonExistentPath()
		{
			// Arrange
			const string path = "/d:multistatus/d:response[99]/d:href";

			// Act
			string value = Parser.ExtractPathValue(_addressBookXml, path);

			// Assert
			Assert.IsNull(value);
		}

		[TestMethod]
		public void ShouldReturnNullForInvalidPath()
		{
			// Arrange
			const string path = "/invalid/path/that/does/not/exist";

			// Act
			string value = Parser.ExtractPathValue(_addressBookXml, path);

			// Assert
			Assert.IsNull(value);
		}

		#endregion ExtractPathValue Tests

		#region AddressBookList Tests

		[TestMethod]
		public void ShouldReturnEmptyListForEmptyResponse()
		{
			// Arrange
			string emptyXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var addressBooks = Parser.AddressBookList(emptyXml, principalUri);

			// Assert
			Assert.HasCount(0, addressBooks);
		}

		[TestMethod]
		public void ShouldSkipAddressBooksWithoutResourceType()
		{
			// Arrange
			string xmlWithoutResourceType = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/invalid/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>Invalid AddressBook</d:displayname>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var addressBooks = Parser.AddressBookList(xmlWithoutResourceType, principalUri);

			// Assert
			Assert.HasCount(0, addressBooks);
		}

		[TestMethod]
		public void ShouldParseAddressBookWithoutOptionalFields()
		{
			// Arrange
			string xmlMinimal = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/minimal/</d:href>
	<d:resourcetype>
	  <card:addressbook />
	</d:resourcetype>
  </d:response>
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var addressBooks = Parser.AddressBookList(xmlMinimal, principalUri);

			// Assert
			Assert.HasCount(1, addressBooks);
			var addressBook = addressBooks.First();
			Assert.AreEqual("minimal", addressBook.Name);
			Assert.IsNull(addressBook.DisplayName);
			Assert.IsNull(addressBook.Description);
			Assert.IsNull(addressBook.CTag);
			Assert.IsNull(addressBook.ETag);
		}

		[TestMethod]
		public void ShouldParseMultipleAddressBooks()
		{
			// Arrange
			string xmlMultiple = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/work/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>Work Contacts</d:displayname>
	  </d:prop>
	</d:propstat>
	<d:resourcetype>
	  <card:addressbook />
	</d:resourcetype>
  </d:response>
  <d:response>
	<d:href>/addressbooks/personal/</d:href>
	<d:propstat>
	  <d:prop>
		<d:displayname>Personal Contacts</d:displayname>
	  </d:prop>
	</d:propstat>
	<d:resourcetype>
	  <card:addressbook />
	</d:resourcetype>
  </d:response>
</d:multistatus>";
			var principalUri = new Uri("https://example.com/");

			// Act
			var addressBooks = Parser.AddressBookList(xmlMultiple, principalUri);

			// Assert
			Assert.HasCount(2, addressBooks);
			var addressBookList = addressBooks.ToList();
			Assert.AreEqual("work", addressBookList[0].Name);
			Assert.AreEqual("Work Contacts", addressBookList[0].DisplayName);
			Assert.AreEqual("personal", addressBookList[1].Name);
			Assert.AreEqual("Personal Contacts", addressBookList[1].DisplayName);
		}

		[TestMethod]
		public void ShouldParseAddressBooksWithNullPrincipalUri()
		{
			// Act
			var addressBooks = Parser.AddressBookList(_addressBookXml, null);

			// Assert
			Assert.HasCount(1, addressBooks);
			var addressBook = addressBooks.First();
			Assert.AreEqual("personal", addressBook.Name);
			Assert.IsNotNull(addressBook.Uri);
		}

		[TestMethod]
		public void ShouldTrimQuotesFromETagAndCTag()
		{
			// Arrange
			var principalUri = new Uri("https://example.com/");

			// Act
			var addressBooks = Parser.AddressBookList(_addressBookXml, principalUri);

			// Assert
			var addressBook = addressBooks.First();
			Assert.AreEqual("456", addressBook.CTag);
			Assert.AreEqual("def456", addressBook.ETag);
			Assert.DoesNotContain("\"", addressBook.CTag);
			Assert.DoesNotContain("\"", addressBook.ETag);
		}

		#endregion AddressBookList Tests

		#region CardList Tests

		[TestMethod]
		public void ShouldReturnEmptyListForEmptyCardResponse()
		{
			// Arrange
			string emptyXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
</d:multistatus>";
			var addressBookUri = new Uri("https://example.com/addressbooks/personal/");

			// Act
			var cards = Parser.CardList(emptyXml, addressBookUri);

			// Assert
			Assert.HasCount(0, cards);
		}

		[TestMethod]
		public void ShouldSkipCardsWithoutAddressData()
		{
			// Arrange
			string xmlWithoutCardData = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/personal/invalid.vcf</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""card-1""</d:getetag>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var addressBookUri = new Uri("https://example.com/addressbooks/personal/");

			// Act
			var cards = Parser.CardList(xmlWithoutCardData, addressBookUri);

			// Assert
			Assert.HasCount(0, cards);
		}

		[TestMethod]
		public void ShouldParseMultipleCards()
		{
			// Arrange
			string xmlMultipleCards = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/personal/card1.vcf</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""card-1""</d:getetag>
		<card:address-data>BEGIN:VCARD
VERSION:3.0
UID:contact-1
FN:Alice Smith
N:Smith;Alice;;;
EMAIL:alice@example.com
END:VCARD</card:address-data>
	  </d:prop>
	</d:propstat>
  </d:response>
  <d:response>
	<d:href>/addressbooks/personal/card2.vcf</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""card-2""</d:getetag>
		<card:address-data>BEGIN:VCARD
VERSION:3.0
UID:contact-2
FN:Bob Johnson
N:Johnson;Bob;;;
EMAIL:bob@example.com
END:VCARD</card:address-data>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var addressBookUri = new Uri("https://example.com/addressbooks/personal/");

			// Act
			var cards = Parser.CardList(xmlMultipleCards, addressBookUri);

			// Assert
			Assert.HasCount(2, cards);
			var cardList = cards.ToList();
			Assert.AreEqual("contact-1", cardList[0].UID);
			Assert.AreEqual("Alice Smith", cardList[0].DisplayName);
			Assert.AreEqual("contact-2", cardList[1].UID);
			Assert.AreEqual("Bob Johnson", cardList[1].DisplayName);
		}

		[TestMethod]
		public void ShouldParseCardWithNullAddressBookUri()
		{
			// Act
			var cards = Parser.CardList(_cardXml, null);

			// Assert
			Assert.HasCount(1, cards);
			var card = cards.First();
			Assert.AreEqual("john-doe-123", card.UID);
			Assert.IsNotNull(card.Href);
		}

		[TestMethod]
		public void ShouldParseCardWithBirthday()
		{
			// Arrange
			string xmlWithBirthday = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/personal/contact.vcf</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""card-1""</d:getetag>
		<card:address-data>BEGIN:VCARD
VERSION:3.0
UID:bd-contact
FN:Jane Doe
N:Doe;Jane;;;
BDAY:19850620
EMAIL:jane@example.com
END:VCARD</card:address-data>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var addressBookUri = new Uri("https://example.com/addressbooks/personal/");

			// Act
			var cards = Parser.CardList(xmlWithBirthday, addressBookUri);

			// Assert
			Assert.HasCount(1, cards);
			var card = cards.First();
			Assert.IsNotNull(card.Birthday);
			Assert.AreEqual(new DateTime(1985, 6, 20), card.Birthday);
		}

		[TestMethod]
		public void ShouldParseCardWithoutBirthday()
		{
			// Act
			var cards = Parser.CardList(_cardXml, new Uri("https://example.com/addressbooks/personal/"));

			// Assert
			Assert.HasCount(1, cards);
			var card = cards.First();
			Assert.IsNotNull(card.Birthday);
		}

		[TestMethod]
		public void ShouldTrimQuotesFromCardETag()
		{
			// Arrange
			var addressBookUri = new Uri("https://example.com/addressbooks/personal/");

			// Act
			var cards = Parser.CardList(_cardXml, addressBookUri);

			// Assert
			var card = cards.First();
			Assert.AreEqual("card-1", card.ETag);
			Assert.DoesNotContain("\"", card.ETag);
		}

		[TestMethod]
		public void ShouldHandleFormattedNameFromN()
		{
			// Arrange
			string xmlWithoutFN = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<d:multistatus xmlns:d=""DAV:"" xmlns:card=""urn:ietf:params:xml:ns:carddav"">
  <d:response>
	<d:href>/addressbooks/personal/contact.vcf</d:href>
	<d:propstat>
	  <d:prop>
		<d:getetag>""card-1""</d:getetag>
		<card:address-data>BEGIN:VCARD
VERSION:3.0
UID:n-contact
N:Smith;Robert;;;
NICKNAME:Rob
EMAIL:robert@example.com
END:VCARD</card:address-data>
	  </d:prop>
	</d:propstat>
  </d:response>
</d:multistatus>";
			var addressBookUri = new Uri("https://example.com/addressbooks/personal/");

			// Act
			var cards = Parser.CardList(xmlWithoutFN, addressBookUri);

			// Assert
			Assert.HasCount(1, cards);
			var card = cards.First();
			Assert.IsNotNull(card.DisplayName);
			Assert.IsTrue(card.DisplayName.Contains("Smith") || card.DisplayName.Contains("Robert"));
		}

		#endregion CardList Tests
	}
}
