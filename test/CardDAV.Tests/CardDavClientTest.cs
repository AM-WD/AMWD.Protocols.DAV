using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.CardDAV;
using Moq;
using Moq.Protected;
using vCard.Net.CardComponents;
using vCard.Net.DataTypes;

namespace CardDAV.Tests
{
	[TestClass]
	public class CardDavClientTest
	{
		public TestContext TestContext { get; set; }

		private Queue<HttpResponseMessage> _responses = null!;
		private List<HttpRequestMessage> _requests = null!;
		private CardDavClient _client = null!;

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
			var exception = Assert.ThrowsExactly<ArgumentException>(() => _ = new CardDavClient(" "));

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
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, addressbook" }),
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
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("https://example.com/principals/redirected/")));

			// Act
			bool result = await _client.InitializeAsync(TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(new Uri("https://example.com/principals/redirected/"), _client.PrincipalUri);
			Assert.HasCount(3, _requests);
			Assert.AreEqual(new Uri("https://example.com/"), _requests[0].RequestUri);
			Assert.AreEqual(new Uri("https://example.com/.well-known/carddav"), _requests[1].RequestUri);
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
		public async Task ShouldThrowWhenGetAddressBooksCannotInitialize()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK),
				CreateResponse(HttpStatusCode.OK));

			// Act
			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => _ = await _client.GetAddressBooksAsync(TestContext.CancellationToken));

			// Assert
			Assert.IsNotNull(exception);
			Assert.AreEqual("Cannot run with an uninitialized client.", exception.Message);
		}

		[TestMethod]
		public async Task ShouldGetAddressBooksUsingPropfindDepthOne()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/personal/", "Personal")));

			// Act
			var addressBooks = await _client.GetAddressBooksAsync(TestContext.CancellationToken);

			// Assert
			Assert.HasCount(1, addressBooks);
			Assert.HasCount(3, _requests);
			Assert.AreEqual("PROPFIND", _requests[2].Method.Method);
			Assert.AreEqual(new Uri("https://example.com/principals/user/"), _requests[2].RequestUri);
			Assert.AreEqual("1", string.Join(",", _requests[2].Headers.GetValues("Depth")));
		}

		[TestMethod]
		public async Task ShouldThrowWhenCreateAddressBookNameContainsInvalidCharacters()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")));
			var createRequest = new CreateAddressBookRequest("Personal");

			// Act
			var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(async () => _ = await _client.CreateAddressBookAsync(createRequest, TestContext.CancellationToken));

			// Assert
			Assert.IsNotNull(exception);
			Assert.AreEqual("Address book name (url-path) can only contain lowercase letters, numbers, hyphens and underscores. (Parameter 'Name')", exception.Message);
		}

		[TestMethod]
		public async Task ShouldThrowWhenCreateAddressBookAlreadyExists()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/personal/", "Personal")));
			var createRequest = new CreateAddressBookRequest("personal");

			// Act
			var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => _ = await _client.CreateAddressBookAsync(createRequest, TestContext.CancellationToken));

			// Assert
			Assert.IsNotNull(exception);
			Assert.AreEqual("An address book with the name 'personal' already exists.", exception.Message);
		}

		[TestMethod]
		public async Task ShouldCreateAddressBookUsingMkColRequest()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/work/", "Work")),
				CreateResponse(HttpStatusCode.Created));
			var createRequest = new CreateAddressBookRequest("personal")
			{
				DisplayName = "Personal",
				Description = "Personal Address Book",
			};

			// Act
			bool result = await _client.CreateAddressBookAsync(createRequest, TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.HasCount(4, _requests);
			Assert.AreEqual("MKCOL", _requests[3].Method.Method);
			Assert.AreEqual(new Uri("https://example.com/principals/user/personal/"), _requests[3].RequestUri);
			string payload = await _requests[3].Content!.ReadAsStringAsync(TestContext.CancellationToken);
			Assert.Contains("Personal", payload);
		}

		[TestMethod]
		public async Task ShouldDeleteAddressBookWithoutIfMatchWhenNoEtag()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/work/", "Work")),
				CreateResponse(HttpStatusCode.NoContent));

			// Act
			bool result = await _client.DeleteAddressBookAsync(new DavAddressBook { Name = "work" }, TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(HttpMethod.Delete, _requests[3].Method);
			Assert.HasCount(0, _requests[3].Headers.IfMatch);
		}

		[TestMethod]
		public async Task ShouldDeleteAddressBookUsingDeleteAndIfMatch()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "1, addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/work/", "Work", "etag-1")),
				CreateResponse(HttpStatusCode.NoContent));

			// Act
			bool result = await _client.DeleteAddressBookAsync(new DavAddressBook
			{
				Name = "work",
				ETag = "etag-1"
			}, TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.HasCount(4, _requests);
			Assert.AreEqual(HttpMethod.Delete, _requests[3].Method);
			Assert.AreEqual(new Uri("https://example.com/addressbooks/user/work/"), _requests[3].RequestUri);
			Assert.AreEqual("\"etag-1\"", _requests[3].Headers.IfMatch.ToString());
		}

		[TestMethod]
		public async Task ShouldGetCardsUsingReportRequest()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/work/", "Work")),
				CreateResponse(HttpStatusCode.MultiStatus, CardListResponseXml("contact.vcf", "john-doe-123")));

			// Act
			var cards = await _client.GetCardsAsync(new DavAddressBook { Name = "work" }, cancellationToken: TestContext.CancellationToken);

			// Assert
			Assert.HasCount(1, cards);
			Assert.AreEqual("REPORT", _requests[3].Method.Method);
			Assert.AreEqual("1", string.Join(",", _requests[3].Headers.GetValues("Depth")));
		}

		[TestMethod]
		public async Task ShouldCreateCardAndSetIfNoneMatchHeader()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/work/", "Work")),
				CreateResponse(HttpStatusCode.Created));
			var vCard = new VCard { FormattedName = "John Doe", Uid = null, Version = VCardVersion.vCard3_0 };

			// Act
			var cardUri = await _client.CreateCardAsync(new DavAddressBook { Name = "work" }, vCard, TestContext.CancellationToken);

			// Assert
			Assert.IsNotNull(vCard.Uid);
			Assert.AreEqual(HttpMethod.Put, _requests[3].Method);
			Assert.AreEqual("*", string.Join(",", _requests[3].Headers.GetValues("If-None-Match")));
			Assert.EndsWith(".vcf", cardUri.ToString());
		}

		[TestMethod]
		public async Task ShouldUpdateCardWithIfMatchWhenEtagProvided()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/work/", "Work")),
				CreateResponse(HttpStatusCode.NoContent));
			var vCard = new VCard { Uid = "contact-1", FormattedName = "Updated", Version = VCardVersion.vCard3_0 };

			// Act
			bool result = await _client.UpdateCardAsync(new DavAddressBook { Name = "work" }, vCard, "etag-1", TestContext.CancellationToken);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(HttpMethod.Put, _requests[3].Method);
			Assert.AreEqual("\"etag-1\"", string.Join(",", _requests[3].Headers.GetValues("If-Match")));
		}

		[TestMethod]
		public async Task ShouldDeleteCardWithoutIfMatchWhenNoEtag()
		{
			// Arrange
			EnqueueResponses(
				CreateResponse(HttpStatusCode.OK, headers: new Dictionary<string, string> { ["DAV"] = "addressbook" }),
				CreateResponse(HttpStatusCode.MultiStatus, CurrentPrincipalResponseXml("/principals/user/")),
				CreateResponse(HttpStatusCode.MultiStatus, AddressBookListResponseXml("/addressbooks/user/work/", "Work")),
				CreateResponse(HttpStatusCode.NoContent));
			var vCard = new VCard { Uid = "contact-1" };

			// Act
			bool result = await _client.DeleteCardAsync(new DavAddressBook { Name = "work" }, vCard, cancellationToken: TestContext.CancellationToken);

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

		#region Helper Methods

		private void EnqueueResponses(params HttpResponseMessage[] responses)
		{
			foreach (var response in responses)
				_responses.Enqueue(response);
		}

		private (CardDavClient Client, List<HttpRequestMessage> Requests) GetClient()
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

			var client = new CardDavClient("https://example.com/");
			var field = typeof(CardDavClient).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new MissingFieldException(nameof(CardDavClient), "_httpClient");

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

		private static string AddressBookListResponseXml(string href, string displayName, string eTag = null)
		{
			string eTagPart = string.IsNullOrWhiteSpace(eTag) ? string.Empty : $"<d:getetag>\"{eTag}\"</d:getetag>";
			return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><d:multistatus xmlns:d=\"DAV:\" xmlns:card=\"urn:ietf:params:xml:ns:carddav\"><d:response><d:href>{href}</d:href><d:propstat><d:prop><d:resourcetype><d:collection/><card:addressbook/></d:resourcetype><d:displayname>{displayName}</d:displayname>{eTagPart}</d:prop></d:propstat></d:response></d:multistatus>";
		}

		private static string CardListResponseXml(string href, string uid)
			=> $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><d:multistatus xmlns:d=\"DAV:\" xmlns:card=\"urn:ietf:params:xml:ns:carddav\"><d:response><d:href>{href}</d:href><d:propstat><d:prop><d:getetag>\"etag-card\"</d:getetag><card:address-data>BEGIN:VCARD\nVERSION:3.0\nUID:{uid}\nFN:John Doe\nEMAIL:john@example.com\nEND:VCARD</card:address-data></d:prop></d:propstat></d:response></d:multistatus>";

		#endregion Helper Methods
	}
}
