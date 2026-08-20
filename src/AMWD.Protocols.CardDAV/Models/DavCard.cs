using System;
using vCard.Net.CardComponents;

namespace AMWD.Protocols.CardDAV
{
	/// <summary>
	/// Represents a vCard retrieved from a CardDAV server.
	/// </summary>
	public class DavCard
	{
		/// <summary>
		/// Gets or sets the unique identifier (UID) of the vCard,
		/// which is used to uniquely identify the contact across different address books and systems.
		/// </summary>
		public string UID { get; set; } = string.Empty;
		
		/// <summary>
		/// Gets or sets the display name of the contact.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the birthday of the contact, if available.
		/// </summary>
		public DateTime? Birthday { get; set; }

		/// <summary>
		/// Gets or sets the entity tag (ETag) of the vCard, which is used for concurrency control and caching.
		/// </summary>
		public string ETag { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the URI of the vCard, providing a reference to the contact's location on the server.
		/// </summary>
		public Uri? Href { get; set; }

		/// <summary>
		/// Gets or sets the raw vCard data as a string, which contains the complete vCard information in its original format.
		/// </summary>
		public string? RawCard { get; set; }

		/// <summary>
		/// Gets or sets the parsed vCard object, which provides structured access to the contact's information.
		/// </summary>
		public VCard? VCard { get; set; }
	}
}
