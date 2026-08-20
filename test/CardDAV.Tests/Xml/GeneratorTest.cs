using AMWD.Protocols.CardDAV.Xml;
using DAV.Tests;

namespace CardDAV.Tests.Xml
{
	[TestClass]
	public class GeneratorTest
	{
		private string _displayName;
		private string _description;

		[TestInitialize]
		public void Initialize()
		{
			_displayName = "Contacts";
			_description = "Shared contacts address book";
		}

		[TestMethod]
		public void ShouldGenerateCurrentUserPrincipalRequest()
		{
			// Arrange

			// Act
			string xml = Generator.CurrentUserPrincipal();

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateExpectedPropertiesForAddressBooks()
		{
			// Arrange

			// Act
			string xml = Generator.FindAddressBooks();

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateMkColRequestForAddressBook()
		{
			// Arrange

			// Act
			string xml = Generator.CreateAddressBook(_displayName, _description);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateMkColRequestForAddressBookWithOnlyDisplayName()
		{
			// Arrange
			string displayName = "My Contacts";

			// Act
			string xml = Generator.CreateAddressBook(displayName, null);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateMkColRequestForAddressBookWithOnlyDescription()
		{
			// Arrange
			string description = "Personal address book";

			// Act
			string xml = Generator.CreateAddressBook(null, description);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateMkColRequestForAddressBookWithoutProperties()
		{
			// Arrange

			// Act
			string xml = Generator.CreateAddressBook(null, null);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateAddressBookQueryWithName()
		{
			// Arrange
			string name = "John Doe";

			// Act
			string xml = Generator.FindCards(name);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateAddressBookQueryWithoutName()
		{
			// Arrange

			// Act
			string xml = Generator.FindCards(null);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateAddressBookQueryWithEmptyName()
		{
			// Arrange
			string name = "   ";

			// Act
			string xml = Generator.FindCards(name);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}
	}
}
