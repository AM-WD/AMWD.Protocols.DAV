namespace AMWD.Protocols.CardDAV
{
	/// <summary>
	/// Represents a request to create a new address book on a CardDAV server.
	/// </summary>
	public class CreateAddressBookRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CreateAddressBookRequest"/> class with the specified name.
		/// </summary>
		/// <param name="name">The name of the address book.</param>
		public CreateAddressBookRequest(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets or sets the name of the address book, typically derived from the last segment of the address book's URI.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the display name of the address book, which is a human-readable name for the address book.
		/// </summary>
		public string? DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the description of the address book, providing additional information about the address book's purpose or content.
		/// </summary>
		public string? Description { get; set; }
	}
}
