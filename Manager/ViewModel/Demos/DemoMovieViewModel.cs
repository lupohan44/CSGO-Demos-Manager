﻿#region Using
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Core;
using Core.Models;
using Core.Models.Source;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls.Dialogs;
using Manager.Models.Movie;
using Manager.Properties;
using Manager.Services;
using Manager.ViewModel.Shared;
using Manager.Views.Demos;
using Services.Interfaces;
using Services.Models.Timelines;
using Telerik.Windows.Controls;
#endregion

namespace Manager.ViewModel.Demos
{
	public class DemoMovieViewModel : SingleDemoViewModel
	{
		private readonly IDialogService _dialogService;
		private readonly IDemosService _demoService;
		private readonly ICacheService _cacheService;
		private Player _focusedPlayer;
		private RelayCommand _windowLoaded;
		private RelayCommand _backToDemoDetails;
		private RelayCommand _generateMovie;
		private RelayCommand _cancel;
		private RelayCommand _selectRawFilesFolderDestination;
		private RelayCommand _selectOutputFileDestinationFolder;
		private RelayCommand _navigateToRawFilesFolder;
		private RelayCommand _navigateToOutputFileFolderDestination;
		private RelayCommand _installFFmpeg;
		private RelayCommand _navigateToFFmpegFolder;
		private RelayCommand _installHLAE;
		private RelayCommand _navigateToHLAEFolder;
		private RelayCommand _updateHLAE;
		private RelayCommand _installVirtualDub;
		private RelayCommand _navigateToVirtualDubFolder;
		private RelayCommand _selectCsgoExePathLocation;
		private RelayCommand<int> _updateStartTick;
		private RelayCommand<int> _updateEndTick;
		private RelayCommand<int> _updateStartTickBeforeSeconds;
		private RelayCommand<int> _updateStartTickAfterSeconds;
		private RelayCommand<int> _updateEndTickBeforeSeconds;
		private RelayCommand<int> _updateEndTickAfterSeconds;
		private RelayCommand<string> _updateFocusPlayer;
		private RelayCommand _saveCommentDemo;
		private RelayCommand _resetCfg;
		private RelayCommand _selectHlaeConfigParentPath;
		private RelayCommand _toggleDemoCommentVisibility;
		private RelayCommand<MouseButtonEventArgs> _timelineRightClick;
		private List<TimelineEvent> _timelineEventList = new List<TimelineEvent>();
		private MovieService _movieService;
		private MovieConfiguration _movieConfig = new MovieConfiguration();
		private string _csgoExePath;
		private DateTime _visiblePeriodStart;
		private DateTime _visiblePeriodEnd;
		private DateTime _periodStart;
		private DateTime _periodEnd;
		private bool _isInstalling;
		private bool _focusOnPlayer;
		private bool _showDemoComment = Settings.Default.MovieShowDemoComment;
		/// <summary>
		/// Manual player's SteamID for POV demo.
		/// </summary>
		private long _playerFocusSteamId;
		private bool _isTimelineAvailable;
		private int _selectedTick;
		/// <summary>
		/// Ignore corrupted and POV demos
		/// </summary>
		private bool _isIncompatibleDemo;
		/// <summary>
		/// Flag to detect corrupted demos because we can't determine seconds duration and required space with it since we don't have the tickrate.
		/// </summary>
		private bool _isCorruptedDemo;
		/// <summary>
		/// Flag to adapt the UI with POV demos.
		/// </summary>
		private bool _isPovDemo;

		public bool IsInstalling
		{
			get => _isInstalling;
			set { Set(() => IsInstalling, ref _isInstalling, value); }
		}

		public DateTime PeriodStart
		{
			get => _periodStart;
			set { Set(() => PeriodStart, ref _periodStart, value); }
		}

		public DateTime PeriodEnd
		{
			get => _periodEnd;
			set { Set(() => PeriodEnd, ref _periodEnd, value); }
		}

		public ObservableCollection<int> AudioBitrateValues { get; }

		public long PlayerFocusSteamID
		{
			get => _playerFocusSteamId;
			set
			{
				_movieConfig.FocusSteamId = Convert.ToInt64(value);
				Set(() => PlayerFocusSteamID, ref _playerFocusSteamId, value);
			}
		}

		public int SelectedTick
		{
			get => _selectedTick;
			set { Set(() => SelectedTick, ref _selectedTick, value); }
		}

		public bool IsTimelineAvailable
		{
			get => _isTimelineAvailable;
			set { Set(() => IsTimelineAvailable, ref _isTimelineAvailable, value); }
		}

		public bool IsPovDemo
		{
			get => _isPovDemo;
			set { Set(() => IsPovDemo, ref _isPovDemo, value); }
		}

		public bool IsCorruptedDemo
		{
			get => _isCorruptedDemo;
			set { Set(() => IsCorruptedDemo, ref _isCorruptedDemo, value); }
		}

		public bool IsIncompatibleDemo
		{
			get => _isIncompatibleDemo;
			set { Set(() => IsIncompatibleDemo, ref _isIncompatibleDemo, value); }
		}

		public bool ShowDemoComment
		{
			get => _showDemoComment;
			set
			{
				Settings.Default.MovieShowDemoComment = value;
				Set(() => ShowDemoComment, ref _showDemoComment, value);
			}
		}

		public bool EnableHlaeConfigParentFolder
		{
			get => _movieConfig.EnableHlaeConfigParent;
			set
			{
				_movieConfig.EnableHlaeConfigParent = value;
				Settings.Default.MovieEnableHlaeConfigParent = value;
				RaisePropertyChanged(() => EnableHlaeConfigParentFolder);
			}
		}

		public string HlaeConfigParentFolderPath
		{
			get => _movieConfig.HlaeConfigParentFolderPath;
			set
			{
				_movieConfig.HlaeConfigParentFolderPath = value;
				Settings.Default.MovieHlaeConfigParentFolderPath = value;
				RaisePropertyChanged(() => HlaeConfigParentFolderPath);
			}
		}

		public bool FocusOnPlayer
		{
			get => _focusOnPlayer;
			set
			{
				if (!value) FocusedPlayer = null;
				else if (FocusedPlayer == null && Demo.Players.Count > 0) FocusedPlayer = Demo.Players[0];
				Set(() => FocusOnPlayer, ref _focusOnPlayer, value);
			}
		}

		public Player FocusedPlayer
		{
			get => _focusedPlayer;
			set
			{
				Set(() => FocusedPlayer, ref _focusedPlayer, value);
				_movieConfig.FocusSteamId = value?.SteamId ?? 0;
			}
		}

		public bool UseVirtualDub
		{
			get => _movieConfig.UseVirtualDub;
			set
			{
				_movieConfig.UseVirtualDub = value;
				Settings.Default.MovieUseVirtualDub = value;
				RaisePropertyChanged(() => UseVirtualDub);
			}
		}

		public int Width
		{
			get => _movieConfig.Width;
			set
			{
				_movieConfig.Width = value;
				Settings.Default.MovieWidth = value;
				RaisePropertyChanged(() => Width);
				RaisePropertyChanged(() => RequiredSpace);
			}
		}

		public int Height
		{
			get => _movieConfig.Height;
			set
			{
				_movieConfig.Height = value;
				Settings.Default.MovieHeight = value;
				RaisePropertyChanged(() => Height);
				RaisePropertyChanged(() => RequiredSpace);
			}
		}

		public string Cfg
		{
			get
			{
				if (Settings.Default.MovieShowDefaultCfg)
				{
					List<string> cmds = new List<string>
					{
						"// CFG example with some useful commands",
						"// You should paste your moviemaking CFG here",
					};
					cmds.AddRange(MovieService.DefaultCommands);

					return string.Join(Environment.NewLine, cmds);
				}

				return string.Join(Environment.NewLine, _movieConfig.UserCfg);
			}
			set
			{
				_movieConfig.UserCfg = new List<string> { value };
				Settings.Default.MovieShowDefaultCfg = false;
				Settings.Default.MovieUserCfg = value;
				RaisePropertyChanged(() => Cfg);
			}
		}

		public string OutputFileName
		{
			get => _movieConfig.OutputFilename;
			set
			{
				_movieConfig.OutputFilename = value;
				RaisePropertyChanged(() => OutputFileName);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public string FFmpegInputParameters
		{
			get => _movieConfig.FFmpegInputParameters;
			set
			{
				_movieConfig.FFmpegInputParameters = value;
				RaisePropertyChanged(() => FFmpegInputParameters);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public string FFmpegExtraParameters
		{
			get => _movieConfig.FFmpegExtraParameters;
			set
			{
				_movieConfig.FFmpegExtraParameters = value;
				RaisePropertyChanged(() => FFmpegExtraParameters);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public string RawFilesFolderDestination
		{
			get => _movieConfig.RawFilesDestination;
			set
			{
				_movieConfig.RawFilesDestination = value;
				Settings.Default.RawFilesFolderDestination = value;
				RaisePropertyChanged(() => RawFilesFolderDestination);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public string OuputFileDestinationFolder
		{
			get => _movieConfig.OutputFileDestinationFolder;
			set
			{
				_movieConfig.OutputFileDestinationFolder = value;
				Settings.Default.MovieOutputFileFolderDestination = value;
				RaisePropertyChanged(() => OuputFileDestinationFolder);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public int StartTick
		{
			get => _movieConfig.StartTick;
			set
			{
				_movieConfig.StartTick = value;
				RaisePropertyChanged(() => StartTick);
				RaisePropertyChanged(() => Duration);
				RaisePropertyChanged(() => RequiredSpace);
			}
		}

		public int EndTick
		{
			get => _movieConfig.EndTick;
			set
			{
				_movieConfig.EndTick = value;
				RaisePropertyChanged(() => EndTick);
				RaisePropertyChanged(() => Duration);
				RaisePropertyChanged(() => RequiredSpace);
			}
		}

		public float Duration
		{
			get
			{
				if (Demo != null && Demo.ServerTickrate > 0 && StartTick > 0 && EndTick > 0 && EndTick > StartTick)
				{
					return (EndTick - StartTick) / Demo.ServerTickrate;
				}

				return 0;
			}
			set
			{
				if (StartTick > 0)
					EndTick = (int)(StartTick + Demo.ServerTickrate * value);
			}
		}

		public int FrameRate
		{
			get => _movieConfig.FrameRate;
			set
			{
				Settings.Default.MovieFramerate = value;
				_movieConfig.FrameRate = value;
				RaisePropertyChanged(() => FrameRate);
				RaisePropertyChanged(() => MandatoryCvars);
				RaisePropertyChanged(() => FFmpegCommand);
				RaisePropertyChanged(() => RequiredSpace);
			}
		}

		public float RequiredSpace
		{
			get
			{
				float value = 0;
				if (FrameRate > 0 && StartTick != 0 && EndTick != 0 && EndTick > StartTick)
				{
					int tickCount = EndTick - StartTick;
					float ratio = tickCount / Demo.ServerTickrate;
					float tgaCount = ratio * FrameRate;
					float imageSize = Width * Height * 24f / 8f / 1024f / 1024f; // 24 bit depth
					value = tgaCount * imageSize / 1024f;
					value = (float)Math.Round(value, 2);
				}

				return value;
			}
		}

		public string CsgoExePath
		{
			get => _csgoExePath;
			set
			{
				Settings.Default.MovieCsgoExePath = value;
				Set(() => CsgoExePath, ref _csgoExePath, value);
			}
		}

		public int VideoFrameRate
		{
			get => _movieConfig.FFmpegVideoFramerate;
			set
			{
				Settings.Default.MovieVideoFramerate = value;
				_movieConfig.FFmpegVideoFramerate = value;
				RaisePropertyChanged(() => VideoFrameRate);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public bool GenerateVideoFile
		{
			get => _movieConfig.GenerateVideoFile;
			set
			{
				_movieConfig.GenerateVideoFile = value;
				Settings.Default.MovieGenerateVideoFile = value;
				RaisePropertyChanged(() => GenerateVideoFile);
			}
		}

		public bool AutoCloseGame
		{
			get => _movieConfig.AutoCloseGame;
			set
			{
				_movieConfig.AutoCloseGame = value;
				Settings.Default.MovieAutoCloseGame = value;
				RaisePropertyChanged(() => AutoCloseGame);
			}
		}

		public bool CleanUpRawFiles
		{
			get => _movieConfig.CleanUpRawFiles;
			set
			{
				_movieConfig.CleanUpRawFiles = value;
				Settings.Default.MovieCleanUpRawFiles = value;
				RaisePropertyChanged(() => CleanUpRawFiles);
			}
		}

		public bool OpenInExplorer
		{
			get => _movieConfig.OpenInExplorer;
			set
			{
				_movieConfig.OpenInExplorer = value;
				Settings.Default.MovieOpenInExplorer = value;
				RaisePropertyChanged(() => OpenInExplorer);
			}
		}

		public int VideoQuality
		{
			get => _movieConfig.VideoQuality;
			set
			{
				_movieConfig.VideoQuality = value;
				Settings.Default.MovieVideoQuality = value;
				RaisePropertyChanged(() => VideoQuality);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public int AudioBitrate
		{
			get => _movieConfig.AudioBitrate;
			set
			{
				_movieConfig.AudioBitrate = value;
				Settings.Default.MovieAudioBitrate = value;
				RaisePropertyChanged(() => AudioBitrate);
				RaisePropertyChanged(() => FFmpegCommand);
			}
		}

		public DateTime VisiblePeriodStart
		{
			get => _visiblePeriodStart;
			set { Set(() => VisiblePeriodStart, ref _visiblePeriodStart, value); }
		}

		public DateTime VisiblePeriodEnd
		{
			get => _visiblePeriodEnd;
			set { Set(() => VisiblePeriodEnd, ref _visiblePeriodEnd, value); }
		}

		public List<TimelineEvent> TimelineEventList
		{
			get => _timelineEventList;
			set { Set(() => TimelineEventList, ref _timelineEventList, value); }
		}

		public string FFmpegCommand => _movieService?.GetFFmpegCommandLineAsString();

		public bool IsFFmpegInstalled => FFmpegService.IsFFmpegInstalled();

		public bool IsInstallFFmpegAvailable => FFmpegService.IsUpdateAvailable();

		public string FFmpegVersion => FFmpegService.GetInstalledVersion();

		public string HLAEVersion => HlaeService.GetHlaeVersion();

		public bool IsHLAEInstalled => HlaeService.IsHlaeInstalled();

		public bool IsVirtualDubInstalled => VirtualDubService.IsVirtualDubInstalled();

		public string VirtualDubVersion => VirtualDubService.GetInstalledVersion();

		public bool IsInstallVirtualDubAvailable => VirtualDubService.IsUpdateAvailable();

		public string MandatoryCvars => string.Join(Environment.NewLine, MovieService.MandatoryCommands) + Environment.NewLine + "host_framerate " + FrameRate;

		public bool IsAnalyzeRequired => !IsPovDemo && !IsTimelineAvailable && !IsCorruptedDemo;

		/// <summary>
		/// Command called when the user right clicked on the timeline (not on a timeline item).
		/// </summary>
		public RelayCommand<MouseButtonEventArgs> TimelineRightClick
		{
			get
			{
				return _timelineRightClick
					   ?? (_timelineRightClick = new RelayCommand<MouseButtonEventArgs>(
						   e =>
						   {
							   RadTimeline timeline = (RadTimeline)e.Source;
							   Point point = e.MouseDevice.GetPosition(timeline);
							   DateTime dateTime = timeline.ConvertPointToDateTime(point);
							   SelectedTick = (int)(dateTime.Subtract(DateTime.Today).TotalSeconds * Demo.ServerTickrate);
						   },
						   demo => Demo != null));
			}
		}

		/// <summary>
		/// Command to toggle demo's comment panel.
		/// </summary>
		public RelayCommand ToggleDemoCommentVisibility
		{
			get
			{
				return _toggleDemoCommentVisibility
					   ?? (_toggleDemoCommentVisibility = new RelayCommand(
						   () =>
						   {
							   ShowDemoComment = !ShowDemoComment;
						   },
						   () => Demo != null));
			}
		}

		/// <summary>
		/// Command to save demo's comment.
		/// </summary>
		public RelayCommand SaveCommentDemo
		{
			get
			{
				return _saveCommentDemo
					   ?? (_saveCommentDemo = new RelayCommand(
						   async () =>
						   {
							   try
							   {
								   await _cacheService.WriteDemoDataCache(Demo);
							   }
							   catch (Exception e)
							   {
								   Logger.Instance.Log(e);
								   await _dialogService.ShowErrorAsync("An error occured while saving demo's comment", MessageDialogStyle.Affirmative);
							   }
						   }));
			}
		}

		/// <summary>
		/// Command to back to demo's details view.
		/// </summary>
		public RelayCommand BackToDemoDetails
		{
			get
			{
				return _backToDemoDetails
					   ?? (_backToDemoDetails = new RelayCommand(
						   () =>
						   {
							   var detailsViewModel = new ViewModelLocator().DemoDetails;
							   detailsViewModel.CurrentDemo = Demo;
							   var mainViewModel = new ViewModelLocator().Main;
							   DemoDetailsView detailsView = new DemoDetailsView();
							   mainViewModel.CurrentPage.ShowPage(detailsView);
							   Cleanup();
						   },
						   () => Demo != null && !IsBusy));
			}
		}

		/// <summary>
		/// Command to start movie generation.
		/// </summary>
		public RelayCommand GenerateMovie
		{
			get
			{
				return _generateMovie
					   ?? (_generateMovie = new RelayCommand(
						   async () =>
						   {
							   if (!File.Exists(Demo.Path))
							   {
								   await _dialogService.ShowErrorAsync(string.Format(Properties.Resources.DialogErrorDemoNotFound, Demo.Name), MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (!IsHLAEInstalled)
							   {
								   await _dialogService.ShowErrorAsync("Please install HLAE first from HLAE section.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (!_movieConfig.UseVirtualDub && !IsFFmpegInstalled)
							   {
								   await _dialogService.ShowErrorAsync("Please install FFmpeg first from FFmpeg section.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (_movieConfig.UseVirtualDub && !IsVirtualDubInstalled)
							   {
								   await _dialogService.ShowErrorAsync("Please install VirtualDub first from VirtualDub section.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (string.IsNullOrEmpty(CsgoExePath))
							   {
								   await _dialogService.ShowErrorAsync("Please select csgo.exe path from HLAE section.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (string.IsNullOrEmpty(RawFilesFolderDestination))
							   {
								   await _dialogService.ShowErrorAsync("Please select a RAW files folder destination.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (StartTick <= 0)
							   {
								   await _dialogService.ShowErrorAsync("Please select a valid start tick.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (EndTick <= 0)
							   {
								   await _dialogService.ShowErrorAsync("Please select a valid end tick.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (EndTick <= StartTick)
							   {
								   await _dialogService.ShowErrorAsync("End tick must be greater than start tick.", MessageDialogStyle.Affirmative);
								   return;
							   }
							   if (RequiredSpace >= 30)
							   {
								   MessageDialogResult confirm = await _dialogService.ShowMessageAsync($"You need about {RequiredSpace} GB available on your HDD.{Environment.NewLine}Do you want to continue?", MessageDialogStyle.AffirmativeAndNegative);
								   if (confirm == MessageDialogResult.Negative) return;
							   }

							   try
							   {
								   _movieConfig.GenerateRawFiles = true; // reset raw files generation in case of multiple attempt
																		 // check if there is already tga files and ask if we want to delete it or not
								   if (_movieService.IsFirstTgaExists())
								   {
									   MessageDialogResult choice = await _dialogService.ShowTgaFound(_movieConfig.GenerateVideoFile);
									   if (_movieConfig.GenerateVideoFile)
									   {
										   if (choice == MessageDialogResult.FirstAuxiliary) return;
										   if (choice == MessageDialogResult.Affirmative) _movieService.DeleteTgaFiles();
										   if (choice == MessageDialogResult.Negative) _movieConfig.GenerateRawFiles = false;
									   }
									   else
									   {
										   if (choice == MessageDialogResult.Negative) return;
										   _movieService.DeleteTgaFiles();
									   }
								   }

								   IsBusy = true;
								   Win32Utils.SendMessageToBot(Win32Utils.WM_TOGGLE_DOWNLOAD_NOTIFICATION);
								   await _movieService.Start();
								   // TODO notification
							   }
							   catch (Exception e)
							   {
								   Logger.Instance.Log(e);
								   await _dialogService.ShowErrorAsync("An error occured while generating the video." + Environment.NewLine + e.Message, MessageDialogStyle.Affirmative);
							   }
							   finally
							   {
								   IsBusy = false;
								   HasNotification = false;
								   Win32Utils.SendMessageToBot(Win32Utils.WM_TOGGLE_DOWNLOAD_NOTIFICATION);
							   }
						   },
						   () => Demo != null && !IsBusy));
			}
		}

		public RelayCommand Cancel
		{
			get
			{
				return _cancel
					   ?? (_cancel = new RelayCommand(
						   () =>
						   {
							   _movieService.Cancel();
						   },
						   () => Demo != null && IsBusy));
			}
		}

		/// <summary>
		/// Command to install FFmpeg.
		/// </summary>
		public RelayCommand InstallFFmpeg
		{
			get
			{
				return _installFFmpeg
					   ?? (_installFFmpeg = new RelayCommand(
						   async () =>
						   {
							   try
							   {
								   if (!AppSettings.IsInternetConnectionAvailable())
								   {
									   await _dialogService.ShowNoInternetConnectionAsync();
									   return;
								   }

								   Notification = "Installing FFmpeg...";
								   HasNotification = true;
								   IsInstalling = true;

								   bool installed = await FFmpegService.Install();
								   if (!installed)
								   {
									   await _dialogService.ShowMessageAsync("FFmpeg has not been installed.", MessageDialogStyle.Affirmative);
									   return;
								   }

								   await _dialogService.ShowMessageAsync("FFmpeg has been installed.", MessageDialogStyle.Affirmative);
								   RaisePropertyChanged(() => IsFFmpegInstalled);
								   RaisePropertyChanged(() => IsInstallFFmpegAvailable);
								   RaisePropertyChanged(() => FFmpegVersion);
								   RaisePropertyChanged(() => FFmpegCommand);
							   }
							   catch (Exception e)
							   {
								   Logger.Instance.Log(e);
								   await _dialogService.ShowErrorAsync("An error occured while installing FFmpeg." + Environment.NewLine + e.Message, MessageDialogStyle.Affirmative);
							   }
							   finally
							   {
								   HasNotification = false;
								   IsInstalling = false;
								   CommandManager.InvalidateRequerySuggested();
							   }
						   }, () => !IsBusy && !IsInstalling));
			}
		}

		/// <summary>
		/// Command to install VirtualDub.
		/// </summary>
		public RelayCommand InstallVirtualDub
		{
			get
			{
				return _installVirtualDub
					   ?? (_installVirtualDub = new RelayCommand(
						   async () =>
						   {
							   try
							   {
								   if (!AppSettings.IsInternetConnectionAvailable())
								   {
									   await _dialogService.ShowNoInternetConnectionAsync();
									   return;
								   }

								   Notification = "Installing VirtualDub...";
								   HasNotification = true;
								   IsInstalling = true;

								   bool installed = await VirtualDubService.Install();
								   if (!installed)
								   {
									   await _dialogService.ShowMessageAsync("VirtualDub has not been installed.", MessageDialogStyle.Affirmative);
									   return;
								   }

								   await _dialogService.ShowMessageAsync("VirtualDub has been installed.", MessageDialogStyle.Affirmative);
								   RaisePropertyChanged(() => IsVirtualDubInstalled);
								   RaisePropertyChanged(() => IsInstallVirtualDubAvailable);
								   RaisePropertyChanged(() => VirtualDubVersion);
							   }
							   catch (Exception e)
							   {
								   Logger.Instance.Log(e);
								   await _dialogService.ShowErrorAsync("An error occured while installing VirtualDub." + Environment.NewLine + e.Message, MessageDialogStyle.Affirmative);
							   }
							   finally
							   {
								   HasNotification = false;
								   IsInstalling = false;
								   CommandManager.InvalidateRequerySuggested();
							   }
						   }, () => !IsBusy && !IsInstalling));
			}
		}

		/// <summary>
		/// Command to install HLAE.
		/// </summary>
		public RelayCommand InstallHLAE
		{
			get
			{
				return _installHLAE
					   ?? (_installHLAE = new RelayCommand(
						   async () =>
						   {
							   try
							   {
								   if (!AppSettings.IsInternetConnectionAvailable())
								   {
									   await _dialogService.ShowNoInternetConnectionAsync();
									   return;
								   }

								   Notification = "Installing HLAE...";
								   HasNotification = true;
								   IsInstalling = true;

								   bool installed = await HlaeService.UpgradeHlae();
								   if (!installed)
								   {
									   await _dialogService.ShowMessageAsync("HLAE installation failed.", MessageDialogStyle.Affirmative);
									   return;
								   }

								   await _dialogService.ShowMessageAsync("HLAE has been installed.", MessageDialogStyle.Affirmative);
								   RaisePropertyChanged(() => IsHLAEInstalled);
								   RaisePropertyChanged(() => HLAEVersion);

								   // ask for csgo.exe path if it has not been detected
								   if (string.IsNullOrEmpty(CsgoExePath))
								   {
									   MessageDialogResult confirm = await _dialogService.ShowMessageAsync(Properties.Resources.DialogSelectCsgoPath, MessageDialogStyle.AffirmativeAndNegative);
									   if (confirm == MessageDialogResult.Negative) return;

									   string path = HlaeService.ShowCsgoExeDialog();
									   if (!string.IsNullOrEmpty(path)) CsgoExePath = path;
								   }
							   }
							   catch (Exception e)
							   {
								   Logger.Instance.Log(e);
								   await _dialogService.ShowErrorAsync("An error occured while installing HLAE.", MessageDialogStyle.Affirmative);
							   }
							   finally
							   {
								   HasNotification = false;
								   IsInstalling = false;
								   CommandManager.InvalidateRequerySuggested();
							   }
						   }, () => !IsBusy && !IsInstalling));
			}
		}

		/// <summary>
		/// Command to update HLAE.
		/// </summary>
		public RelayCommand UpdateHLAE
		{
			get
			{
				return _updateHLAE
					   ?? (_updateHLAE = new RelayCommand(
						   async () =>
						   {
							   try
							   {
								   if (!AppSettings.IsInternetConnectionAvailable())
								   {
									   await _dialogService.ShowNoInternetConnectionAsync();
									   return;
								   }

								   IsInstalling = true;
								   HasNotification = true;
								   Notification = "Checking for HLAE update...";
								   bool isUpdateAvailable = await HlaeService.IsUpdateAvailable();
								   if (!isUpdateAvailable)
								   {
									   await _dialogService.ShowMessageAsync("No HLAE update available",
										   MessageDialogStyle.Affirmative);
									   return;
								   }

								   Notification = "Updating HLAE...";
								   bool hasUpdated = await HlaeService.UpgradeHlae();
								   if (!hasUpdated)
								   {
									   await _dialogService.ShowMessageAsync("HLAE has not been updated", MessageDialogStyle.Affirmative);
									   return;
								   }

								   await _dialogService.ShowMessageAsync("HLAE has been updated", MessageDialogStyle.Affirmative);
								   RaisePropertyChanged(() => HLAEVersion);
							   }
							   catch (Exception e)
							   {
								   Logger.Instance.Log(e);
								   await _dialogService.ShowErrorAsync("An error occured while updating HLAE.", MessageDialogStyle.Affirmative);
							   }
							   finally
							   {
								   IsInstalling = false;
								   HasNotification = false;
								   CommandManager.InvalidateRequerySuggested();
							   }
						   }, () => !IsBusy && !IsInstalling));
			}
		}

		/// <summary>
		/// Command to reset the user's CFG.
		/// </summary>
		public RelayCommand ResetCfg
		{
			get
			{
				return _resetCfg
					   ?? (_resetCfg = new RelayCommand(
						   () =>
						   {
							   Settings.Default.MovieShowDefaultCfg = true;
							   Settings.Default.MovieUserCfg = string.Empty;
							   RaisePropertyChanged(() => Cfg);
						   }, () => !IsBusy));
			}
		}

		/// <summary>
		/// Command to select the csgo.exe location
		/// </summary>
		public RelayCommand SelectCsgoExePathLocation
		{
			get
			{
				return _selectCsgoExePathLocation
					   ?? (_selectCsgoExePathLocation = new RelayCommand(
						   () =>
						   {
							   string path = HlaeService.ShowCsgoExeDialog();
							   if (!string.IsNullOrEmpty(path))
							   {
								   CsgoExePath = path;
								   RaisePropertyChanged(() => IsHLAEInstalled);
							   }
						   }, () => !IsBusy));
			}
		}

		public RelayCommand SelectRawFilesFolderDestination
		{
			get
			{
				return _selectRawFilesFolderDestination
					   ?? (_selectRawFilesFolderDestination = new RelayCommand(
						   () =>
						   {
							   string folderPath = SelectFolderPath(RawFilesFolderDestination);
							   if (!string.IsNullOrEmpty(folderPath)) RawFilesFolderDestination = folderPath;
						   }, () => !IsBusy));
			}
		}

		public RelayCommand SelectOutputFileDestinationFolder
		{
			get
			{
				return _selectOutputFileDestinationFolder
					   ?? (_selectOutputFileDestinationFolder = new RelayCommand(
						   () =>
						   {
							   string path = SelectFolderPath(OuputFileDestinationFolder);
							   if (!string.IsNullOrEmpty(path)) OuputFileDestinationFolder = path;
						   }, () => !IsBusy));
			}
		}

		public RelayCommand NavigateToRawFilesFolder
		{
			get
			{
				return _navigateToRawFilesFolder
					   ?? (_navigateToRawFilesFolder = new RelayCommand(
						   async () =>
						   {
							   await NavigateToFolder(RawFilesFolderDestination);
						   }, () => !string.IsNullOrEmpty(RawFilesFolderDestination)));
			}
		}

		public RelayCommand NavigateToOutputFileFolderDestination
		{
			get
			{
				return _navigateToOutputFileFolderDestination
					   ?? (_navigateToOutputFileFolderDestination = new RelayCommand(
						   async () =>
						   {
							   await NavigateToFolder(OuputFileDestinationFolder);
						   }, () => !string.IsNullOrEmpty(OuputFileDestinationFolder)));
			}
		}

		public RelayCommand NavigateToFFmpegFolder
		{
			get
			{
				return _navigateToFFmpegFolder
					   ?? (_navigateToFFmpegFolder = new RelayCommand(
						   async () =>
						   {
							   await NavigateToFolder(FFmpegService.GetFFmpegPath());
						   }, () => !string.IsNullOrEmpty(FFmpegService.GetFFmpegPath())));
			}
		}

		public RelayCommand NavigateToHLAEFolder
		{
			get
			{
				return _navigateToHLAEFolder
					   ?? (_navigateToHLAEFolder = new RelayCommand(
						   async () =>
						   {
							   await NavigateToFolder(HlaeService.GetHlaePath());
						   }, () => !string.IsNullOrEmpty(HlaeService.GetHlaePath())));
			}
		}

		public RelayCommand NavigateToVirtualDubFolder
		{
			get
			{
				return _navigateToVirtualDubFolder
					   ?? (_navigateToVirtualDubFolder = new RelayCommand(
						   async () =>
						   {
							   await NavigateToFolder(VirtualDubService.GetVirtualDubPath());
						   }, () => !string.IsNullOrEmpty(VirtualDubService.GetVirtualDubPath())));
			}
		}

		public RelayCommand<int> UpdateStartTickBeforeSeconds
		{
			get
			{
				return _updateStartTickBeforeSeconds
					   ?? (_updateStartTickBeforeSeconds = new RelayCommand<int>(
						   async tick =>
						   {
							   int seconds = await GetSecondsFromInput();
							   if (seconds > 0)
								   StartTick = (int)(tick - seconds * Demo.ServerTickrate);
						   }, tick => !IsBusy));
			}
		}

		public RelayCommand<int> UpdateStartTickAfterSeconds
		{
			get
			{
				return _updateStartTickAfterSeconds
					   ?? (_updateStartTickAfterSeconds = new RelayCommand<int>(
						   async tick =>
						   {
							   int seconds = await GetSecondsFromInput();
							   if (seconds > 0)
								   StartTick = (int)(tick + seconds * Demo.ServerTickrate);
						   }, tick => !IsBusy));
			}
		}

		public RelayCommand<int> UpdateEndTickBeforeSeconds
		{
			get
			{
				return _updateEndTickBeforeSeconds
					   ?? (_updateEndTickBeforeSeconds = new RelayCommand<int>(
						   async tick =>
						   {
							   int seconds = await GetSecondsFromInput();
							   if (seconds > 0)
								   EndTick = (int)(tick - seconds * Demo.ServerTickrate);
						   }, tick => !IsBusy));
			}
		}

		public RelayCommand<int> UpdateEndTickAfterSeconds
		{
			get
			{
				return _updateEndTickAfterSeconds
					   ?? (_updateEndTickAfterSeconds = new RelayCommand<int>(
						   async tick =>
						   {
							   int seconds = await GetSecondsFromInput();
							   if (seconds > 0)
								   EndTick = (int)(tick + seconds * Demo.ServerTickrate);
						   }, tick => !IsBusy));
			}
		}

		public RelayCommand<string> UpdateFocusPlayer
		{
			get
			{
				return _updateFocusPlayer
					   ?? (_updateFocusPlayer = new RelayCommand<string>(
						   name =>
						   {
							   FocusedPlayer = Demo.Players.FirstOrDefault(p => p.Name == name);
							   if (FocusedPlayer != null) FocusOnPlayer = true;
						   }, name => !IsBusy));
			}
		}

		private async Task<int> GetSecondsFromInput()
		{
			string input = await _dialogService.ShowInputAsync(Properties.Resources.Information, "How many seconds?");
			bool isNumeric = int.TryParse(input, out var seconds); // C# 7 power
			if (!isNumeric && input != null)
			{
				await _dialogService.ShowErrorAsync("A valid number is required.", MessageDialogStyle.Affirmative);
			}

			return seconds;
		}

		public RelayCommand<int> UpdateStartTick
		{
			get
			{
				return _updateStartTick
					   ?? (_updateStartTick = new RelayCommand<int>(
						   tick =>
						   {
							   StartTick = tick;
						   }, tick => !IsBusy));
			}
		}

		public RelayCommand<int> UpdateEndTick
		{
			get
			{
				return _updateEndTick
					   ?? (_updateEndTick = new RelayCommand<int>(
						   tick =>
						   {
							   EndTick = tick;
						   }, tick => !IsBusy));
			}
		}

		/// <summary>
		/// Command to select the HLAE config parent folder path.
		/// </summary>
		public RelayCommand SelectHlaeConfigParentPath
		{
			get
			{
				return _selectHlaeConfigParentPath
					   ?? (_selectHlaeConfigParentPath = new RelayCommand(
						   () =>
						   {
							   FolderBrowserDialog dialog = new FolderBrowserDialog();
							   DialogResult result = dialog.ShowDialog();
							   if (result == DialogResult.OK)
							   {
								   string path = dialog.SelectedPath;
								   bool isFolderSelected = !string.IsNullOrWhiteSpace(path);
								   if (isFolderSelected) HlaeConfigParentFolderPath = path;
							   }
						   },
						   () => !IsBusy));
			}
		}

		public RelayCommand WindowLoaded
		{
			get
			{
				return _windowLoaded
					   ?? (_windowLoaded = new RelayCommand(
						   async () =>
						   {
							   try
							   {
								   Notification = "Loading data...";
								   HasNotification = true;
								   IsPovDemo = Demo.Type == Pov.NAME;
								   IsCorruptedDemo = Demo.Ticks == 0;
								   IsIncompatibleDemo = IsCorruptedDemo || IsPovDemo;

								   InitRawFilesDestinationFolder();
								   InitOutputFileDestinationFolder();
								   InitCsgoExePath();
								   InitConfiguration();
								   InitMovieService();

								   IsTimelineAvailable = _cacheService.HasDemoInCache(Demo.Id);
								   // ignore corrupted / POV / not analyzed demos
								   if (!IsIncompatibleDemo && IsTimelineAvailable)
								   {
									   PeriodStart = DateTime.Today;
									   PeriodEnd = DateTime.Today.AddSeconds(Demo.Duration);
									   VisiblePeriodStart = PeriodStart;
									   VisiblePeriodEnd = PeriodStart.AddSeconds(Demo.Duration / 3);
									   Demo.WeaponFired = await _cacheService.GetDemoWeaponFiredAsync(Demo);
									   TimelineEventList = await _demoService.GetTimeLineEventList(Demo);
								   }

								   // check for HLAE update
								   if (AppSettings.IsInternetConnectionAvailable())
								   {
									   bool isUpdateAvailable = await HlaeService.IsUpdateAvailable();
									   if (isUpdateAvailable)
										   await _dialogService.ShowMessageAsync("A new HLAE version is available." + Environment.NewLine + "Please update it by clicking on the \"Update\" button.", MessageDialogStyle.Affirmative);
								   }
							   }
							   catch (Exception e)
							   {
								   Logger.Instance.Log(e);
								   await _dialogService.ShowErrorAsync("An error occured while loading data " + Environment.NewLine + e.Message,
									   MessageDialogStyle.Affirmative);
							   }
							   finally
							   {
								   HasNotification = false;
								   RaisePropertyChanged(() => IsAnalyzeRequired);
							   }
						   }));
			}
		}

		public DemoMovieViewModel(IDialogService dialogService, IDemosService demoService, ICacheService cacheService)
		{
			_dialogService = dialogService;
			_demoService = demoService;
			_cacheService = cacheService;

			AudioBitrateValues = new ObservableCollection<int>
			{
				128,
				256,
				320,
			};

			if (IsInDesignMode)
			{
				IsBusy = false;
			}
		}

		private void HandleOnGameRunning()
		{
			HasNotification = true;
			Notification = "CSGO recording in progress, please wait...";
		}

		private void OnHLAEStarted()
		{
			HasNotification = true;
			Notification = "Starting HLAE, please wait...";
		}

		private void OnFFmpegStarted()
		{
			HasNotification = true;
			Notification = "FFmpeg encoding in progress, please wait...";
		}

		private void OnGameStarted()
		{
			HasNotification = true;
			Notification = "Starting CSGO, please wait...";
		}

		private void OnVirtualDubStarted()
		{
			HasNotification = true;
			Notification = "VirtualDub encoding in progress, please wait...";
		}

		private void InitConfiguration()
		{
			OutputFileName = Path.GetFileNameWithoutExtension(Demo.Name);
			FrameRate = Settings.Default.MovieFramerate;
			AutoCloseGame = Settings.Default.MovieAutoCloseGame;
			GenerateVideoFile = Settings.Default.MovieGenerateVideoFile;
			CleanUpRawFiles = Settings.Default.MovieCleanUpRawFiles;
			Width = Settings.Default.MovieWidth;
			Height = Settings.Default.MovieHeight;
			UseVirtualDub = Settings.Default.MovieUseVirtualDub;
			VideoQuality = Settings.Default.MovieVideoQuality;
			AudioBitrate = Settings.Default.MovieAudioBitrate;
			VideoFrameRate = Settings.Default.MovieVideoFramerate;
			OpenInExplorer = Settings.Default.MovieOpenInExplorer;
			EnableHlaeConfigParentFolder = Settings.Default.MovieEnableHlaeConfigParent;
			HlaeConfigParentFolderPath = Settings.Default.MovieHlaeConfigParentFolderPath;
			if (!Settings.Default.MovieShowDefaultCfg)
				Cfg = Settings.Default.MovieUserCfg;
		}

		private void InitMovieService()
		{
			_movieConfig = new MovieConfiguration
			{
				Demo = Demo,
				StartTick = StartTick,
				EndTick = EndTick,
				RawFilesDestination = RawFilesFolderDestination,
				OutputFilename = OutputFileName,
				OutputFileDestinationFolder = OuputFileDestinationFolder,
				FrameRate = FrameRate,
				UserCfg = new List<string>(Cfg.Split(Environment.NewLine.ToCharArray())),
				Width = Width,
				Height = Height,
				UseVirtualDub = UseVirtualDub,
				VideoQuality = VideoQuality,
				AudioBitrate = AudioBitrate,
				CleanUpRawFiles = CleanUpRawFiles,
				OpenInExplorer = OpenInExplorer,
				EnableHlaeConfigParent = EnableHlaeConfigParentFolder,
				HlaeConfigParentFolderPath = HlaeConfigParentFolderPath,
			};
			_movieService = new MovieService(_movieConfig);
			_movieService.OnVirtualDubStarted += OnVirtualDubStarted;
			_movieService.OnVirtualDubClosed += () => HasNotification = false;
			_movieService.OnFFmpegStarted += OnFFmpegStarted;
			_movieService.OnFFmpegClosed += () => HasNotification = false;
			_movieService.OnGameStarted += OnGameStarted;
			_movieService.OnGameRunning += HandleOnGameRunning;
			_movieService.OnGameClosed += () => HasNotification = false;
			_movieService.OnHLAEStarted += OnHLAEStarted;
			_movieService.OnHLAEClosed += () => HasNotification = false;

			// update FFmpeg CLI when movie service has been created
			RaisePropertyChanged(() => FFmpegCommand);
		}

		/// <summary>
		/// Init the folder's path where all RAW files will be saved.
		/// </summary>
		private void InitRawFilesDestinationFolder()
		{
			string path = Settings.Default.RawFilesFolderDestination;
			if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
				path = AppSettings.GetCsgoPath();
			if (!string.IsNullOrEmpty(path))
				RawFilesFolderDestination = path;
		}

		/// <summary>
		/// Init the path to the file csgo.exe required to use HLAE.
		/// </summary>
		private void InitCsgoExePath()
		{
			// user's settings
			string path = Settings.Default.MovieCsgoExePath;
			// try with auto detection
			if (string.IsNullOrEmpty(path))
				path = AppSettings.GetCsgoExePath();
			if (!string.IsNullOrEmpty(path))
				CsgoExePath = path;
		}

		/// <summary>
		/// Init the folder's path where the final video file will be saved.
		/// </summary>
		private void InitOutputFileDestinationFolder()
		{
			string path = Settings.Default.MovieOutputFileFolderDestination;
			if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
				path = AppSettings.GetCsgoPath();
			if (!string.IsNullOrEmpty(path))
				OuputFileDestinationFolder = path;
		}

		private async Task NavigateToFolder(string path)
		{
			if (!Directory.Exists(path))
			{
				await _dialogService.ShowErrorAsync(Properties.Resources.DialogNoErrorsFile, MessageDialogStyle.Affirmative);
				return;
			}
			string argument = "\"" + path + "\"";
			Process.Start("explorer.exe", argument);
		}

		private static string SelectFolderPath(string selectedPath)
		{
			FolderBrowserDialog folderDialog = new FolderBrowserDialog
			{
				SelectedPath = selectedPath
			};

			DialogResult result = folderDialog.ShowDialog();
			if (result != DialogResult.OK) return string.Empty;
			return Path.GetFullPath(folderDialog.SelectedPath);
		}

		public override void Cleanup()
		{
			base.Cleanup();
			TimelineEventList.Clear();
			StartTick = 0;
			EndTick = 0;
			FocusOnPlayer = false;
			FocusedPlayer = null;
			PlayerFocusSteamID = 0;
			Demo = null;
		}
	}
}