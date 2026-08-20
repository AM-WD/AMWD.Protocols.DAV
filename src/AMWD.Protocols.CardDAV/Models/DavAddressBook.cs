using System;

namespace AMWD.Protocols.CardDAV
{
	/// <summary>
	/// Represents an address book resource in a CardDAV server.
	/// </summary>
	public class DavAddressBook
	{
		/// <summary>
		/// Gets or sets the name of the address book, typically derived from the last segment of the address book's URI.
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Gets or sets the display name of the address book, which is a human-readable name for the address book.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the description of the address book, providing additional information about the address book's purpose or content.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Gets or sets the URI of the address book, which is the unique identifier for the address book resource on the CardDAV server.
		/// </summary>
		public Uri? Uri { get; set; }

		/// <summary>
		/// Gets or sets the CTag of the address book, which is a unique identifier that changes whenever the address book's content changes, allowing clients to detect updates.
		/// </summary>
		public string? CTag { get; set; }

		/// <summary>
		/// Gets or sets the ETag of the address book, which is a unique identifier that changes whenever the address book's content changes, allowing clients to detect updates.
		/// </summary>
		public string? ETag { get; set; }
	}
}
