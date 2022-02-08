﻿using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Tools.WindowsDevicePortal;
using static Microsoft.Tools.WindowsDevicePortal.DevicePortal;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.Security.ExchangeActiveSyncProvisioning;
using System.Linq;
using System.Threading;

namespace WPDevPortal
{
    /// <summary>
    /// The main page of the application.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// The device portal to which we are connecting.
        /// </summary>
        private DevicePortal portal;
        private Certificate certificate;
        private string WDPAddress { get; set; }
        private string WDPUser { get; set; }
        private string WDPPass { get; set; }
        public string pkgResults { get; set; }
        public Task<AppPackages> packages { get; set; }
        public StringBuilder sb { get; set; }
        public StringBuilder sb1 { get; set; }
        private string pkgFullName { get; set; }
        private string pkgAppID { get; set; }
        private string pkgOrigin { get; set; }
        private string pkgPublisher { get; set; }
        private string pkgVersion { get; set; }
        private bool IsMatching { get; set; }
        private DispatcherTimer _timer = new DispatcherTimer();
        private bool isConnected { get; set; }
        /// <summary>
        /// The main page constructor.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            this.EnableDeviceControls(false);
            


            var str = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
            if (str == "Windows.Desktop")
            {
                address.Text = @"http://127.0.0.1:50080";
                WDPAddress = address.Text;
                WDPUser = "Administrator";
                WDPPass = "IJustNeedSomeTextHere";
                connectToDevice.IsEnabled = true;
            }
            else if (str == "Windows.Mobile")
            {
                address.Text = @"https://127.0.0.1";
                WDPAddress = address.Text;
                WDPUser = "Administrator";
                WDPPass = "IJustNeedSomeTextHere";
                connectToDevice.IsEnabled = true;
            }

        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            this.DataContext = this;

            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += UpdateRealTimeData;
            _timer.Start();



        }
       

        /// <summary>
        /// TextChanged handler for the address text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Address_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /// <summary>
        /// If specified in the UI, clears the test output display, otherwise does nothing.
        /// </summary>
        private void ClearOutput()
        {
            bool clearOutput = this.clearOutput.IsChecked.HasValue ? this.clearOutput.IsChecked.Value : false;
            if (clearOutput)
            {
                this.commandOutput.Text = string.Empty;
            }
        }




        /// <summary>
        /// Click handler for the connectToDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void ConnectToDevice_Click(object sender, RoutedEventArgs e)
        {
            
           
            try
            {
                
                ProgBar.Visibility = Visibility.Visible;
                ProgBar.IsEnabled = true;
                ProgBar.IsIndeterminate = true;
                
                                   this.EnableConnectionControls(false);
                this.EnableDeviceControls(false);

                this.ClearOutput();
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   commandOutput.Text = "Connecting, Please wait..";
                               });
                bool allowUntrusted = true;

                portal = new DevicePortal(
                    new DefaultDevicePortalConnection(
                        WDPAddress,
                        WDPUser,
                        WDPPass));
               
               
                EasClientDeviceInformation eas = new EasClientDeviceInformation();
                string DeviceManufacturer = eas.SystemManufacturer;
                string DeviceModel = eas.SystemProductName;
                
                
                sb = new StringBuilder();
                sb1 = new StringBuilder();
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   commandOutput.Text = "";
                               });
                                   sb.Append(this.commandOutput.Text);
                sb.AppendLine("");
                this.commandOutput.Text = sb.ToString();
                portal.ConnectionStatus += async (portal, connectArgs) =>
                {
                    
                    if (connectArgs.Status == DeviceConnectionStatus.Connected)
                    {
                        var token = processesAppsContainerScroll.RegisterPropertyChangedCallback(ScrollViewer.HorizontalOffsetProperty, OnScrollChangedChange);
                        


                        sb.Append("Connected to: ");
                        sb.AppendLine(portal.Address);
                        sb.Append("OS version: ");
                        sb.AppendLine(portal.OperatingSystemVersion);
                        sb.Append("Device family: ");
                        sb.AppendLine(portal.DeviceFamily);
                        sb.Append("Platform: ");
                        sb.AppendLine(String.Format("{0} ({1})",
                            portal.PlatformName,
                            portal.Platform.ToString()));
                        sb.Append("Manufacture: ");
                        sb.AppendLine($"{DeviceManufacturer}");
                        sb.Append("Model: ");
                        sb.AppendLine($"{DeviceModel}");

                        Task<BatteryState> batteryStat = portal.GetBatteryStateAsync();

                        BatteryState batteryResult = batteryStat.Result;
                        sb.Append("Battery Level: ");
                        sb.AppendLine($"{batteryResult.Level.ToString()}%");
                        

                        await Task.Run(async () =>
                        {

                            packages = portal.GetInstalledAppPackagesAsync();
                            foreach (var pkg in packages.Result.Packages)
                            {
                                // Updating te textBox required this to prevent halting the main thread
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () =>
                                    {
                                        appsComboBox.Items.Add(pkg.FullName);
                                        address.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Green);

                                    });
                            }


                        });

                        await Task.Run(() =>
                        {
                            FetchProcessInfo();
                            //FetchPerfInfo();
                            FetchHWInfo();
                        });
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   ProgBar.Visibility = Visibility.Collapsed;
                                   ProgBar.IsEnabled = false;
                                   ProgBar.IsIndeterminate = false;
                               });




                        isConnected = true;
                        
                            FillInitialRealTimeData();
                        

                    }
                    else if (connectArgs.Status == DeviceConnectionStatus.Failed)
                    {
                        sb.AppendLine("Failed to connect to the device.");
                        sb.AppendLine($"{connectArgs.Message}\n\n{connectArgs.Phase}");
                    }
                };

                try
                {
                    // If the user wants to allow untrusted connections, make a call to GetRootDeviceCertificate
                    // with acceptUntrustedCerts set to true. This will enable untrusted connections for the
                    // remainder of this session.
                    if (allowUntrusted)
                    {
                        this.certificate = await portal.GetRootDeviceCertificateAsync(true);
                    }
                    await portal.ConnectAsync(manualCertificate: this.certificate);
                }
                catch (Exception exception)
                {
                    sb.AppendLine(exception.Message);
                }

                this.commandOutput.Text = sb.ToString();
                EnableDeviceControls(true);
                EnableConnectionControls(true);
            }
            catch (Exception ex)
            {
                ProgBar.Visibility = Visibility.Collapsed;
                ProgBar.IsEnabled = false;
                ProgBar.IsIndeterminate = false;
                address.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                commandOutput.Text = ex.Message;
                //ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }

       public void timer_Tick(object sender, EventArgs e)
        {
            FetchPerfInfo();
        }

        /// <summary>
        /// Enables or disables the Connect button based on the current state of the
        /// Address, User name and Password fields.
        /// </summary>
        private void EnableConnectButton()
        {

            this.connectToDevice.IsEnabled = true;
        }



        /// <summary>
        /// Sets the IsEnabled property appropriately for the connection controls.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void EnableConnectionControls(bool enable)
        {
            this.address.IsEnabled = enable;
            this.connectToDevice.IsEnabled = enable;
        }



        /// <summary>
        /// Sets the IsEnabled property appropriately for the device command controls.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        private void EnableDeviceControls(bool enable)
        {
            this.rebootDevice.IsEnabled = enable;
            this.shutdownDevice.IsEnabled = enable;

            this.getIPConfig.IsEnabled = enable;
            this.getWiFiInfo.IsEnabled = enable;
        }



        /// <summary>
        /// Click handler for the getIpConfig button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void GetIPConfig_Click(object sender, RoutedEventArgs e)
        {
            this.ClearOutput();
            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();
            sb.Append(commandOutput.Text);
            sb.AppendLine("Getting IP configuration...");
            commandOutput.Text = sb.ToString();

            try
            {
                IpConfiguration ipconfig = await portal.GetIpConfigAsync();

                foreach (NetworkAdapterInfo adapterInfo in ipconfig.Adapters)
                {
                    sb.Append(" ");
                    sb.AppendLine(adapterInfo.Description);
                    sb.Append("  MAC address :");
                    sb.AppendLine(adapterInfo.MacAddress);
                    foreach (IpAddressInfo address in adapterInfo.IpAddresses)
                    {
                        sb.Append("  IP address :");
                        sb.AppendLine(address.Address);
                    }
                    sb.Append("  DHCP address :");
                    sb.AppendLine($"{adapterInfo.Dhcp.Address.Address}\n");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to get IP config info.");
                sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
            }

            commandOutput.Text = sb.ToString();
            EnableDeviceControls(true);
            EnableConnectionControls(true);
        }

        /// <summary>
        /// Click handler for the getWifiInfo button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void GetWifiInfo_Click(object sender, RoutedEventArgs e)
        {
            this.ClearOutput();
            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();

            sb.Append(commandOutput.Text);
            sb.AppendLine("Getting WiFi interfaces and networks...");
            commandOutput.Text = sb.ToString();

            try
            {
                WifiInterfaces wifiInterfaces = await portal.GetWifiInterfacesAsync();
                sb.AppendLine("WiFi Interfaces:");
                foreach (WifiInterface wifiInterface in wifiInterfaces.Interfaces)
                {
                    sb.Append(" ");
                    sb.AppendLine(wifiInterface.Description);
                    sb.Append("  GUID: ");
                    sb.AppendLine($"{wifiInterface.Guid.ToString()}\n");

                    WifiNetworks wifiNetworks = await portal.GetWifiNetworksAsync(wifiInterface.Guid);
                    sb.AppendLine("  Networks:");
                    foreach (WifiNetworkInfo network in wifiNetworks.AvailableNetworks)
                    {
                        sb.Append("   SSID: ");
                        sb.AppendLine(network.Ssid);
                        sb.Append("   Profile name: ");
                        sb.AppendLine(network.ProfileName);
                        sb.Append("   is connected: ");
                        sb.AppendLine(network.IsConnected.ToString());
                        sb.Append("   Channel: ");
                        sb.AppendLine(network.Channel.ToString());
                        sb.Append("   Authentication algorithm: ");
                        sb.AppendLine(network.AuthenticationAlgorithm);
                        sb.Append("   Signal quality: ");
                        sb.AppendLine($"{network.SignalQuality.ToString()}\n");

                    }
                };
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to get WiFi info.");
                sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
            }

            commandOutput.Text = sb.ToString();
            EnableDeviceControls(true);
            EnableConnectionControls(true);
        }

        /// <summary>
        /// PasswordChanged handler for the password text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            EnableConnectButton();
        }

        /// <summary>
        /// Click handler for the rebootDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void RebootDevice_Click(object sender, RoutedEventArgs e)
        {
            bool reenableDeviceControls = false;

            this.ClearOutput();
            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();

            sb.Append(commandOutput.Text);
            sb.AppendLine("Rebooting the device");
            commandOutput.Text = sb.ToString();

            try
            {
                await portal.RebootAsync();
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to reboot the device.");
                sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
                reenableDeviceControls = true;
            }

            commandOutput.Text = sb.ToString();
            EnableDeviceControls(reenableDeviceControls);
            EnableConnectionControls(true);
        }

        /// <summary>
        /// Click handler for the shutdownDevice button.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void ShutdownDevice_Click(object sender, RoutedEventArgs e)
        {
            bool reenableDeviceControls = false;

            this.ClearOutput();
            this.EnableConnectionControls(false);
            this.EnableDeviceControls(false);

            StringBuilder sb = new StringBuilder();
            sb.Append(commandOutput.Text);
            sb.AppendLine("Shutting down the device");
            commandOutput.Text = sb.ToString();
            try
            {
                await portal.ShutdownAsync();
            }
            catch (Exception ex)
            {
                sb.AppendLine("Failed to shut down the device.");
                sb.AppendLine(ex.GetType().ToString() + " - " + ex.Message);
                reenableDeviceControls = true;
            }

            commandOutput.Text = sb.ToString();
            EnableDeviceControls(reenableDeviceControls);
            EnableConnectionControls(true);
        }

        /// <summary>
        /// TextChanged handler for the username text box.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private void Username_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableConnectButton();
        }

        /// <summary>
        /// Loads a cert file for cert validation.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="e">The arguments associated with this event.</param>
        private async void LoadCertificate_Click(object sender, RoutedEventArgs e)
        {
            await LoadCertificate();
        }

        /// <summary>
        /// Loads a certificates asynchronously (runs on the UI thread).
        /// </summary>
        /// <returns></returns>
        private async Task LoadCertificate()
        {
            try
            {
                FileOpenPicker filePicker = new FileOpenPicker();
                filePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                filePicker.FileTypeFilter.Add(".cer");

                StorageFile file = await filePicker.PickSingleFileAsync();

                if (file != null)
                {
                    IBuffer cerBlob = await FileIO.ReadBufferAsync(file);

                    if (cerBlob != null)
                    {
                        certificate = new Certificate(cerBlob);
                    }
                }
            }
            catch (Exception exception)
            {
                this.commandOutput.Text = "Failed to get cert file: " + exception.Message;
            }
        }

        private void appsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                applicationList.Text = "";
                sb1.Clear();
                string selected = (sender as ComboBox).SelectedItem.ToString();
                FetchAppInfo(selected);

            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }
        public string stuff { get; set; }
        private async void CheckInstalledPackages()
        {




        }


        /// <summary>
        /// Fetch Package information
        /// </summary>
        /// <param name="selectedItem"></param>
        private void FetchAppInfo(string selectedItem)
        {
            try
            {
                sb1.Append(applicationList.Text);
                sb1.AppendLine("Package Info:\n");
                foreach (var pkg in packages.Result.Packages)
                {
                    if (pkg.FullName == selectedItem)
                    {

                        IsMatching = true;
                        pkgFullName = pkg.FullName;
                        pkgAppID = pkg.AppId;
                        pkgOrigin = PkgOrigin(pkg.PackageOrigin);
                        pkgPublisher = pkg.Publisher;
                        pkgVersion = pkg.Version.Version.ToString(); ;
                    }
                }
                if (IsMatching == true)
                {
                    sb1.Append("Full Name: ");
                    sb1.AppendLine(pkgFullName);
                    sb1.Append("AppID: ");
                    sb1.AppendLine(pkgAppID);
                    sb1.Append("Origin: ");
                    sb1.AppendLine(pkgOrigin);
                    sb1.Append("Publisher: ");
                    sb1.AppendLine(pkgPublisher);
                    sb1.Append("Version: ");
                    sb1.AppendLine(pkgVersion);
                }
                applicationList.Text = sb1.ToString();
            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
            }
        }


        public string result;
        /// <summary>
        /// Determine what origin the app is
        /// </summary>
        /// <param name="pkgOrigin"></param>
        /// <returns></returns>
        private string PkgOrigin(int pkgOrigin)
        {
            try
            {

                switch (pkgOrigin)
                {
                    case 0:
                        result = "Unkown Origin";
                        break;
                    case 1:
                        result = "Unsigned";
                        break;
                    case 2:
                        result = "Inbox App";
                        break;
                    case 3:
                        result = "Store App";
                        break;
                    case 4:
                        result = "Developer Unsigned";
                        break;
                    case 5:
                        result = "Developer Signed";

                        break;
                    case 6:
                        result = "LineOfBusiness(OEM?)";
                        break;
                }
                return result;
            }
            catch (Exception ex)
            {
                ExceptionHelper.Exceptions.ThrownExceptionErrorExtended(ex);
                return "Error finding value";
            }
        }
        private List<StorageFile> depList = new List<StorageFile>();
        private IReadOnlyList<StorageFile> depFiles = new List<StorageFile>();
        private StorageFile pkgFile { get; set; }

        /// <summary>
        /// Uninstall selected App
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void uninstallApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                applicationList.Text = $"Please Wait, Attempting to uninstall {pkgFullName}\n";
                await portal.UninstallApplicationAsync(pkgFullName);
                applicationList.Text = "Uninstall Complete!";
            }
            catch (Exception ex)
            {
                applicationList.Text = $"ERROR:\n" +
                    $"{ex.Message}\n\n" +
                    $"{ex.StackTrace}";
            }
        }

        /// <summary>
        /// Launch selected app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void launchApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                await portal.LaunchApplicationAsync(pkgAppID, pkgFullName);
            }
            catch (Exception ex)
            {
                applicationList.Text = $"ERROR: {ex.Message}\n" +
                    $"{ex.StackTrace}\n{ex.Source}";
            }
        }

        /// <summary>
        /// Load file to install
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void applicationLoadButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".appx");
            picker.FileTypeFilter.Add(".appxbundle");
            pkgFile = await picker.PickSingleFileAsync();
            if (pkgFile == null)
            {
                applicationList.Text = "No File Selected";
            }
            applicationList.Text = $"Loaded: {pkgFile.Name}\n";

        }

        /// <summary>
        /// Load dependencies for the App being Installed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void loadDependencies_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".appx");
            picker.FileTypeFilter.Add(".appxbundle");
            depFiles = await picker.PickMultipleFilesAsync();
            if (depFiles == null)
            {
                applicationList.Text = "No Dependencies Selected";
            }
            foreach (var file in depFiles)
            {
                applicationList.Text += $"Dependency: {file.Name}\n";
                depList.Add(file);
            }
        }



        /// <summary>
        /// Install Selected Application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void installPkg_Click(object sender, RoutedEventArgs e)
        {
            try
            {


                applicationList.Text = $"Installing {pkgFile.Name}";
                await portal.InstallApplicationAsync(pkgFile.Name, pkgFile, depList);
                applicationList.Text = "Complete!";
            }
            catch (Exception ex)
            {
                applicationList.Text = $"ERROR:\n" +
                    $"{ex.Message}\n\n" +
                    $"{ex.StackTrace}\n\n" +
                    $"{ex.Source}";
            }
        }



        public AppRowInfo processRow { get; set; }
        private async void FetchProcessInfo()
        {

            ObservableCollection<AppRowInfo> processesList = new ObservableCollection<AppRowInfo>();
            RunningProcesses proclists = await portal.GetRunningProcessesAsync();
            List<DeviceProcessInfo> processes = proclists.Processes;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () =>
                                {
                                    foreach (var processItem in processes.OrderBy(item => item.Name)    )
                                    {
                                        string PID = processItem.ProcessId.ToString(); //PID Value
                                        string SessionID = processItem.SessionId.ToString(); //PID Value
                                        string Name = processItem.Name; //Name Value
                                        string UserName = processItem.UserName; //UserName Value
                                        string Usage = processItem.Publisher; //Publisher Value
                                        string Memory = ((long)processItem.WorkingSet).ToFileSize(); //Working Set Memory Value
                                        string PageFile = ((long)processItem.PageFile).ToFileSize(); ; //PageFile Value
                                        string Disk = ((long)processItem.TotalCommit).ToFileSize(); //Disk Value
                                        string AppType = processItem.IsXAP.ToString(); //Type Value
                                        processRow = new AppRowInfo(PID, SessionID, Name, UserName, Usage, Memory, PageFile, Disk, AppType);
                                        //Append row
                                        processesList.Add(processRow);
                                    }

                                    //Set Items Source
                                    processesListView.ItemsSource = processesList;
                                });
        }

        public class AppRowInfo : BindableBase
        {
            public string PID { get; internal set; }
            public string Name { get; internal set; }
            public string UserName { get; internal set; }
            public string SessionID { get; internal set; }
            public string AppType { get; internal set; }

            private string usage;
            public string Usage
            {
                get { return usage; }
                set
                {
                    if (usage != value)
                    {
                        usage = value;
                        RaisePropertyChanged("Usage");
                    }
                }
            }

            private string memory;
            public string Memory
            {
                get { return memory; }
                set
                {
                    if (memory != value)
                    {
                        memory = value;
                        RaisePropertyChanged("Memory");
                    }
                }
            }

            private string pfile;
            public string PageFile
            {
                get { return pfile; }
                set
                {
                    if (pfile != value)
                    {
                        pfile = value;
                        RaisePropertyChanged("PageFile");
                    }
                }
            }

            private string disk;
            public string Disk
            {
                get { return disk; }
                set
                {
                    if (disk != value)
                    {
                        disk = value;
                        RaisePropertyChanged("Disk");
                    }
                }
            }
            public AppRowInfo(string pid, string sid, string name, string username, string usage, string memory, string pfile, string disk, string type)
            {
                PID = pid;
                SessionID = sid;
                Name = name;
                UserName = username;
                Usage = usage;
                Memory = memory;
                PageFile = pfile;
                Disk = disk;
                AppType = type;
            }

            public void Update(string usage, string memory, string pfile, string disk)
            {
                if (!Usage.Equals(usage))
                {
                    Usage = usage;
                }
                if (!Memory.Equals(memory))
                {
                    Memory = memory;
                }
                if (!PageFile.Equals(pfile))
                {
                    PageFile = pfile;
                }
                if (!Disk.Equals(disk))
                {
                    Disk = disk;
                }
            }
        }


        private void OnScrollChangedChange(DependencyObject sender, DependencyProperty e)
        {
            try
            {
                var currentHOffset = processesAppsContainerScroll.HorizontalOffset;
                processesAppsHeaderContainerScroll.ChangeView(currentHOffset, 0, 1);
            }
            catch (Exception ex)
            {

            }
        }


      
        private SystemPerformanceInformation perfResult { get; set; }
       
        private string PerformanceResults;
        private string engines { get; set; }
        private async void FetchPerfInfo()
        {
            Task<SystemPerformanceInformation> perf = portal.GetSystemPerfAsync();
            perfResult = perf.Result;

            

           

          

            //Paging/Memory
            uint pagedPoolPages = perfResult.PagedPoolPages;
            uint nonpagedPoolPages = perfResult.NonPagedPoolPages;
            uint pageSize = perfResult.PageSize;
            ulong pagesAvailable = perfResult.AvailablePages;
            uint pagesCommited = perfResult.CommittedPages;
            uint commitLimit = perfResult.CommitLimit;
            uint pagesTotal = perfResult.TotalPages;
            ulong totalInstalledPages = perfResult.TotalInstalledKb;

            PerformanceResults +=
                $"Paging/Memory:\n" +
                $"Page Size: {pageSize}\n" +
                $"Pages Available: {pagesAvailable}\n" +
                $"Pages Commited: {pagesCommited}\n" +
                $"Paged Pool: {pagedPoolPages}\n" +
                $"Non-Paged Pool: {nonpagedPoolPages}\n" +
                $"Pages Total: {pagesTotal}\n" +
                $"Commit Limit: {commitLimit}\n" +
                $"Total Installed Pages: {totalInstalledPages}\n\n";


          
        }

        private List<string> HWList = new List<string>();
        private string HardwareInformation { get; set; }
        private string hwout { get; set; }
        private async void FetchHWInfo()
        {
            HardwareInformation =
                "";

            Task<List<Device>> hardwareList = portal.GetDeviceListAsync();
            Task<List<Device>> hardwareResult = hardwareList;
            foreach (Device device in hardwareResult.Result)
            {
                string devFriendlyname = device.Class;
                string devID = device.ID;
                string devManufacture = device.Manufacturer;
                string devDesc = device.Description;
                hwout =
                    $"Class: {devFriendlyname}\n" +
                    $"Device ID: {devID}\n" +
                    $"Manufacture: {devManufacture}\n" +
                    $"Description: {devDesc}\n\n";
                HWList.Add(hwout);
            }

            HWList.Sort();
            HardwareInformation += String.Join("\n\n", HWList);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () =>
                               {
                                   DevicesText.Text = HardwareInformation;
                               });
            
        }













        public ObservableCollection<RealTimeDataItem> RealTimeData { get { return _realTimeData; } }
        private ObservableCollection<RealTimeDataItem> _realTimeData = new ObservableCollection<RealTimeDataItem>();
        long gpuSharedTotal;
        long gpuSharedUsed;
        long gpuSelfTotal;
        long gpuSelfUsed;
        string gpuname;
        /// <summary>
        /// 
        /// </summary>
        private async void FillInitialRealTimeData()
        {
            SystemPerformanceInformation perf = await portal.GetSystemPerfAsync();

            var startDate = DateTime.Now.AddSeconds(-15);
            var cpuload = perf.CpuLoad;
            var gpu = perf.GpuData.Adapters;
            var network = perf.NetworkData;
            double gpuresult = 0;
            uint pagedPoolPages = perf.PagedPoolPages;
            uint nonpagedPoolPages = perf.NonPagedPoolPages;
            uint pageSize = perf.PageSize;
            ulong pagesAvailable = perf.AvailablePages;
            uint pagesCommited = perf.CommittedPages;
            uint commitLimit = perf.CommitLimit;
            uint pagesTotal = perf.TotalPages;
            ulong totalInstalledPages = perf.TotalInstalledKb;
            long KB = 1024;
            long MB = 1024 * 1024;
            long GB = MB * 1024;
            double convertedFileSize;
            
            foreach (var adapter in gpu)
            {
                
                var maingpu = adapter.EnginesUtilization;
                foreach (var engine in maingpu)
                {
                    gpuname = adapter.Description;
                    gpuresult += engine;
                }
            }
            foreach (var gpumem in gpu)
            {
                convertedFileSize = (long)gpumem.SystemMemory / MB;
                gpuSharedTotal = (long)convertedFileSize;
                convertedFileSize = (long)gpumem.SystemMemoryUsed / MB;
                gpuSharedUsed = (long)convertedFileSize;
                convertedFileSize = (long)gpumem.DedicatedMemory / MB;
                gpuSelfTotal = (long)convertedFileSize;
                convertedFileSize = (long)gpumem.DedicatedMemoryUsed / MB;
                gpuSelfUsed = (long)convertedFileSize;
            }
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                              () =>
                              {
                                  GPUheader.Text += $" {gpuname}";
                              });

            double ioreaddata = (long)perf.IoReadSpeed / MB;
            double iowritedata = (long)perf.IoWriteSpeed / MB;
            double iootherdata = (long)perf.IoOtherSpeed / MB;
            double netread = ((long)network.BytesIn) / KB;
            double netwrite = ((long)network.BytesOut) / KB;
            double totalpages = ((long)pagesTotal);
            double pagescommited = ((long)pagesCommited);
            double pagesavailable = ((long)pagesAvailable);
            double pagesize = ((long)pageSize) / MB;
            double nonpaged = ((long)nonpagedPoolPages);
            double pagedpages = (long)pagedPoolPages;
            var rnd = new Random();

            for (var date = startDate; date < DateTime.Now; date = date.AddSeconds(1))
            {
                RealTimeData.Add(new RealTimeDataItem()
                {
                    Seconds = date.Second,
                    CPULoad = (int)cpuload,
                    GPULoad = (int)gpuresult,
                    GPUMemSharedTotal = gpuSharedTotal,
                    GPUMemShared = gpuSharedUsed,
                    GPUMemSelf = gpuSelfUsed,
                    GPUMemSelfTotal = gpuSelfTotal,
                    IORead = ioreaddata,
                    IOWrite = iowritedata,
                    IOOther = iootherdata,
                    NetRead = netread,
                    NetWrite = netwrite,
                    PagedPages = pagedpages,
                    NonPagedPages = nonpaged,
                    PagesAvailable = pagesavailable,
                    PagesCommited = pagescommited,
                    PageSize = pageSize,
                    TotalPages = totalpages
                });

                cpuload = cpuload < 0 ? 0 : cpuload;
                gpuresult = gpuresult < 0 ? 0 : gpuresult;
                gpuSharedUsed = gpuSharedUsed < 0 ? 0 : gpuSharedUsed;
                gpuSelfUsed = gpuSelfUsed < 0 ? 0 : gpuSelfUsed;
                ioreaddata = ioreaddata < 0 ? 0 : ioreaddata;
                iowritedata = iowritedata < 0 ? 0 : iowritedata;
                iootherdata = iootherdata < 0 ? 0 : iootherdata;
                netread = netread < 0 ? 0 : netread;
                netwrite = netwrite < 0 ? 0 : netwrite;
                totalpages = totalpages < 0 ? 0 : totalpages;
                pagescommited = pagescommited < 0 ? 0 : pagescommited;
                pagesavailable = pagesavailable < 0 ? 0 : pagesavailable;
                pagesize = pagesize < 0 ? 0 : pagesize;
                nonpaged = nonpaged < 0 ? 0 : nonpaged;
                pagedpages = pagedpages < 0 ? 0 : pagedpages;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateRealTimeData(object sender, object e)
        {
            if (isConnected == true)
            {
                SystemPerformanceInformation perf = await portal.GetSystemPerfAsync();
                
                RealTimeData.RemoveAt(0);
                var gpu = perf.GpuData.Adapters;
                var network = perf.NetworkData;
                uint pagedPoolPages = perf.PagedPoolPages;
                uint nonpagedPoolPages = perf.NonPagedPoolPages;
                uint pageSize = perf.PageSize;
                ulong pagesAvailable = perf.AvailablePages;
                uint pagesCommited = perf.CommittedPages;
                uint commitLimit = perf.CommitLimit;
                uint pagesTotal = perf.TotalPages;
                ulong totalInstalledPages = perf.TotalInstalledKb;
                
                double gpuresult = 0;
                long KB = 1024;
                long MB = 1024 * 1024;
                long GB = MB * 1024;
                double convertedFileSize;
               
                foreach (var adapter in gpu)
                {
                    var maingpu = adapter.EnginesUtilization;
                    foreach (var engine in maingpu)
                    {
                        gpuresult += engine;
                    }
                }
                foreach (var gpumem in gpu)
                {
                    convertedFileSize = (long)gpumem.SystemMemory / MB;
                    gpuSharedTotal = (long)convertedFileSize;
                    convertedFileSize = (long)gpumem.SystemMemoryUsed / MB;
                    gpuSharedUsed = (long)convertedFileSize;
                    convertedFileSize = (long)gpumem.DedicatedMemory / MB;
                    gpuSelfTotal = (long)convertedFileSize;
                    convertedFileSize = (long)gpumem.DedicatedMemoryUsed / MB;
                    gpuSelfUsed = (long)convertedFileSize;
                }
                double ioreaddata = (long)perf.IoReadSpeed / MB;
                double iowritedata = (long)perf.IoWriteSpeed / MB;
                double iootherdata = (long)perf.IoOtherSpeed / MB;
                double netread = ((long)network.BytesIn) / KB;
                double netwrite = ((long)network.BytesOut) / KB;
                double totalpages = ((long)pagesTotal);
                double pagescommited = ((long)pagesCommited);
                double pagesavailable = ((long)pagesAvailable);
                double pagesize = ((long)pageSize) / MB;
                double nonpaged = ((long)nonpagedPoolPages);
                double pagedpages = (long)pagedPoolPages;
                var rnd = new Random();
                var cpuload = perf.CpuLoad;
                cpuload = cpuload < 0 ? 0 : cpuload;
                gpuresult = gpuresult < 0 ? 0 : gpuresult;
                gpuSharedUsed = gpuSharedUsed < 0 ? 0 : gpuSharedUsed;
                gpuSelfUsed = gpuSelfUsed < 0 ? 0 : gpuSelfUsed;
                ioreaddata = ioreaddata < 0 ? 0 : ioreaddata;
                iowritedata = iowritedata < 0 ? 0 : iowritedata;
                iootherdata = iootherdata < 0 ? 0 : iootherdata;
                netread = netread < 0 ? 0 : netread;
                netwrite = netwrite < 0 ? 0 : netwrite;
                totalpages = totalpages < 0 ? 0 : totalpages;
                pagescommited = pagescommited < 0 ? 0 : pagescommited;
                pagesavailable = pagesavailable < 0 ? 0 : pagesavailable;
                pagesize = pagesize < 0 ? 0 : pagesize;
                nonpaged = nonpaged < 0 ? 0 : nonpaged;
                pagedpages = pagedpages < 0 ? 0 : pagedpages;

                RealTimeData.Add(new RealTimeDataItem()
                {
                    Seconds = DateTime.Now.Second,
                    CPULoad = (int)cpuload,
                    GPULoad = (int)gpuresult,
                     GPUMemSharedTotal = gpuSharedTotal,
                    GPUMemShared = gpuSharedUsed,
                    GPUMemSelf = gpuSelfUsed,
                    GPUMemSelfTotal = gpuSelfTotal,
                    IORead = ioreaddata,
                    IOWrite = iowritedata,
                    IOOther = iootherdata,
                    NetRead = netread,
                    NetWrite = netwrite,
                    PagedPages = pagedpages,
                    NonPagedPages = nonpaged,
                    PagesAvailable = pagesavailable,
                    PagesCommited = pagescommited,
                    PageSize = pageSize,
                    TotalPages = totalpages

                });
            }
        }


        public class RealTimeDataItem
        {
            public int Seconds { get; set; }
            public int CPULoad { get; set; }
            public int GPULoad { get; set; }
            public double GPUMemShared { get; set; }
            public double GPUMemSelf { get; set; }
            public double GPUMemSharedTotal { get; set; }
            public double GPUMemSelfTotal { get; set; }
            public double IORead { get; set; }
            public double IOWrite { get; set; }
            public double IOOther { get; set; }
            public double NetRead { get; set; }
            public double NetWrite { get; set; }
            public double PagesAvailable { get; set; }
            public double PagesCommited { get; set; }
            public double PageSize { get; set; }
            public double PagedPages { get; set; }
            public double NonPagedPages { get; set; }
            public double TotalPages { get; set; }
        }

        
    }
}