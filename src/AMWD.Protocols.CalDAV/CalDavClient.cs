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
using AMWD.Protocols.CalDAV.Xml;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;

namespace AMWD.Protocols.CalDAV
{
	/// <inheritdoc cref="ICalDavClient"/>
	public class CalDavClient : ICalDavClient, IDisposable
	{
		private const string XmlMimeType = "application/xml";
		private readonly HttpMethod _httpMethodPropfind = new("PROPFIND");
		private readonly HttpMethod _httpMethodMkCalendar = new("MKCALENDAR");
		private readonly HttpMethod _httpMethodReport = new("REPORT");

		private bool _isDisposed;

		private readonly Uri _baseUri;
		private readonly HttpClient _httpClient;
		private readonly CalendarSerializer _iCalSerializer = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="CalDavClient"/> class with the specified base URL and optional credentials.
		/// </summary>
		/// <param name="baseUrl">The base URL of the CalDAV server.</param>
		/// <param name="username">The username for authentication (optional).</param>
		/// <param name="password">The password for authentication (optional).</param>
		public CalDavClient(string baseUrl, string? username = null, string? password = null)
		{
			if (string.IsNullOrWhiteSpace(baseUrl))
				throw new ArgumentException("A base URL is required.", nameof(baseUrl));

			string version = FileVersionInfo.GetVersionInfo(typeof(CalDavClient).Assembly.Location).ProductVersion;

			_baseUri = new Uri(baseUrl);
			_httpClient = new HttpClient();

			_httpClient.DefaultRequestHeaders.Clear();
			_httpClient.DefaultRequestHeaders.Add("User-Agent", $"AMWD.Protocols.CalDAV/{version}");

			if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
			{
				string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
			}
		}

		/// <summary>
		/// Disposes the resources used by the <see cref="CalDavClient"/> instance.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			_httpClient.Dispose();

			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public Uri? PrincipalUri { get; private set; }

		/// <inheritdoc/>
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

		#region Calendar Management

		/// <inheritdoc/>
		public async Task<IReadOnlyCollection<DavCalendar>> GetCalendarsAsync(CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			var request = new HttpRequestMessage(_httpMethodPropfind, PrincipalUri);
			request.Headers.Add("Depth", "1");
			request.Content = new StringContent(Generator.FindCalendars(), Encoding.UTF8, XmlMimeType);

			var response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();

			string xmlResponse = await response.Content.ReadAsStringAsync();
			return Parser.CalendarList(xmlResponse, PrincipalUri);
		}

		/// <inheritdoc/>
		public async Task<bool> CreateCalendarAsync(CreateCalendarRequest request, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (string.IsNullOrWhiteSpace(request.Name))
				throw new ArgumentNullException(nameof(request.Name), "A calendar name (url-path) is required.");

			if (Regex.IsMatch(request.Name, @"[^a-z0-9_-]"))
				throw new ArgumentException("Calendar name (url-path) can only contain lowercase letters, numbers, hyphens and underscores.", nameof(request.Name));

			// Alternative for short hex codes: @"^#(?:[0-9a-fA-F]{3}){1,2}$"
			if (!string.IsNullOrWhiteSpace(request.Color) && !Regex.IsMatch(request.Color, @"^#([0-9a-fA-F]{6})$"))
				throw new ArgumentException("Calendar color must be a valid hex color code (#RRGGBB, e.g. #aBc123).", nameof(request.Color));

			var existingCalendars = await GetCalendarsAsync(cancellationToken);
			if (existingCalendars.Any(c => c.Uri?.ToString().EndsWith($"/{request.Name}/") == true))
				throw new InvalidOperationException($"A calendar with the name '{request.Name}' already exists.");

			var calendarUri = new Uri(PrincipalUri, request.Name + "/");

			var httpRequest = new HttpRequestMessage(_httpMethodMkCalendar, calendarUri)
			{
				Content = new StringContent(Generator.CreateCalendar(request.DisplayName, request.Description, request.Color), Encoding.UTF8, XmlMimeType)
			};

			var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
			return httpResponse.IsSuccessStatusCode;
		}

		/// <inheritdoc/>
		public async Task<bool> DeleteCalendarAsync(DavCalendar calendar, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (calendar == null)
				throw new ArgumentNullException(nameof(calendar), "A calendar is required.");

			var existingCalendars = await GetCalendarsAsync(cancellationToken);
			var calendarUri = existingCalendars
				.Where(c => c.Uri?.ToString().EndsWith($"/{calendar.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();

			if (calendarUri == null)
				throw new InvalidOperationException($"A calendar with the name '{calendar.Name}' does not exist.");

			var request = new HttpRequestMessage(HttpMethod.Delete, calendarUri);
			if (!string.IsNullOrWhiteSpace(calendar.ETag))
				request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{calendar.ETag}\""));

			var response = await _httpClient.SendAsync(request, cancellationToken);
			return response.IsSuccessStatusCode;
		}

		#endregion Calendar Management

		#region Event Management

		/// <inheritdoc/>
		public async Task<IReadOnlyCollection<DavEvent>> GetEventsAsync(DavCalendar calendar, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (calendar == null)
				throw new ArgumentNullException(nameof(calendar), "A calendar is required.");

			var existingCalendars = await GetCalendarsAsync(cancellationToken);
			var calendarUri = existingCalendars
				.Where(c => c.Uri?.ToString().EndsWith($"/{calendar.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();

			if (calendarUri == null)
				throw new InvalidOperationException($"A calendar with the name '{calendar.Name}' does not exist.");

			var request = new HttpRequestMessage(_httpMethodReport, calendarUri);
			request.Headers.Add("Depth", "1");
			request.Content = new StringContent(Generator.FindEvents(start, end), Encoding.UTF8, XmlMimeType);

			var response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();

			string xmlResponse = await response.Content.ReadAsStringAsync();
			return Parser.EventList(xmlResponse, calendarUri);
		}

		/// <inheritdoc/>
		public async Task<Uri> CreateEventAsync(DavCalendar calendar, CalendarEvent iCalEvent, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (calendar == null)
				throw new ArgumentNullException(nameof(calendar), "A calendar is required.");

			var existingCalendars = await GetCalendarsAsync(cancellationToken);
			var calendarUri = existingCalendars
				.Where(c => c.Uri?.ToString().EndsWith($"/{calendar.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();

			if (calendarUri == null)
				throw new InvalidOperationException($"A calendar with the name '{calendar.Name}' does not exist.");

			iCalEvent.Uid ??= Guid.NewGuid().ToString();
			var eventUri = new Uri(calendarUri, $"{iCalEvent.Uid}.ics");

			var ical = new Calendar();
			ical.Events.Add(iCalEvent);

			string? content = _iCalSerializer.SerializeToString(ical);
			if (string.IsNullOrWhiteSpace(content))
				throw new InvalidOperationException("Failed to serialize the event.");

			var httpRequest = new HttpRequestMessage(HttpMethod.Put, eventUri);
			httpRequest.Headers.Add("If-None-Match", "*");
			httpRequest.Content = new StringContent(content, Encoding.UTF8, "text/calendar");

			var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
			response.EnsureSuccessStatusCode();

			return eventUri;
		}

		/// <inheritdoc/>
		public async Task<bool> UpdateEventAsync(DavCalendar calendar, CalendarEvent iCalEvent, string? eTag = null, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (calendar == null)
				throw new ArgumentNullException(nameof(calendar), "A calendar is required.");

			var existingCalendars = await GetCalendarsAsync(cancellationToken);
			var calendarUri = existingCalendars
				.Where(c => c.Uri?.ToString().EndsWith($"/{calendar.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();

			if (calendarUri == null)
				throw new InvalidOperationException($"A calendar with the name '{calendar.Name}' does not exist.");

			var eventUri = new Uri(calendarUri, $"{iCalEvent.Uid}.ics");

			var ical = new Calendar();
			ical.Events.Add(iCalEvent);

			string? content = _iCalSerializer.SerializeToString(ical);
			if (string.IsNullOrWhiteSpace(content))
				throw new InvalidOperationException("Failed to serialize the event.");

			var httpRequest = new HttpRequestMessage(HttpMethod.Put, eventUri);

			if (!string.IsNullOrWhiteSpace(eTag))
				httpRequest.Headers.Add("If-Match", $"\"{eTag}\"");

			httpRequest.Content = new StringContent(content, Encoding.UTF8, "text/calendar");

			var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
			return response.IsSuccessStatusCode;
		}

		/// <inheritdoc/>
		public async Task<bool> DeleteEventAsync(DavCalendar calendar, CalendarEvent iCalEvent, string? eTag = null, CancellationToken cancellationToken = default)
		{
			if (PrincipalUri == null && !await InitializeAsync(cancellationToken))
				throw new InvalidOperationException("Cannot run with an uninitialized client.");

			if (calendar == null)
				throw new ArgumentNullException(nameof(calendar), "A calendar is required.");

			var existingCalendars = await GetCalendarsAsync(cancellationToken);
			var calendarUri = existingCalendars
				.Where(c => c.Uri?.ToString().EndsWith($"/{calendar.Name}/") == true)
				.Select(c => c.Uri)
				.FirstOrDefault();

			if (calendarUri == null)
				throw new InvalidOperationException($"A calendar with the name '{calendar.Name}' does not exist.");

			var eventUri = new Uri(calendarUri, $"{iCalEvent.Uid}.ics");

			var httpRequest = new HttpRequestMessage(HttpMethod.Delete, eventUri);

			if (!string.IsNullOrWhiteSpace(eTag))
				httpRequest.Headers.Add("If-Match", $"\"{eTag}\"");

			var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
			return response.IsSuccessStatusCode;
		}

		#endregion Event Management

		#region Private Helpers

		private async Task<Uri?> DiscoverCurrentPrincipalAsync(CancellationToken cancellationToken)
		{
			var urls = new Queue<string>();
			urls.Enqueue(_baseUri.ToString());
			urls.Enqueue($"{_baseUri.ToString().TrimEnd('/')}/.well-known/caldav");

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

				if (!response.Headers.TryGetValues("DAV", out var davValues) || !davValues.Any(v => v.Contains("calendar-access")))
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
