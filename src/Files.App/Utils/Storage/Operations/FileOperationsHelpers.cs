// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Storage.Operations;
using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Tulpep.ActiveDirectoryObjectPicker;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Storage
{
	public sealed class FileOperationsHelpers
	{
		private static readonly Ole32.PROPERTYKEY PKEY_FilePlaceholderStatus = new Ole32.PROPERTYKEY(new Guid("B2F9B9D6-FEC4-4DD5-94D7-8957488C807B"), 2);
		private const uint PS_CLOUDFILE_PLACEHOLDER = 8;

		private static ProgressHandler? progressHandler; // Warning: must be initialized from a MTA thread

		public static Task SetClipboard(string[] filesToCopy, DataPackageOperation operation)
		{
			return Win32Helper.StartSTATask(() =>
			{
				System.Windows.Forms.Clipboard.Clear();
				var fileList = new System.Collections.Specialized.StringCollection();
				fileList.AddRange(filesToCopy);
				MemoryStream dropEffect = new MemoryStream(operation == DataPackageOperation.Copy ?
					[5, 0, 0, 0] : [2, 0, 0, 0]);
				var data = new System.Windows.Forms.DataObject();
				data.SetFileDropList(fileList);
				data.SetData("Preferred DropEffect", dropEffect);
				System.Windows.Forms.Clipboard.SetDataObject(data, true);
			});
		}

		public static Task<(bool, ShellOperationResult)> CreateItemAsync(string filePath, string fileOp, long ownerHwnd, bool asAdmin, string template = "", byte[]? dataBytes = null)
		{
			return Win32Helper.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations2();

				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.RenameOnCollision
							| ShellFileOperations.OperationFlags.NoErrorUI;
				if (asAdmin)
				{
					op.Options |= ShellFileOperations.OperationFlags.ShowElevationPrompt
								| ShellFileOperations.OperationFlags.RequireElevation;
				}
				op.OwnerWindow = (IntPtr)ownerHwnd;

				var shellOperationResult = new ShellOperationResult();

				if (!SafetyExtensions.IgnoreExceptions(() =>
				{
					using var shd = new ShellFolder(Path.GetDirectoryName(filePath));
					op.QueueNewItemOperation(shd, Path.GetFileName(filePath),
						fileOp == "CreateFolder" ? FileAttributes.Directory : FileAttributes.Normal, template);
				}))
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = false,
						Destination = filePath,
						HResult = -1
					});
				}

				var createTcs = new TaskCompletionSource<bool>();
				op.PostNewItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Destination = e.DestItem.GetParsingPath(),
						HResult = (int)e.Result
					});
				};
				op.FinishOperations += (s, e) => createTcs.TrySetResult(e.Result.Succeeded);

				try
				{
					op.PerformOperations();
				}
				catch
				{
					createTcs.TrySetResult(false);
				}

				if (dataBytes is not null && (shellOperationResult.Items.SingleOrDefault()?.Succeeded ?? false))
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						using var fs = new FileStream(shellOperationResult.Items.Single().Destination, FileMode.Open);
						fs.Write(dataBytes, 0, dataBytes.Length);
						fs.Flush();
					}, App.Logger);
				}

				return (await createTcs.Task, shellOperationResult);
			});
		}

		public static Task<(bool, ShellOperationResult)> TestRecycleAsync(string[] fileToDeletePath)
		{
			return Win32Helper.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations2();

				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmation
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete;

				var shellOperationResult = new ShellOperationResult();
				var tryDelete = false;

				for (var i = 0; i < fileToDeletePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using var shi = new ShellItem(fileToDeletePath[i]);
						using var file = SafetyExtensions.IgnoreExceptions(() => GetFirstFile(shi)) ?? shi;
						if ((uint?)file.Properties.GetValueOrDefault(PKEY_FilePlaceholderStatus) == PS_CLOUDFILE_PLACEHOLDER)
						{
							// Online only files cannot be tried for deletion, so they are treated as to be permanently deleted.
							shellOperationResult.Items.Add(new ShellOperationItemResult()
							{
								Succeeded = false,
								Source = fileToDeletePath[i],
								HResult = HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
							});
						}
						else
						{
							op.QueueDeleteOperation(file);
							tryDelete = true;
						}
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToDeletePath[i],
							HResult = -1
						});
					}
				}

				if (!tryDelete)
					return (true, shellOperationResult);

				var deleteTcs = new TaskCompletionSource<bool>();
				op.PreDeleteItem += [DebuggerHidden] (s, e) =>
				{
					if (!e.Flags.HasFlag(ShellFileOperations.TransferFlags.DeleteRecycleIfPossible))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = e.SourceItem.GetParsingPath(),
							HResult = HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
						});
						throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
					}
					else
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = true,
							Source = e.SourceItem.GetParsingPath(),
							HResult = HRESULT.COPYENGINE_E_USER_CANCELLED
						});
						throw new Win32Exception(HRESULT.COPYENGINE_E_USER_CANCELLED); // E_FAIL, stops operation
					}
				};
				op.FinishOperations += (s, e) => deleteTcs.TrySetResult(e.Result.Succeeded);

				try
				{
					op.PerformOperations();
				}
				catch
				{
					deleteTcs.TrySetResult(false);
				}

				return (await deleteTcs.Task, shellOperationResult);
			});
		}

		public static Task<(bool, ShellOperationResult)> DeleteItemAsync(string[] fileToDeletePath, bool permanently, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			var cts = new CancellationTokenSource();
			var sizeCalculator = new FileSizeCalculator(fileToDeletePath);
			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			sizeTask.ContinueWith(_ =>
			{
				fsProgress.TotalSize = 0;
				fsProgress.ItemsCount = sizeCalculator.ItemsCount;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			});

			fsProgress.Report();
			progressHandler ??= new();

			return Win32Helper.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations2();

				op.Options =
					ShellFileOperations.OperationFlags.Silent |
					ShellFileOperations.OperationFlags.NoConfirmation |
					ShellFileOperations.OperationFlags.NoErrorUI;

				if (asAdmin)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.ShowElevationPrompt |
						ShellFileOperations.OperationFlags.RequireElevation;
				}

				op.OwnerWindow = (IntPtr)ownerHwnd;

				if (!permanently)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.RecycleOnDelete |
						ShellFileOperations.OperationFlags.WantNukeWarning;
				}

				var shellOperationResult = new ShellOperationResult();

				for (var i = 0; i < fileToDeletePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using var shi = new ShellItem(fileToDeletePath[i]);

						op.QueueDeleteOperation(shi);
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToDeletePath[i],
							HResult = -1
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var deleteTcs = new TaskCompletionSource<bool>();

				// Right before deleting item
				op.PreDeleteItem += (s, e) =>
				{
					// E_FAIL, stops operation
					if (!permanently && !e.Flags.HasFlag(ShellFileOperations.TransferFlags.DeleteRecycleIfPossible))
						throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND);

					sizeCalculator.ForceComputeFileSize(e.SourceItem.GetParsingPath());
					fsProgress.FileName = e.SourceItem.Name;
					fsProgress.Report();
				};

				// Right after deleted item
				op.PostDeleteItem += (s, e) =>
				{
					if (!e.SourceItem.IsFolder)
					{
						if (sizeCalculator.TryGetComputedFileSize(e.SourceItem.GetParsingPath(), out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestItem.GetParsingPath(),
						HResult = (int)e.Result
					});

					UpdateFileTagsDb(e, "delete");
				};

				op.FinishOperations += (s, e)
					=> deleteTcs.TrySetResult(e.Result.Succeeded);

				op.UpdateProgress += (s, e) =>
				{
					// E_FAIL, stops operation
					if (progressHandler.CheckCanceled(operationID))
						throw new Win32Exception(unchecked((int)0x80004005));

					fsProgress.Report(e.ProgressPercentage);
					progressHandler.UpdateOperation(operationID, e.ProgressPercentage);
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					deleteTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				cts.Cancel();

				return (await deleteTcs.Task, shellOperationResult);
			});
		}

		public static Task<(bool, ShellOperationResult)> RenameItemAsync(string fileToRenamePath, string newName, bool overwriteOnRename, long ownerHwnd, bool asAdmin, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			progressHandler ??= new();

			return Win32Helper.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations2();
				var shellOperationResult = new ShellOperationResult();

				op.Options = ShellFileOperations.OperationFlags.Silent
						  | ShellFileOperations.OperationFlags.NoErrorUI;
				if (asAdmin)
				{
					op.Options |= ShellFileOperations.OperationFlags.ShowElevationPrompt
							| ShellFileOperations.OperationFlags.RequireElevation;
				}
				op.OwnerWindow = (IntPtr)ownerHwnd;
				op.Options |= !overwriteOnRename ? ShellFileOperations.OperationFlags.RenameOnCollision : 0;

				if (!SafetyExtensions.IgnoreExceptions(() =>
				{
					using var shi = new ShellItem(fileToRenamePath);
					op.QueueRenameOperation(shi, newName);
				}))
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = false,
						Source = fileToRenamePath,
						HResult = -1
					});
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var renameTcs = new TaskCompletionSource<bool>();
				op.PostRenameItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = !string.IsNullOrEmpty(e.Name) ? Path.Combine(Path.GetDirectoryName(e.SourceItem.GetParsingPath()), e.Name) : null,
						HResult = (int)e.Result
					});
				};
				op.PostRenameItem += (_, e) => UpdateFileTagsDb(e, "rename");
				op.FinishOperations += (s, e) => renameTcs.TrySetResult(e.Result.Succeeded);

				try
				{
					op.PerformOperations();
				}
				catch
				{
					renameTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				return (await renameTcs.Task, shellOperationResult);
			});
		}

		public static Task<(bool, ShellOperationResult)> MoveItemAsync(string[] fileToMovePath, string[] moveDestination, bool overwriteOnMove, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			var cts = new CancellationTokenSource();
			var sizeCalculator = new FileSizeCalculator(fileToMovePath);
			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			sizeTask.ContinueWith(_ =>
			{
				fsProgress.TotalSize = sizeCalculator.Size;
				fsProgress.ItemsCount = sizeCalculator.ItemsCount;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			});

			fsProgress.Report();
			progressHandler ??= new();

			return Win32Helper.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations2();
				var shellOperationResult = new ShellOperationResult();

				op.Options =
					ShellFileOperations.OperationFlags.NoConfirmMkDir |
					ShellFileOperations.OperationFlags.Silent |
					ShellFileOperations.OperationFlags.NoErrorUI;

				if (asAdmin)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.ShowElevationPrompt |
						ShellFileOperations.OperationFlags.RequireElevation;
				}

				op.OwnerWindow = (IntPtr)ownerHwnd;

				op.Options |=
					!overwriteOnMove
						? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
						: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToMovePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new(fileToMovePath[i]);
						using ShellFolder shd = new(Path.GetDirectoryName(moveDestination[i]));

						op.QueueMoveOperation(shi, shd, Path.GetFileName(moveDestination[i]));
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToMovePath[i],
							Destination = moveDestination[i],
							HResult = -1
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var moveTcs = new TaskCompletionSource<bool>();

				op.PreMoveItem += (s, e) =>
				{
					sizeCalculator.ForceComputeFileSize(e.SourceItem.GetParsingPath());
					fsProgress.FileName = e.SourceItem.Name;
					fsProgress.Report();
				};

				op.PostMoveItem += (s, e) =>
				{
					if (!e.SourceItem.IsFolder)
					{
						if (sizeCalculator.TryGetComputedFileSize(e.SourceItem.GetParsingPath(), out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestFolder.GetParsingPath() is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
						HResult = (int)e.Result
					});

					UpdateFileTagsDb(e, "move");
				};

				op.FinishOperations += (s, e)
					=> moveTcs.TrySetResult(e.Result.Succeeded);

				op.UpdateProgress += (s, e) =>
				{
					// E_FAIL, stops operation
					if (progressHandler.CheckCanceled(operationID))
						throw new Win32Exception(unchecked((int)0x80004005));

					fsProgress.Report(e.ProgressPercentage);
					progressHandler.UpdateOperation(operationID, e.ProgressPercentage);
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					moveTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				cts.Cancel();

				return (await moveTcs.Task, shellOperationResult);
			});
		}

		public static Task<(bool, ShellOperationResult)> CopyItemAsync(string[] fileToCopyPath, string[] copyDestination, bool overwriteOnCopy, long ownerHwnd, bool asAdmin, IProgress<StatusCenterItemProgressModel> progress, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				false,
				FileSystemStatusCode.InProgress);

			var cts = new CancellationTokenSource();
			var sizeCalculator = new FileSizeCalculator(fileToCopyPath);
			var sizeTask = sizeCalculator.ComputeSizeAsync(cts.Token);
			sizeTask.ContinueWith(_ =>
			{
				fsProgress.TotalSize = sizeCalculator.Size;
				fsProgress.ItemsCount = sizeCalculator.ItemsCount;
				fsProgress.EnumerationCompleted = true;
				fsProgress.Report();
			});

			fsProgress.Report();
			progressHandler ??= new();

			return Win32Helper.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations2();

				var shellOperationResult = new ShellOperationResult();

				op.Options =
					ShellFileOperations.OperationFlags.NoConfirmMkDir |
					ShellFileOperations.OperationFlags.Silent |
					ShellFileOperations.OperationFlags.NoErrorUI;

				if (asAdmin)
				{
					op.Options |=
						ShellFileOperations.OperationFlags.ShowElevationPrompt |
						ShellFileOperations.OperationFlags.RequireElevation;
				}

				op.OwnerWindow = (IntPtr)ownerHwnd;

				op.Options |=
					!overwriteOnCopy
						? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
						: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToCopyPath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new(fileToCopyPath[i]);
						using ShellFolder shd = new(Path.GetDirectoryName(copyDestination[i]));

						var fileName = GetIncrementalName(overwriteOnCopy, copyDestination[i], fileToCopyPath[i]);
						// Perform a copy operation
						op.QueueCopyOperation(shi, shd, fileName);
					}))
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = false,
							Source = fileToCopyPath[i],
							Destination = copyDestination[i],
							HResult = -1
						});
					}
				}

				progressHandler.OwnerWindow = op.OwnerWindow;
				progressHandler.AddOperation(operationID);

				var copyTcs = new TaskCompletionSource<bool>();

				op.PreCopyItem += (s, e) =>
				{
					sizeCalculator.ForceComputeFileSize(e.SourceItem.GetParsingPath());
					fsProgress.FileName = e.SourceItem.Name;
					fsProgress.Report();
				};

				op.PostCopyItem += (s, e) =>
				{
					if (!e.SourceItem.IsFolder)
					{
						if (sizeCalculator.TryGetComputedFileSize(e.SourceItem.GetParsingPath(), out _))
							fsProgress.AddProcessedItemsCount(1);
					}

					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = e.SourceItem.GetParsingPath(),
						Destination = e.DestFolder.GetParsingPath() is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(e.DestFolder.GetParsingPath(), e.Name) : null,
						HResult = (int)e.Result
					});

					UpdateFileTagsDb(e, "copy");
				};

				op.FinishOperations += (s, e)
					=> copyTcs.TrySetResult(e.Result.Succeeded);

				op.UpdateProgress += (s, e) =>
				{
					// E_FAIL, stops operation
					if (progressHandler.CheckCanceled(operationID))
						throw new Win32Exception(unchecked((int)0x80004005));

					fsProgress.Report(e.ProgressPercentage);
					progressHandler.UpdateOperation(operationID, e.ProgressPercentage);
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					copyTcs.TrySetResult(false);
				}

				progressHandler.RemoveOperation(operationID);

				cts.Cancel();

				return (await copyTcs.Task, shellOperationResult);
			});
		}

		public static void TryCancelOperation(string operationId)
			=> progressHandler?.TryCancel(operationId);

		public static IEnumerable<Win32Process>? CheckFileInUse(string[] fileToCheckPath)
		{
			var processes = SafetyExtensions.IgnoreExceptions(() => Win32Helper.WhoIsLocking(fileToCheckPath), App.Logger);

			if (processes is not null)
			{
				var win32proc = processes.Select(x => new Win32Process()
				{
					Name = x.ProcessName,
					Pid = x.Id,
					FileName = SafetyExtensions.IgnoreExceptions(() => x.MainModule?.FileName),
					AppName = SafetyExtensions.IgnoreExceptions(() => x.MainModule?.FileVersionInfo?.FileDescription)
				}).ToList();
				processes.ForEach(x => x.Dispose());

				return win32proc;
			}
			else
			{
				return null;
			}
		}

		public static async Task<ShellLinkItem?> ParseLinkAsync(string linkPath)
		{
			if (string.IsNullOrEmpty(linkPath))
				return null;

			string targetPath = string.Empty;

			try
			{
				if (FileExtensionHelpers.IsShortcutFile(linkPath))
				{
					using var link = new ShellLink(linkPath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));
					targetPath = link.TargetPath;
					return ShellFolderExtensions.GetShellLinkItem(link);
				}
				else if (FileExtensionHelpers.IsWebLinkFile(linkPath))
				{
					targetPath = await Win32Helper.StartSTATask(() =>
					{
						var ipf = new Url.IUniformResourceLocator();
						(ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Load(linkPath, 0);
						ipf.GetUrl(out var retVal);
						return retVal;
					});
					return string.IsNullOrEmpty(targetPath) ?
						new ShellLinkItem
						{
							TargetPath = string.Empty,
							InvalidTarget = true
						} : new ShellLinkItem { TargetPath = targetPath };
				}
				return null;
			}
			catch (FileNotFoundException ex) // Could not parse shortcut
			{
				App.Logger?.LogWarning(ex, ex.Message);
				// Return a item containing the invalid target path
				return new ShellLinkItem
				{
					TargetPath = string.IsNullOrEmpty(targetPath) ? string.Empty : targetPath,
					InvalidTarget = true
				};
			}
			catch (Exception ex)
			{
				// Could not parse shortcut
				App.Logger.LogWarning(ex, ex.Message);
				return null;
			}
		}

		public static Task<bool> CreateOrUpdateLinkAsync(string linkSavePath, string targetPath, string arguments = "", string workingDirectory = "", bool runAsAdmin = false, SHOW_WINDOW_CMD showWindowCommand = SHOW_WINDOW_CMD.SW_NORMAL)
		{
			try
			{
				if (FileExtensionHelpers.IsShortcutFile(linkSavePath))
				{
					using var newLink = new ShellLink(targetPath, arguments, workingDirectory);

					// Check if the target is a file
					if (File.Exists(targetPath))
						newLink.RunAsAdministrator = runAsAdmin;

					newLink.SaveAs(linkSavePath); // Overwrite if exists

					// ShowState has to be set after SaveAs has been called, otherwise an UnauthorizedAccessException gets thrown in some cases
					newLink.ShowState = (ShowWindowCommand)showWindowCommand;

					return Task.FromResult(true);
				}
				else if (FileExtensionHelpers.IsWebLinkFile(linkSavePath))
				{
					return Win32Helper.StartSTATask(() =>
					{
						var ipf = new Url.IUniformResourceLocator();
						ipf.SetUrl(targetPath, Url.IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL);
						(ipf as System.Runtime.InteropServices.ComTypes.IPersistFile).Save(linkSavePath, false); // Overwrite if exists
						return true;
					});
				}
			}
			catch (UnauthorizedAccessException ex)
			{
				// Could not create shortcut
				App.Logger.LogInformation(ex, "Failed to create shortcut");
			}
			catch (Exception ex)
			{
				// Could not create shortcut
				App.Logger.LogWarning(ex, ex.Message);
			}

			return Task.FromResult(false);
		}

		public static bool SetLinkIcon(string filePath, string iconFile, int iconIndex)
		{
			try
			{
				using var link = new ShellLink(filePath, LinkResolution.NoUIWithMsgPump, default, TimeSpan.FromMilliseconds(100));
				link.IconLocation = new IconLocation(iconFile, iconIndex);
				link.SaveAs(filePath); // Overwrite if exists
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				string psScript = $@"
					$FilePath = '{filePath}'
					$IconFile = '{iconFile}'
					$IconIndex = '{iconIndex}'

					$Shell = New-Object -ComObject WScript.Shell
					$Shortcut = $Shell.CreateShortcut($FilePath)
					$Shortcut.IconLocation = ""$IconFile, $IconIndex""
					$Shortcut.Save()
				";

				var base64EncodedScript = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(psScript));

				ProcessStartInfo startInfo = new ProcessStartInfo()
				{
					FileName = "powershell.exe",
					Arguments = $"-NoProfile -EncodedCommand {base64EncodedScript}",
					Verb = "runas",
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					UseShellExecute = true
				};

				// Start the process
				Process process = new Process() { StartInfo = startInfo };
				process.Start();
				process.WaitForExit();

				return true;
			}
			catch (Exception ex)
			{
				// Could not create shortcut
				App.Logger.LogWarning(ex, ex.Message);
			}

			return false;
		}

		public static Task<string?> OpenObjectPickerAsync(long hWnd)
		{
			return Win32Helper.StartSTATask(() =>
			{
				var picker = new DirectoryObjectPickerDialog()
				{
					AllowedObjectTypes = ObjectTypes.All,
					DefaultObjectTypes = ObjectTypes.Users | ObjectTypes.Groups,
					AllowedLocations = Locations.All,
					DefaultLocations = Locations.LocalComputer,
					MultiSelect = false,
					ShowAdvancedView = true
				};

				picker.AttributesToFetch.Add("objectSid");

				using (picker)
				{
					if (picker.ShowDialog(Win32Helper.Win32Window.FromLong(hWnd)) == System.Windows.Forms.DialogResult.OK)
					{
						try
						{
							var attribs = picker.SelectedObject.FetchedAttributes;
							if (attribs.Any() && attribs[0] is byte[] objectSid)
								return new SecurityIdentifier(objectSid, 0).Value;
						}
						catch { }
					}
				}

				return null;
			});
		}

		private static ShellItem? GetFirstFile(ShellItem shi)
		{
			if (!shi.IsFolder || shi.Attributes.HasFlag(ShellItemAttribute.Stream))
			{
				return shi;
			}
			using var shf = new ShellFolder(shi);
			if (shf.FirstOrDefault(x => !x.IsFolder || x.Attributes.HasFlag(ShellItemAttribute.Stream)) is ShellItem item)
			{
				return item;
			}
			foreach (var shsfi in shf.Where(x => x.IsFolder && !x.Attributes.HasFlag(ShellItemAttribute.Stream)))
			{
				using var shsf = new ShellFolder(shsfi);
				if (GetFirstFile(shsf) is ShellItem item2)
				{
					return item2;
				}
			}
			return null;
		}

		private static void UpdateFileTagsDb(ShellFileOperations2.ShellFileOpEventArgs e, string operationType)
		{
			var dbInstance = FileTagsHelper.GetDbInstance();
			if (e.Result.Succeeded)
			{
				var sourcePath = e.SourceItem.GetParsingPath();
				var destPath = e.DestFolder.GetParsingPath();
				var destination = operationType switch
				{
					"delete" => e.DestItem.GetParsingPath(),
					"rename" => !string.IsNullOrEmpty(e.Name) ? Path.Combine(Path.GetDirectoryName(sourcePath), e.Name) : null,
					"copy" => destPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destPath, e.Name) : null,
					_ => destPath is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(destPath, e.Name) : null
				};
				if (destination is null)
				{
					dbInstance.SetTags(sourcePath, null, []); // remove tag from deleted files
				}
				else
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						if (operationType == "copy")
						{
							var tag = dbInstance.GetTags(sourcePath, null);

							dbInstance.SetTags(destination, FileTagsHelper.GetFileFRN(destination), tag); // copy tag to new files
							using var si = new ShellItem(destination);
							if (si.IsFolder) // File tag is not copied automatically for folders
							{
								FileTagsHelper.WriteFileTag(destination, tag);
							}
						}
						else
						{
							dbInstance.UpdateTag(sourcePath, FileTagsHelper.GetFileFRN(destination), destination); // move tag to new files
						}
					}, App.Logger);
				}
				if (e.Result == HRESULT.COPYENGINE_S_DONT_PROCESS_CHILDREN) // child items not processed, update manually
				{
					var tags = dbInstance.GetAllUnderPath(sourcePath).ToList();
					if (destination is null) // remove tag for items contained in the folder
					{
						tags.ForEach(t => dbInstance.SetTags(t.FilePath, null, []));
					}
					else
					{
						if (operationType == "copy") // copy tag for items contained in the folder
						{
							tags.ForEach(t =>
							{
								SafetyExtensions.IgnoreExceptions(() =>
								{
									var subPath = t.FilePath.Replace(sourcePath, destination, StringComparison.Ordinal);
									dbInstance.SetTags(subPath, FileTagsHelper.GetFileFRN(subPath), t.Tags ?? []);
								}, App.Logger);
							});
						}
						else // move tag to new files
						{
							tags.ForEach(t =>
							{
								SafetyExtensions.IgnoreExceptions(() =>
								{
									var subPath = t.FilePath.Replace(sourcePath, destination, StringComparison.Ordinal);
									dbInstance.UpdateTag(t.FilePath, FileTagsHelper.GetFileFRN(subPath), subPath);
								}, App.Logger);
							});
						}
					}
				}
			}
		}

		public static void WaitForCompletion()
			=> progressHandler?.WaitForCompletion();

		private sealed class ProgressHandler : Disposable
		{
			private readonly ManualResetEvent operationsCompletedEvent;

			public sealed class OperationWithProgress
			{
				public double Progress { get; set; }
				public bool Canceled { get; set; }
			}

			private readonly Shell32.ITaskbarList4? taskbar;
			private readonly ConcurrentDictionary<string, OperationWithProgress> operations;

			public HWND OwnerWindow { get; set; }

			public ProgressHandler()
			{
				taskbar = Win32Helper.CreateTaskbarObject();
				operations = new ConcurrentDictionary<string, OperationWithProgress>();
				operationsCompletedEvent = new ManualResetEvent(true);
			}

			public int Progress
			{
				get
				{
					var ongoing = operations.ToArray().Where(x => !x.Value.Canceled);
					return ongoing.Any() ? (int)ongoing.Average(x => x.Value.Progress) : 0;
				}
			}

			public void AddOperation(string uid)
			{
				operations.TryAdd(uid, new OperationWithProgress());
				UpdateTaskbarProgress();
				operationsCompletedEvent.Reset();
			}

			public void RemoveOperation(string uid)
			{
				operations.TryRemove(uid, out _);
				UpdateTaskbarProgress();
				if (!operations.Any())
				{
					operationsCompletedEvent.Set();
				}
			}

			public void UpdateOperation(string uid, double progress)
			{
				if (operations.TryGetValue(uid, out var op))
				{
					op.Progress = progress;
					UpdateTaskbarProgress();
				}
			}

			public bool CheckCanceled(string uid)
			{
				return !operations.TryGetValue(uid, out var op) || op.Canceled;
			}

			public void TryCancel(string uid)
			{
				if (operations.TryGetValue(uid, out var op))
				{
					op.Canceled = true;
					UpdateTaskbarProgress();
				}
			}

			private void UpdateTaskbarProgress()
			{
				if (OwnerWindow == HWND.NULL || taskbar is null)
				{
					return;
				}
				if (operations.Any())
				{
					taskbar.SetProgressValue(OwnerWindow, (ulong)Progress, 100);
				}
				else
				{
					taskbar.SetProgressState(OwnerWindow, Shell32.TBPFLAG.TBPF_NOPROGRESS);
				}
			}

			public void WaitForCompletion()
			{
				operationsCompletedEvent.WaitOne();
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					operationsCompletedEvent?.Dispose();
					if (taskbar is not null)
						Marshal.ReleaseComObject(taskbar);
				}
			}
		}

		private static string GetIncrementalName(bool overWriteOnCopy, string? filePathToCheck, string? filePathToCopy)
		{
			if (filePathToCheck == null)
				return null;

			if ((!Path.Exists(filePathToCheck)) || overWriteOnCopy || filePathToCheck == filePathToCopy)
				return Path.GetFileName(filePathToCheck);

			var index = 2;
			var filePath = filePathToCheck;
			if (Path.HasExtension(filePathToCheck))
				filePath = filePathToCheck.Substring(0, filePathToCheck.LastIndexOf("."));

			Func<int, string> genFilePath = x => string.Concat([filePath, " (", x.ToString(), ")", Path.GetExtension(filePathToCheck)]);

			while (Path.Exists(genFilePath(index)))
				index++;

			return Path.GetFileName(genFilePath(index));
		}
	}
}
