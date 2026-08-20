using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.CalDAV;
using Ical.Net.CalendarComponents;
using Moq;
using Moq.Protected;

namespace CalDAV.Tests
{
	[TestClass]
	public class CalDavClientTest
	{
		public TestContext TestContext { get; set; }

		private Queue<HttpResponseMessage> _responses = null!;
		private List<HttpRequestMessage> _requests = null!;
		private CalDavClient _client = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			_responses = new Queue<HttpResponseMessage>();
			(_client, _requests) = GetClient();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_client.Dispose();
		}

		[TestMethod]
		public void ShouldThrowForEmptyBaseUrl()
		{
			// Arrange

			// Act
			var exception = Assert.ThrowsExactly<ArgumentException>(() => _ = new CalDavClient(" "));

			// Assert
			Assert.IsNotNull(exception);
			Assert.AreEqual("A base URL is required. (Parameter 'baseUrl')", exception.Message);
			Assert.AreEqual("baseUrl", exception.ParamName);
		}

		[TestMethod]
		public async Task ShouldInitializeAndSetPrincipalUri()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")));

			// Act
			bool result = await _client.InitializeAsync(TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(new Uri("https://example.com/principals/user/"), _client.PrincipalUri);
			Assert.HasCount(2, _requests);
			Assert.AreEqual(HttpMethod.Options, _requests[0].Method);
			Assert.AreEqual("PROPFIND", _requests[1].Method.Method);
			Assert.AreEqual("0", string.Join(",", _requests[1].Headers.GetValues("Depth")));
		}

		[TestMethod]
		public async Task ShouldInitializeUsingRedirectedUrl()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.Moved, headers: new Dictionary<string, string> { ["Location"] = "/dav/" }),
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("https://example.com/principals/redirected/")));

			// Act
			bool result = await _client.InitializeAsync(TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(new Uri("https://example.com/principals/redirected/"), _client.PrincipalUri);
			Assert.HasCount(3, _requests);
			Assert.AreEqual(new Uri("https://example.com/"), _requests[0].RequestUri);
			Assert.AreEqual(new Uri("https://example.com/.well-known/caldav"), _requests[1].RequestUri);
		}

		[TestMethod]
		public async Task ShouldReturnFalseWhenInitializeFails()
		{
			// Arrange
			// no responses

			// Act
			bool result = await _client.InitializeAsync(TestContext.CancellationToken);

			// Assert
			Assert.IsFalse(result);
			Assert.IsNull(_client.PrincipalUri);
		}

		[TestMethod]
		public async Task ShouldThrowWhenGetCalendarsCannotInitialize()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK),
				CreateResponse(HttpStatusCode.OK));

			// Act
			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => _ = await _client.GetCalendarsAsync(TestContext.CancellationToken));

			// Assert
			Assert.IsNotNull(exception);
			Assert.AreEqual("Cannot run with an uninitialized client.", exception.Message);
		}

		[TestMethod]
		public async Task ShouldGetCalendarsUsingPropfindDepthOne()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work")));

			// Act
			var calendars = await _client.GetCalendarsAsync(TestContext.CancellationToken);

			// Assert
			Assert.HasCount(1, calendars);
			Assert.HasCount(3, _requests);
			Assert.AreEqual("PROPFIND", _requests[2].Method.Method);
			Assert.AreEqual(new Uri("https://example.com/principals/user/"), _requests[2].RequestUri);
			Assert.AreEqual("1", string.Join(",", _requests[2].Headers.GetValues("Depth")));
		}

		[TestMethod]
		public async Task ShouldThrowWhenCreateCalendarNameContainsInvalidCharacters()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")));
			var createRequest = new CreateCalendarRequest("Private");

			// Act
			var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(async () => _ = await _client.CreateCalendarAsync(createRequest, TestContext.CancellationToken));

			// Assert
			Assert.IsNotNull(exception);
			Assert.AreEqual("Calendar name (url-path) can only contain lowercase letters, numbers, hyphens and underscores. (Parameter 'Name')", exception.Message);
		}

		[TestMethod]
		public async Task ShouldThrowWhenCreateCalendarAlreadyExists()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/private/", "Private")));
			var createRequest = new CreateCalendarRequest("private");

			// Act
			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => _ = await _client.CreateCalendarAsync(createRequest, TestContext.CancellationToken));

			// Assert
			Assert.IsNotNull(exception);
			Assert.AreEqual("A calendar with the name 'private' already exists.", exception.Message);
		}

		[TestMethod]
		public async Task ShouldCreateCalendarUsingMkCalendarRequest()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work")),
				CreateResponse(HttpStatusCode.Created));
			var createRequest = new CreateCalendarRequest("private")
			{
				DisplayName = "Private",
				Description = "Private Calendar",
			};

			// Act
			bool result = await _client.CreateCalendarAsync(createRequest, TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.HasCount(4, _requests);
			Assert.AreEqual("MKCALENDAR", _requests[3].Method.Method);
			Assert.AreEqual(new Uri("https://example.com/principals/user/private/"), _requests[3].RequestUri);
			string payload = await _requests[3].Content!.ReadAsStringAsync(TestContext.CancellationToken);
			Assert.Contains("Private", payload);
		}

		[TestMethod]
		public async Task ShouldDeleteCalendarWithoutIfMatchWhenNoEtag()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work")),
				CreateResponse(HttpStatusCode.NoContent));

			// Act
			bool result = await _client.DeleteCalendarAsync(new DavCalendar { Name = "work" }, TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(HttpMethod.Delete, _requests[3].Method);
			Assert.HasCount(0, _requests[3].Headers.IfMatch);
		}

		[TestMethod]
		public async Task ShouldDeleteCalendarUsingDeleteAndIfMatch()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work", "etag-1")),
				CreateResponse(HttpStatusCode.NoContent));

			// Act
			bool result = await _client.DeleteCalendarAsync(new DavCalendar
			{
				Name = "work",
				ETag = "etag-1"
			}, TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.HasCount(4, _requests);
			Assert.AreEqual(HttpMethod.Delete, _requests[3].Method);
			Assert.AreEqual(new Uri("https://example.com/calendars/user/work/"), _requests[3].RequestUri);
			Assert.AreEqual("\"etag-1\"", _requests[3].Headers.IfMatch.ToString());
		}

		[TestMethod]
		public async Task ShouldGetEventsUsingReportRequest()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work")),
				CreateResponse(HttpStatusCode.MultiStatus, EventListResponseXml("event-1.ics", "event-1")));

			// Act
			var events = await _client.GetEventsAsync(new DavCalendar { Name = "work" }, cancellationToken: TestContext.CancellationToken);

			// Assert
			Assert.HasCount(1, events);
			Assert.AreEqual("REPORT", _requests[3].Method.Method);
			Assert.AreEqual("1", string.Join(",", _requests[3].Headers.GetValues("Depth")));
		}

		[TestMethod]
		public async Task ShouldCreateEventAndSetIfNoneMatchHeader()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work")),
				CreateResponse(HttpStatusCode.Created));
			var calendarEvent = new CalendarEvent { Summary = "Created", Uid = null };

			// Act
			var eventUri = await _client.CreateEventAsync(new DavCalendar { Name = "work" }, calendarEvent, TestContext.CancellationToken);

			// Assert
			Assert.IsNotNull(calendarEvent.Uid);
			Assert.AreEqual(HttpMethod.Put, _requests[3].Method);
			Assert.AreEqual("*", string.Join(",", _requests[3].Headers.GetValues("If-None-Match")));
			Assert.EndsWith(".ics", eventUri.ToString());
		}

		[TestMethod]
		public async Task ShouldUpdateEventWithIfMatchWhenEtagProvided()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work")),
				CreateResponse(HttpStatusCode.NoContent));
			var calendarEvent = new CalendarEvent { Uid = "event-1", Summary = "Updated" };

			// Act
			bool result = await _client.UpdateEventAsync(new DavCalendar { Name = "work" }, calendarEvent, "etag-1", TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(HttpMethod.Put, _requests[3].Method);
			Assert.AreEqual("\"etag-1\"", string.Join(",", _requests[3].Headers.GetValues("If-Match")));
		}

		[TestMethod]
		public async Task ShouldDeleteEventWithoutIfMatchWhenNoEtag()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "calendar-access" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, CalendarListResponseXml("/calendars/user/work/", "Work")),
				CreateResponse(HttpStatusCode.NoContent));
			var calendarEvent = new CalendarEvent { Uid = "event-1" };

			// Act
			bool result = await _client.DeleteEventAsync(new DavCalendar { Name = "work" }, calendarEvent, cancellationToken: TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(HttpMethod.Delete, _requests[3].Method);
			Assert.IsFalse(_requests[3].Headers.Contains("If-Match"));
		}

		[TestMethod]
		public void ShouldDisposeMultipleTimes()
		{
			// Arrange

			// Act
			_client.Dispose();
			_client.Dispose();

			// Assert
		}

		private void EnqueueResponses(params HttpResponseMessage[] responses)
		{
			foreach (var response in responses)
				_responses.Enqueue(response);
		}

		private (CalDavClient Client, List<HttpRequestMessage> Requests) GetClient()
		{
			var requests = new List<HttpRequestMessage>();
			var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Loose);

			handlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.Callback<HttpRequestMessage, CancellationToken>((request, _) => requests.Add(request))
				.ReturnsAsync(() => _responses.Count > 0
					? _responses.Dequeue()
					: throw new InvalidOperationException("No mocked response configured."));

			var client = new CalDavClient("https://example.com/");
			var field = typeof(CalDavClient).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new MissingFieldException(nameof(CalDavClient), "_httpClient");

			if (field.GetValue(client) is HttpClient originalHttpClient)
				originalHttpClient.Dispose();

			field.SetValue(client, new HttpClient(handlerMock.Object));

			return (client, requests);
		}

		private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content = null, Dictionary<string, string> headers = null)
		{
			var response = new HttpResponseMessage(statusCode);
			if (content != null)
				response.Content = new StringContent(content, Encoding.UTF8, "application/xml");

			if (headers != null)
			{
				foreach (var header in headers)
					response.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}

			return response;
		}

		private static string CurrentPrincipalResponseXml(string principalHref)
			=> $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><d:multistatus xmlns:d=\"DAV:\"><d:response><d:propstat><d:prop><d:current-user-principal><d:href>{principalHref}</d:href></d:current-user-principal></d:prop></d:propstat></d:response></d:multistatus>";

		private static string CalendarListResponseXml(string href, string displayName, string eTag = null)
		{
			string eTagPart = string.IsNullOrWhiteSpace(eTag) ? string.Empty : $"<cs:getetag>\"{eTag}\"</cs:getetag>";
			return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><d:multistatus xmlns:d=\"DAV:\" xmlns:cal=\"urn:ietf:params:xml:ns:caldav\" xmlns:cs=\"http://calendarserver.org/ns/\"><d:response><d:href>{href}</d:href><d:propstat><d:prop><d:resourcetype><d:collection/><cal:calendar/></d:resourcetype><d:displayname>{displayName}</d:displayname>{eTagPart}</d:prop></d:propstat></d:response></d:multistatus>";
		}

		private static string EventListResponseXml(string href, string uid)
			=> $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><d:multistatus xmlns:d=\"DAV:\" xmlns:cal=\"urn:ietf:params:xml:ns:caldav\"><d:response><d:href>{href}</d:href><d:propstat><d:prop><d:getetag>\"etag-event\"</d:getetag><cal:calendar-data>BEGIN:VCALENDAR\nBEGIN:VEVENT\nUID:{uid}\nSUMMARY:Demo\nDTSTART:20260101T100000Z\nDTEND:20260101T110000Z\nEND:VEVENT\nEND:VCALENDAR</cal:calendar-data></d:prop></d:propstat></d:response></d:multistatus>";
	}
}
