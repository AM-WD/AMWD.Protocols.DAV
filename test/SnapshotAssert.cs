using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DAV.Tests
{
	public static class SnapshotAssert
	{
		public static void AreEqual(string actual, string message = null, [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerMemberName = null)
		{
			string normalized = actual
				.Replace("\r\n", "\n")
				.Replace("\r", "\n");
			AreEqual(Encoding.UTF8.GetBytes(normalized), message, callerFilePath, callerMemberName);
		}

		public static void AreEqual(byte[] actual, string message = null, [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerMemberName = null)
		{
			string callerDir = Path.GetDirectoryName(callerFilePath);
			string callerFile = Path.GetFileNameWithoutExtension(callerFilePath);

			string snapshotDir = Path.Combine(callerDir, "Snapshots", callerFile);
			string snapshotFile = Path.Combine(snapshotDir, $"{callerMemberName}.snap");

			if (File.Exists(snapshotFile))
			{
				byte[] expected = File.ReadAllBytes(snapshotFile);

				if (message == null)
					CollectionAssert.AreEqual(expected, actual);
				else
					CollectionAssert.AreEqual(expected, actual, message);
			}
			else
			{
				if (!Directory.Exists(snapshotDir))
					Directory.CreateDirectory(snapshotDir);

				File.WriteAllBytes(snapshotFile, actual);
			}
		}
	}
}
