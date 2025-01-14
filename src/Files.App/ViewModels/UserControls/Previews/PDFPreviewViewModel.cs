// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Files.App.ViewModels.Previews
{
	public sealed class PDFPreviewViewModel : BasePreviewModel
	{
		private Visibility loadingBarVisibility;
		public Visibility LoadingBarVisibility
		{
			get => loadingBarVisibility;
			private set => SetProperty(ref loadingBarVisibility, value);
		}

		// The pips pager will crash when binding directly to Pages.Count, so count the pages here
		private int pageCount;
		public int PageCount
		{
			get => pageCount;
			set => SetProperty(ref pageCount, value);
		}

		public ObservableCollection<PageViewModel> Pages { get; } = [];

		public PDFPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public static bool ContainsExtension(string extension)
			=> extension is ".pdf";

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var fileStream = await Item.ItemFile.OpenReadAsync();
			var pdf = await PdfDocument.LoadFromStreamAsync(fileStream);
			_ = TryLoadPagesAsync(pdf, fileStream);

			var details = new List<FileProperty>
			{
				// Add the number of pages to the details
				GetFileProperty("PropertyPageCount", pdf.PageCount)
			};

			return details;
		}

		public async Task TryLoadPagesAsync(PdfDocument pdf, IRandomAccessStream fileStream)
		{
			try
			{
				await LoadPagesAsync(pdf);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}
			finally
			{
				fileStream.Dispose();
			}
		}

		private async Task LoadPagesAsync(PdfDocument pdf)
		{
			// This fixes an issue where loading an absurdly large PDF would take to much RAM and eventually cause a crash
			var limit = Math.Clamp(pdf.PageCount, 0, Constants.PreviewPane.PDFPageLimit);

			for (uint i = 0; i < limit; i++)
			{
				// Stop loading if the user has cancelled
				if (LoadCancelledTokenSource.Token.IsCancellationRequested)
					return;

				PdfPage page = pdf.GetPage(i);
				await page.PreparePageAsync();
				using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
				await page.RenderToStreamAsync(stream);

				BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
				using SoftwareBitmap sw = await decoder.GetSoftwareBitmapAsync();

				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					BitmapImage src = new();
					PageViewModel pageData = new()
					{
						PageImage = src,
						PageNumber = (int)i,
						PageImageSB = sw,
					};

					await src.SetSourceAsync(stream);
					Pages.Add(pageData);

					++PageCount;
				});
			}

			LoadingBarVisibility = Visibility.Collapsed;
		}
	}

	public struct PageViewModel
	{
		public int PageNumber { get; set; }

		public BitmapImage PageImage { get; set; }

		public SoftwareBitmap PageImageSB { get; set; }
	}
}
