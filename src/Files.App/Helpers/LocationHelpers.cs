// Copyright(c) Files Community
// Licensed under the MIT License.

using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace Files.App.Helpers
{
	public static class LocationHelpers
	{
		public static async Task<string> GetAddressFromCoordinatesAsync(double? Lat, double? Lon)
		{
			if (!Lat.HasValue || !Lon.HasValue)
				return null;

			if (string.IsNullOrEmpty(MapService.ServiceToken))
			{
				try
				{
					MapService.ServiceToken = Constants.AutomatedWorkflowInjectionKeys.BingMapsSecret;
				}
				catch (Exception)
				{
					return null;
				}
			}

			BasicGeoposition location = new BasicGeoposition();
			location.Latitude = Lat.Value;
			location.Longitude = Lon.Value;
			Geopoint pointToReverseGeocode = new Geopoint(location);

			// Reverse geocode the specified geographic location.

			var result = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);
			return result?.Locations?.FirstOrDefault()?.DisplayName;
		}
	}
}
