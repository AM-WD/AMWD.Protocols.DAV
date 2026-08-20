using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.CardDAV.Xml;
using vCard.Net.CardComponents;
using vCard.Net.Serialization;

namespace AMWD.Protocols.CardDAV
{
	/// <summary>
	/// Represents a client for interacting with a CardDAV server.
	/// </summary>
	public class CardDavClient : IDisposable
	{
		private const string XmlMimeType = "application/xml";
		private readonly HttpMethod _httpMethodPropfind = new("PROPFIND");
		private readonly HttpMethod _httpMethodMkCol = new("MKCOL");
		private readonly HttpMethod _httpMethodReport = new("REPORT");

		private bool _isDisposed;

		private readonly Uri _baseUri;
		private readonly HttpClient _httpClient;
		private readonly ComponentSerializer _vCardSerializer = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="CardDavClient"/> class with the specified base URL and optional credentials.
		/// </summary>
		/// <param name="baseUrl">The base URL of the CardDAV server.</param>
		/// <param name="username">The username for authentication (optional).</param>
		/// <param name="password">The password for authentication (optional).</param>
		public CardDavClient(string baseUrl, string? username = null, string? password = null)
		{
			if (string.IsNullOrWhiteSpace(baseUrl))
				throw new ArgumentException("A base URL is required.", nameof(baseUrl));

			string version = FileVersionInfo.GetVersionInfo(typeof(CardDavClient).Assembly.Location).ProductVersion;

			_baseUri = new Uri(baseUrl);
			_httpClient = new HttpClient();

			_httpClient.DefaultRequestHeaders.Clear();
			_httpClient.DefaultRequestHeaders.Add("User-Agent", $"AMWD.Protocols.CardDAV/{version}");

			if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
			{
				string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
			}
		}

		/// <summary>
		/// Disposes the resources used by the <see cref="CardDavClient"/> instance.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			_httpClient.Dispose();

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets the URI of the current principal (user) on the CardDAV server.
		/// </summary>
		public Uri? PrincipalUri { get; private set; }

		/// <summary>
		/// Initializes the <see cref="CardDavClient"/> by discovering the current principal (user) on the CardDAV server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if initialization was successful; otherwise, <see langword="false"/>.</returns>
		public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				PrincipalUri = await DiscoverCurrentPrincipalAsync(cancellationToken);
				if (PrincipalUri == null)
					return false;

				return true;
			}
			catch
			{
				return false;
			}
		}

		#region Address Book Management

		/// <summary>
		/// Retrieves the list of address books associated with the current principal (user) on the CardDAV server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		public async Task<IReadOnlyCollection<DavAddressBook>> GetAddressBooksAsync(CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			var request = new HttpRequestMessage(_httpMethodPropfind, PrincipalUri);
			request.Headers.Add("Depth", "1");
			request.Content = new StringContent(Generator.FindAddressBooks(), Encoding.UTF8, XmlMimeType);

			var response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();

			string xmlResponse = await response.Content.ReadAsStringAsync();
			return Parser.AddressBookList(xmlResponse, PrincipalUri);
		}

		/// <summary>
		/// Creates a new address book on the CardDAV server for the current principal (user).
		/// </summary>
		/// <param name="request">The request containing the details of the address book to create.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the address book was created successfully; otherwise, <see langword="false"/>.</returns>
		public async Task<bool> CreateAddressBookAsync(CreateAddressBookRequest request, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (string.IsNullOrWhiteSpace(request.Name))
				throw new ArgumentNullException(nameof(request.Name), "A calendar name (url-path) is required.");

			if (Regex.IsMatch(request.Name, @"[^a-z0-9_-]"))
				throw new ArgumentException("Address book name (url-path) can only contain lowercase letters, numbers, hyphens and underscores.", nameof(request.Name));

			var existingAddressBooks = await GetAddressBooksAsync(cancellationToken);
			if (existingAddressBooks.Any(c => c.Uri?.ToString().EndsWith($"/{request.Name}/") == true))
				throw new InvalidOperationException($"An address book with the name '{request.Name}' already exists.");

			var addressBookUri = new Uri(PrincipalUri, request.Name + "/");

			var httpRequest = new HttpRequestMessage(_httpMethodMkCol, addressBookUri)
			{
				Content = new StringContent(Generator.CreateAddressBook(request.DisplayName, request.Description), Encoding.UTF8, XmlMimeType)
			};

			var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
			return httpResponse.IsSuccessStatusCode;
		}

		/// <summary>
		/// Deletes an existing address book from the CardDAV server for the current principal (user).
		/// </summary>
		/// <param name="addressBook">The address book to delete.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the address book was deleted successfully; otherwise, <see langword="false"/>.</returns>
		public async Task<bool> DeleteAddressBookAsync(DavAddressBook addressBook, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (addressBook == null)
				throw new ArgumentNullException(nameof(addressBook), "An address book is required.");

			var existingAddressBooks = await GetAddressBooksAsync(cancellationToken);
			var addressBookUri = existingAddressBooks
				.Where(c => c.Uri?.ToString().EndsWith($"/{addressBook.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();

			if (addressBookUri == null)
				throw new InvalidOperationException($"An address book with the name '{addressBook.Name}' does not exist.");

			var request = new HttpRequestMessage(HttpMethod.Delete, addressBookUri);
			if (!string.IsNullOrWhiteSpace(addressBook.ETag))
				request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{addressBook.ETag}\""));

			var response = await _httpClient.SendAsync(request, cancellationToken);
			return response.IsSuccessStatusCode;
		}

		#endregion Address Book Management

		#region vCard Management

		/// <summary>
		/// Retrieves the list of vCards (contacts) from a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book from which to retrieve vCards.</param>
		/// <param name="name">The name of the vCard to filter for (optional).</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		public async Task<IReadOnlyCollection<DavCard>> GetCardsAsync(DavAddressBook addressBook, string? name = null, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (addressBook == null)
				throw new ArgumentNullException(nameof(addressBook), "An address book is required.");

			var existingAddressBooks = await GetAddressBooksAsync(cancellationToken);
			var addressBookUri = existingAddressBooks
				.Where(c => c.Uri?.ToString().EndsWith($"/{addressBook.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();

			if (addressBookUri == null)
				throw new InvalidOperationException($"An address book with the name '{addressBook.Name}' does not exist.");

			var request = new HttpRequestMessage(_httpMethodReport, addressBookUri);
			request.Headers.Add("Depth", "1");
			request.Content = new StringContent(Generator.FindCards(name), Encoding.UTF8, XmlMimeType);

			var response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();

			string xmlResponse = await response.Content.ReadAsStringAsync();
			return Parser.CardList(xmlResponse, addressBookUri);
		}

		/// <summary>
		/// Creates a new vCard (contact) in a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book in which to create the vCard.</param>
		/// <param name="vCard">The vCard to create.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The <see cref="Uri"/> of the newly created vCard.</returns>
		public async Task<Uri> CreateCardAsync(DavAddressBook addressBook, VCard vCard, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (addressBook == null)
				throw new ArgumentNullException(nameof(addressBook), "An address book is required.");

			var existingAddressBooks = await GetAddressBooksAsync(cancellationToken);
			var addressBookUri = existingAddressBooks
				.Where(c => c.Uri?.ToString().EndsWith($"/{addressBook.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();
			if (addressBookUri == null)
				throw new InvalidOperationException($"An address book with the name '{addressBook.Name}' does not exist.");

			vCard.Uid ??= Guid.NewGuid().ToString();
			var cardUri = new Uri(addressBookUri, vCard.Uid + ".vcf");

			string content = _vCardSerializer.SerializeToString(vCard);
			if (string.IsNullOrWhiteSpace(content))
				throw new InvalidOperationException("Failed to serialize the vCard.");

			var httpRequest = new HttpRequestMessage(HttpMethod.Put, cardUri);
			httpRequest.Headers.Add("If-None-Match", "*");
			httpRequest.Content = new StringContent(content, Encoding.UTF8, "text/vcard");

			var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
			response.EnsureSuccessStatusCode();

			return cardUri;
		}

		/// <summary>
		/// Updates an existing vCard (contact) in a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book containing the vCard to update.</param>
		/// <param name="vCard">The vCard to update.</param>
		/// <param name="eTag">The entity tag (ETag) of the vCard for concurrency control.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the update was successful; otherwise, <see langword="false"/>.</returns>
		public async Task<bool> UpdateCardAsync(DavAddressBook addressBook, VCard vCard, string? eTag = null, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (addressBook == null)
				throw new ArgumentNullException(nameof(addressBook), "An address book is required.");

			var existingAddressBooks = await GetAddressBooksAsync(cancellationToken);
			var addressBookUri = existingAddressBooks
				.Where(c => c.Uri?.ToString().EndsWith($"/{addressBook.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();
			if (addressBookUri == null)
				throw new InvalidOperationException($"An address book with the name '{addressBook.Name}' does not exist.");

			var cardUri = new Uri(addressBookUri, vCard.Uid + ".vcf");

			string content = _vCardSerializer.SerializeToString(vCard);
			if (string.IsNullOrWhiteSpace(content))
				throw new InvalidOperationException("Failed to serialize the vCard.");

			var httpRequest = new HttpRequestMessage(HttpMethod.Put, cardUri);

			if (!string.IsNullOrWhiteSpace(eTag))
				httpRequest.Headers.Add("If-Match", $"\"{eTag}\"");

			httpRequest.Content = new StringContent(content, Encoding.UTF8, "text/vcard");

			var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
			return response.IsSuccessStatusCode;
		}

		/// <summary>
		/// Deletes an existing vCard (contact) from a specified address book on the CardDAV server.
		/// </summary>
		/// <param name="addressBook">The address book containing the vCard to delete.</param>
		/// <param name="vCard">The vCard to delete.</param>
		/// <param name="eTag">The entity tag (ETag) of the vCard for concurrency control.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the deletion was successful; otherwise, <see langword="false"/>.</returns>
		public async Task<bool> DeleteCardAsync(DavAddressBook addressBook, VCard vCard, string? eTag = null, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (addressBook == null)
				throw new ArgumentNullException(nameof(addressBook), "An address book is required.");

			var existingAddressBooks = await GetAddressBooksAsync(cancellationToken);
			var addressBookUri = existingAddressBooks
				.Where(c => c.Uri?.ToString().EndsWith($"/{addressBook.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();
			if (addressBookUri == null)
				throw new InvalidOperationException($"An address book with the name '{addressBook.Name}' does not exist.");

			var cardUri = new Uri(addressBookUri, vCard.Uid + ".vcf");

			var httpRequest = new HttpRequestMessage(HttpMethod.Delete, cardUri);

			if (!string.IsNullOrWhiteSpace(eTag))
				httpRequest.Headers.Add("If-Match", $"\"{eTag}\"");

			var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
			return response.IsSuccessStatusCode;
		}

		#endregion vCard Management

		#region Private Helpers

		private async Task<Uri?> DiscoverCurrentPrincipalAsync(CancellationToken cancellationToken)
		{
			var urls = new Queue<string>();
			urls.Enqueue(_baseUri.ToString());
			urls.Enqueue($"{_baseUri.ToString().TrimEnd('/')}/.well-known/carddav");

			while (urls.Count > 0)
			{
				string url = urls.Dequeue();
				var request = new HttpRequestMessage(HttpMethod.Options, url);
				var response = await _httpClient.SendAsync(request, cancellationToken);

				if (response.StatusCode == HttpStatusCode.Moved || response.StatusCode == HttpStatusCode.Redirect)
				{
					string redirectUrl = response.Headers.Location.IsAbsoluteUri
						? response.Headers.Location.ToString()
						: new Uri(new Uri(url), response.Headers.Location).ToString();
					urls.Enqueue(redirectUrl);
					continue;
				}

				if (!response.IsSuccessStatusCode)
					continue;

				if (!response.Headers.TryGetValues("DAV", out var davValues) || !davValues.Any(v => v.Contains("addressbook")))
					continue;

				string resultUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

				request = new HttpRequestMessage(_httpMethodPropfind, resultUrl);
				request.Headers.Add("Depth", "0");
				request.Content = new StringContent(Generator.CurrentUserPrincipal(), Encoding.UTF8, XmlMimeType);

				response = await _httpClient.SendAsync(request, cancellationToken);
				if (!response.IsSuccessStatusCode)
					continue;

				string xmlResponse = await response.Content.ReadAsStringAsync();
				string? principalUrlPath = Parser.ExtractPathValue(xmlResponse, "//d:current-user-principal/d:href");

				if (string.IsNullOrWhiteSpace(principalUrlPath))
					continue;

				return principalUrlPath!.StartsWith("http")
					? new Uri(principalUrlPath)
					: new Uri(new Uri(url), principalUrlPath);
			}

			return null;
		}

		#endregion Private Helpers
	}
}
