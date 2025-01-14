// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service to retrieve value from an INI file.
	/// </summary>
	public interface IWindowsIniService
	{
		/// <summary>
		/// Gets parsed data of INI file of a folder with each section.
		/// </summary>
		/// <remarks>
		/// While there's Win32API GetPrivateProfileString, this API can only used for single key.
		/// <br/>
		/// For more information about INI format, visit <a href="https://en.wikipedia.org/wiki/INI_file"/>.
		/// </remarks>
		/// <param name="filePath">The INI file to look up.</param>
		/// <param name="dataItem">The data class to hold the INI data.</param>
		/// <returns>Returns true if succeeded; otherwise, false.</returns>
		List<IniSectionDataItem> GetData(string filePath);
	}
}
