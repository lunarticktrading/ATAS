using System.IO;
using System.Reflection;

namespace LunarTick.ATAS.Indicators.Helpers
{
    public static class SoundPackHelper
	{
		public const string DefaultAlertFile = "alert1.wav";

		public static string DefaultAlertFilePath()
		{
			string atasPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) ?? @"C:\Program Files (x86)\ATAS Platform";
			return System.IO.Path.Combine(atasPath, "Sounds");
        }

        public static string ResolveAlertFilePath(string filename, string alertFilePath)
		{
			if (string.IsNullOrWhiteSpace(filename))
                return string.Empty;

			if (filename.Contains(";"))
			{
				// Multiple alert files specified, pick one at random.
				var alerts = filename.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				var random = new Random();
				var idx = random.Next(0, alerts.Length);
				return ResolveAlertFilePath(alerts[idx], alertFilePath);
			}

			if (filename.Contains(Path.DirectorySeparatorChar))
			{
				// Absolute path specified, return specified path if it exists, otherwise default alert sound.
				if (File.Exists(filename))
				{
					return filename;
				}
			}
			else
			{
                // No path specified, try alertFilePath, then DefaultAlertFilePath, otherwise default alert sound.

                if (!string.IsNullOrWhiteSpace(alertFilePath))
				{
					var fullPath = Path.Combine(alertFilePath, filename);
					if (File.Exists(fullPath))
					{
						return fullPath;
					}

					fullPath = Path.Combine(DefaultAlertFilePath(), filename);
					if (File.Exists(fullPath))
					{
						return fullPath;
					}
				}
			}

            // Default alert sound if specified file could not be located.
            return Path.Combine(DefaultAlertFilePath(), DefaultAlertFile);
        }
    }
}
