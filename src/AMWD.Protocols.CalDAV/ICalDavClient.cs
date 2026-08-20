using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ical.Net.CalendarComponents;

namespace AMWD.Protocols.CalDAV
{
	/// <summary>
	/// Represents a client for interacting with a CalDAV server.
	/// </summary>
	public interface ICalDavClient
	{
		/// <summary>
		/// Gets the URI of the current principal (user) on the CalDAV server.
		/// </summary>
		Uri? PrincipalUri { get; }

		/// <summary>
		/// Initializes the <see cref="ICalDavClient"/> by discovering the current principal (user) on the CalDAV server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if initialization was successful; otherwise, <see langword="false"/>.</returns>
		Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the list of calendars associated with the current principal (user) on the CalDAV server.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		Task<IReadOnlyCollection<DavCalendar>> GetCalendarsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new calendar for the current principal (user) on the CalDAV server.
		/// </summary>
		/// <param name="request">The request containing the details of the calendar to be created.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the calendar was created successfully; otherwise, <see langword="false"/>.</returns>
		Task<bool> CreateCalendarAsync(CreateCalendarRequest request, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes an existing calendar for the current principal (user) on the CalDAV server.
		/// </summary>
		/// <param name="calendar">The calendar to delete.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the calendar was deleted successfully; otherwise, <see langword="false"/>.</returns>
		Task<bool> DeleteCalendarAsync(DavCalendar calendar, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the list of events from a specified calendar within an optional date range.
		/// </summary>
		/// <param name="calendar">The calendar from which to retrieve events.</param>
		/// <param name="start">The optional start date of the date range.</param>
		/// <param name="end">The optional end date of the date range.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		Task<IReadOnlyCollection<DavEvent>> GetEventsAsync(DavCalendar calendar, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new event in the specified calendar on the CalDAV server.
		/// </summary>
		/// <param name="calendar">The calendar in which to create the event.</param>
		/// <param name="iCalEvent">The event to create.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>The <see cref="Uri"/> of the created event.</returns>
		Task<Uri> CreateEventAsync(DavCalendar calendar, CalendarEvent iCalEvent, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates an existing event in the specified calendar on the CalDAV server.
		/// </summary>
		/// <param name="calendar">The calendar containing the event to update.</param>
		/// <param name="iCalEvent">The event to update.</param>
		/// <param name="eTag">The ETag of the event for concurrency control.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the event was successfully updated; otherwise, <see langword="false"/>.</returns>
		Task<bool> UpdateEventAsync(DavCalendar calendar, CalendarEvent iCalEvent, string? eTag = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes an existing event from the specified calendar on the CalDAV server.
		/// </summary>
		/// <param name="calendar">The calendar containing the event to delete.</param>
		/// <param name="iCalEvent">The event to delete.</param>
		/// <param name="eTag">The ETag of the event for concurrency control.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><see langword="true"/> if the event was successfully deleted; otherwise, <see langword="false"/>.</returns>
		Task<bool> DeleteEventAsync(DavCalendar calendar, CalendarEvent iCalEvent, string? eTag = null, CancellationToken cancellationToken = default);
	}
}
