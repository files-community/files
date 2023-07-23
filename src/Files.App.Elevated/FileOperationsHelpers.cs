// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Items;
using Files.Shared.Extensions;
using System.ComponentModel;
using System.Diagnostics;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Elevated
{
	public class FileOperationsHelpers
	{
		private static readonly Ole32.PROPERTYKEY PKEY_FilePlaceholderStatus = new Ole32.PROPERTYKEY(new Guid("B2F9B9D6-FEC4-4DD5-94D7-8957488C807B"), 2);
		private const uint PS_CLOUDFILE_PLACEHOLDER = 8;

		public static async Task<(bool, ShellOperationResult)> CreateItemAsync(string filePath, string fileOp, string template = "", byte[]? dataBytes = null)
		{
			//return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();

				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.RenameOnCollision
							| ShellFileOperations.OperationFlags.NoErrorUI;

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
						Destination = GetParsingPath(e.DestItem),
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
					}, Program.Logger);
				}

				return (await createTcs.Task, shellOperationResult);
			}//);
		}

		public static async Task<(bool, ShellOperationResult)> TestRecycleAsync(string[] fileToDeletePath)
		{
			//return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();

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
						if (file.Properties.GetProperty<uint>(PKEY_FilePlaceholderStatus) == PS_CLOUDFILE_PLACEHOLDER)
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
							Source = GetParsingPath(e.SourceItem),
							HResult = HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND
						});
						throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
					}
					else
					{
						shellOperationResult.Items.Add(new ShellOperationItemResult()
						{
							Succeeded = true,
							Source = GetParsingPath(e.SourceItem),
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
			}//);
		}

		public static async Task<(bool, ShellOperationResult)> DeleteItemAsync(string[] fileToDeletePath, bool permanently, long ownerHwnd, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			//return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();
				op.Options = ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoConfirmation
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.OwnerWindow = (IntPtr)ownerHwnd;
				if (!permanently)
				{
					op.Options |= ShellFileOperations.OperationFlags.RecycleOnDelete
								| ShellFileOperations.OperationFlags.WantNukeWarning;
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

				var deleteTcs = new TaskCompletionSource<bool>();
				op.PreDeleteItem += (s, e) =>
				{
					if (!permanently && !e.Flags.HasFlag(ShellFileOperations.TransferFlags.DeleteRecycleIfPossible))
					{
						throw new Win32Exception(HRESULT.COPYENGINE_E_RECYCLE_BIN_NOT_FOUND); // E_FAIL, stops operation
					}
				};
				op.PostDeleteItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = GetParsingPath(e.SourceItem),
						Destination = GetParsingPath(e.DestItem),
						HResult = (int)e.Result
					});
				};
				op.FinishOperations += (s, e) => deleteTcs.TrySetResult(e.Result.Succeeded);
				op.UpdateProgress += (s, e) =>
				{
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					deleteTcs.TrySetResult(false);
				}

				return (await deleteTcs.Task, shellOperationResult);
			}//);
		}

		public static async Task<(bool, ShellOperationResult)> RenameItemAsync(string fileToRenamePath, string newName, bool overwriteOnRename, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			//return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();
				var shellOperationResult = new ShellOperationResult();

				op.Options = ShellFileOperations.OperationFlags.Silent
						  | ShellFileOperations.OperationFlags.NoErrorUI;
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

				var renameTcs = new TaskCompletionSource<bool>();
				op.PostRenameItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = GetParsingPath(e.SourceItem),
						Destination = !string.IsNullOrEmpty(e.Name) ? Path.Combine(Path.GetDirectoryName(GetParsingPath(e.SourceItem)), e.Name) : null,
						HResult = (int)e.Result
					});
				};
				op.FinishOperations += (s, e) => renameTcs.TrySetResult(e.Result.Succeeded);

				try
				{
					op.PerformOperations();
				}
				catch
				{
					renameTcs.TrySetResult(false);
				}

				return (await renameTcs.Task, shellOperationResult);
			}//);
		}

		public static async Task<(bool, ShellOperationResult)> MoveItemAsync(string[] fileToMovePath, string[] moveDestination, bool overwriteOnMove, long ownerHwnd, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			//return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();
				var shellOperationResult = new ShellOperationResult();

				op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.OwnerWindow = (IntPtr)ownerHwnd;
				op.Options |= !overwriteOnMove ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
					: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToMovePath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new ShellItem(fileToMovePath[i]);
						using ShellFolder shd = new ShellFolder(Path.GetDirectoryName(moveDestination[i]));
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

				var moveTcs = new TaskCompletionSource<bool>();
				op.PostMoveItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = GetParsingPath(e.SourceItem),
						Destination = GetParsingPath(e.DestFolder) is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(GetParsingPath(e.DestFolder), e.Name) : null,
						HResult = (int)e.Result
					});
				};
				op.FinishOperations += (s, e) => moveTcs.TrySetResult(e.Result.Succeeded);
				op.UpdateProgress += (s, e) =>
				{
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					moveTcs.TrySetResult(false);
				}

				return (await moveTcs.Task, shellOperationResult);
			}//);
		}

		public static async Task<(bool, ShellOperationResult)> CopyItemAsync(string[] fileToCopyPath, string[] copyDestination, bool overwriteOnCopy, long ownerHwnd, string operationID = "")
		{
			operationID = string.IsNullOrEmpty(operationID) ? Guid.NewGuid().ToString() : operationID;

			//return Win32API.StartSTATask(async () =>
			{
				using var op = new ShellFileOperations();

				var shellOperationResult = new ShellOperationResult();

				op.Options = ShellFileOperations.OperationFlags.NoConfirmMkDir
							| ShellFileOperations.OperationFlags.Silent
							| ShellFileOperations.OperationFlags.NoErrorUI;
				op.OwnerWindow = (IntPtr)ownerHwnd;
				op.Options |= !overwriteOnCopy ? ShellFileOperations.OperationFlags.PreserveFileExtensions | ShellFileOperations.OperationFlags.RenameOnCollision
					: ShellFileOperations.OperationFlags.NoConfirmation;

				for (var i = 0; i < fileToCopyPath.Length; i++)
				{
					if (!SafetyExtensions.IgnoreExceptions(() =>
					{
						using ShellItem shi = new ShellItem(fileToCopyPath[i]);
						using ShellFolder shd = new ShellFolder(Path.GetDirectoryName(copyDestination[i]));
						op.QueueCopyOperation(shi, shd, Path.GetFileName(copyDestination[i]));
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

				var copyTcs = new TaskCompletionSource<bool>();
				op.PostCopyItem += (s, e) =>
				{
					shellOperationResult.Items.Add(new ShellOperationItemResult()
					{
						Succeeded = e.Result.Succeeded,
						Source = GetParsingPath(e.SourceItem),
						Destination = GetParsingPath(e.DestFolder) is not null && !string.IsNullOrEmpty(e.Name) ? Path.Combine(GetParsingPath(e.DestFolder), e.Name) : null,
						HResult = (int)e.Result
					});
				};
				op.FinishOperations += (s, e) => copyTcs.TrySetResult(e.Result.Succeeded);
				op.UpdateProgress += (s, e) =>
				{
				};

				try
				{
					op.PerformOperations();
				}
				catch
				{
					copyTcs.TrySetResult(false);
				}

				return (await copyTcs.Task, shellOperationResult);
			}//);
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

		private static string GetParsingPath(ShellItem item)
		{
			if (item is null)
				return null;

			return item.IsFileSystem ? item.FileSystemPath : item.ParsingName;
		}
	}
}
