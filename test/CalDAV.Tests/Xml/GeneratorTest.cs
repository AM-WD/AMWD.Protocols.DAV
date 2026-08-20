using System;
using AMWD.Protocols.CalDAV.Xml;
using DAV.Tests;

namespace CalDAV.Tests.Xml
{
	[TestClass]
	public class GeneratorTest
	{
		private string _displayName;
		private string _description;
		private DateTimeOffset _start;
		private DateTimeOffset _end;

		[TestInitialize]
		public void Initialize()
		{
			_displayName = "Team Calendar";
			_description = "Shared team calendar";
			_start = new DateTimeOffset(2025, 1, 10, 8, 0, 0, TimeSpan.Zero);
			_end = new DateTimeOffset(2025, 2, 15, 8, 0, 0, TimeSpan.Zero);
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
		public void ShouldGenerateExpectedPropertiesForCalendars()
		{
			// Arrange

			// Act
			string xml = Generator.FindCalendars();

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateMkCalendarRequest()
		{
			// Arrange

			// Act
			string xml = Generator.CreateCalendar(_displayName, _description);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldGenerateMatchingFiltersWhenTimeRangeIsProvided()
		{
			// Arrange

			// Act
			string xml = Generator.FindEvents(_start, _end);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}

		[TestMethod]
		public void ShouldUseDefaultRangeWhenOnlyStartIsProvided()
		{
			// Arrange
			var start = new DateTimeOffset(2025, 5, 1, 9, 0, 0, TimeSpan.Zero);

			// Act
			string xml = Generator.FindEvents(start, null);

			// Assert
			SnapshotAssert.AreEqual(xml);
		}
	}
}
