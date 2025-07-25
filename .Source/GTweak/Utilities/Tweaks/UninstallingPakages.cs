﻿using GTweak.Utilities.Controls;
using GTweak.Utilities.Helpers;
using GTweak.Utilities.Managers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GTweak.Utilities.Tweaks
{
    internal sealed class UninstallingPakages : TaskSchedulerManager
    {
        internal static bool IsOneDriveInstalled => File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive", "OneDrive.exe"));
        private static bool _isLocalAccount = false;

        internal void LoadInstalledPackages() => InstalledPackages = RegistryHelp.GetSubKeyNames<HashSet<string>>(Registry.CurrentUser, @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
        internal static HashSet<string> InstalledPackages = new HashSet<string>();

        internal static readonly Dictionary<string, (string Alias, bool IsUnavailable, List<string> Scripts)> PackagesDetails = new Dictionary<string, (string Alias, bool IsUnavailable, List<string> Scripts)>()
        {
            ["OneDrive"] = (null, false, null),
            ["MicrosoftStore"] = (null, false, new List<string> { "Microsoft.WindowsStore" }),
            ["Todos"] = ("TodoList", false, new List<string> { "Microsoft.Todos", "Microsoft.ToDo" }),
            ["BingWeather"] = ("MSWeather", false, new List<string> { "Microsoft.BingWeather" }),
            ["Microsoft3D"] = ("3DViewer", false, new List<string> { "Microsoft.Microsoft3DViewer" }),
            ["Music"] = ("zunemusic", false, new List<string> { "Microsoft.ZuneMusic", "Microsoft.GrooveMusic" }),
            ["GetHelp"] = (null, false, new List<string> { "Microsoft.GetHelp" }),
            ["MicrosoftOfficeHub"] = ("officehub", false, new List<string> { "Microsoft.MicrosoftOfficeHub" }),
            ["MicrosoftSolitaireCollection"] = ("solitaire", false, new List<string> { "Microsoft.MicrosoftSolitaireCollection" }),
            ["MixedReality"] = ("MixedRealityPortal", false, new List<string> { "Microsoft.MixedReality.Portal" }),
            ["Xbox"] = (null, false, new List<string> { "Microsoft.XboxApp", "Microsoft.GamingApp", "Microsoft.XboxGamingOverlay", "Microsoft.XboxGameOverlay", "Microsoft.XboxIdentityProvider", "Microsoft.Xbox.TCUI", "Microsoft.XboxSpeechToTextOverlay" }),
            ["Paint3D"] = (null, false, new List<string> { "Microsoft.MSPaint" }),
            ["OneNote"] = ("MSOneNote", false, new List<string> { "Microsoft.Office.OneNote", "Microsoft.OneNote" }),
            ["People"] = (null, false, new List<string> { "Microsoft.People" }),
            ["MicrosoftStickyNotes"] = ("MSStickyNotes", false, new List<string> { "Microsoft.MicrosoftStickyNotes" }),
            ["Widgets"] = ("Windows.Client.WebExperience", false, new List<string> { "MicrosoftWindows.Client.WebExperience", "Microsoft.WidgetsPlatformRuntime", "Windows.Client.WebExperience" }),
            ["ScreenSketch"] = (null, false, new List<string> { "Microsoft.ScreenSketch" }),
            ["Phone"] = ("PhoneLink", false, new List<string> { "Microsoft.YourPhone", "MicrosoftWindows.CrossDevice" }),
            ["Photos"] = ("MSPhotos", false, new List<string> { "Microsoft.Windows.Photos" }),
            ["FeedbackHub"] = ("feedback", false, new List<string> { "Microsoft.WindowsFeedbackHub" }),
            ["SoundRecorder"] = (null, false, new List<string> { "Microsoft.WindowsSoundRecorder" }),
            ["Alarms"] = (null, false, new List<string> { "Microsoft.WindowsAlarms" }),
            ["SkypeApp"] = ("Skype", false, new List<string> { "Microsoft.SkypeApp" }),
            ["Maps"] = (null, false, new List<string> { "Microsoft.WindowsMaps" }),
            ["Camera"] = (null, false, new List<string> { "Microsoft.WindowsCamera" }),
            ["Video"] = ("zunevideo", false, new List<string> { "Microsoft.ZuneVideo" }),
            ["BingNews"] = (null, false, new List<string> { "Microsoft.BingNews" }),
            ["Mail"] = ("communicationsapps", false, new List<string> { "microsoft.windowscommunicationsapps" }),
            ["MicrosoftTeams"] = ("Teams", false, new List<string> { "MicrosoftTeams", "MSTeams" }),
            ["PowerAutomateDesktop"] = (null, false, new List<string> { "Microsoft.PowerAutomateDesktop" }),
            ["Cortana"] = (null, false, new List<string> { "Microsoft.549981C3F5F10" }),
            ["ClipChamp"] = ("Clipchamp Video Editor", false, new List<string> { "Clipchamp.Clipchamp" }),
            ["GetStarted"] = (null, false, new List<string> { "Microsoft.Getstarted" }),
            ["BingSports"] = (null, false, new List<string> { "Microsoft.BingSports" }),
            ["BingFinance"] = (null, false, new List<string> { "Microsoft.BingFinance" }),
            ["MicrosoftFamily"] = ("FamilySafety", false, new List<string> { "MicrosoftCorporationII.MicrosoftFamily" }),
            ["BingSearch"] = (null, false, new List<string> { "Microsoft.BingSearch" }),
            ["Outlook"] = (null, false, new List<string> { "Microsoft.OutlookForWindows" }),
            ["QuickAssist"] = (null, false, new List<string> { "MicrosoftCorporationII.QuickAssist" }),
            ["DevHome"] = (null, false, new List<string> { "Microsoft.Windows.DevHome" }),
            ["WindowsTerminal"] = (null, false, new List<string> { "Microsoft.WindowsTerminal" }),
            ["LinkedIn"] = ("LinkedInforWindows", false, new List<string> { "Microsoft.LinkedIn" }),
            ["WebMediaExtensions"] = (null, false, new List<string> { "Microsoft.WebMediaExtensions" }),
            ["OneConnect"] = ("MobilePlans", false, new List<string> { "Microsoft.OneConnect" }),
            ["Edge"] = ("MicrosoftEdge", false, new List<string> { "Microsoft.MicrosoftEdge.Stable", "Microsoft.MicrosoftEdge.*", "Microsoft.Copilot" }),
        };

        internal static bool HandleAvailabilityStatus(string key, bool? isUnavailable = null)
        {
            if (PackagesDetails.TryGetValue(key, out var details))
            {
                if (isUnavailable.HasValue)
                    PackagesDetails[key] = (details.Alias, isUnavailable.Value, details.Scripts);

                return details.IsUnavailable;
            }
            return false;
        }

        internal async void CheckingForLocalAccount()
        {
            string output = await CommandExecutor.GetCommandOutput("Get-LocalUser | Where-Object { $_.Enabled -match 'True'} | Select-Object -ExpandProperty PrincipalSource");
            _isLocalAccount = output.IndexOf("MicrosoftAccount", StringComparison.OrdinalIgnoreCase) < 0;
        }

        internal static Task RestoreOneDriveFolder()
        {
            return Task.Run(async () =>
            {
                await CommandExecutor.InvokeRunCommand(@"/c %systemroot%\System32\OneDriveSetup.exe & %systemroot%\SysWOW64\OneDriveSetup.exe").ConfigureAwait(false);

                RegistryHelp.CreateFolder(Registry.ClassesRoot, @"CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}");
                RegistryHelp.CreateFolder(Registry.ClassesRoot, @"Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}");
            });
        }

        internal static Task RemoveAppxPackage(string packageName, bool shouldRemoveWebView = false)
        {
            if (packageName == "OneDrive")
            {
                return Task.Run(async () =>
                {
                    await CommandExecutor.InvokeRunCommand(@"/c taskkill /f /im OneDrive.exe & %systemroot%\System32\OneDriveSetup.exe /uninstall & %systemroot%\SysWOW64\OneDriveSetup.exe /uninstall").ConfigureAwait(false);

                    RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, @"CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}");
                    RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, @"Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}");

                    CommandExecutor.RunCommand($@"/c rd /s /q %userprofile%\AppData\Local\Microsoft\OneDrive & rd /s /q %userprofile%\AppData\Local\OneDrive & 
                    rd /s /q ""%allusersprofile%\Microsoft OneDrive"" & rd /s /q {PathLocator.Folders.SystemDrive}OneDriveTemp{(_isLocalAccount ? @" & rd /s /q %userprofile%\OneDrive" : "")}");
                });
            }

            return Task.Run(async () =>
            {
                try
                {
                    var (Alias, _, Scripts) = PackagesDetails[packageName];

                    List<string> packageNamesToRemove = new List<string> { packageName };

                    if (!string.IsNullOrEmpty(Alias))
                        packageNamesToRemove.Add(Alias);

                    if (Scripts != null)
                        packageNamesToRemove.AddRange(Scripts);

                    List<string> psCommands = packageNamesToRemove.SelectMany(name => new[]
                    {
                        $@"Get-AppxPackage -AllUsers | Where-Object {{ $_.Name -like '*{name}*' }} | ForEach-Object {{ Remove-AppxPackage -AllUsers -Package $_.PackageFullName }}",
                        $@"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like '*{name}*' }} | ForEach-Object {{ Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName -AllUsers -Verbose }}"
                    }).ToList();

                    await CommandExecutor.InvokeRunCommand(string.Join(" ; ", psCommands), true).ConfigureAwait(false);

                    CommandExecutor.RunCommandAsTrustedInstaller($@"/c for /d %i in ({string.Join(" ", packageNamesToRemove.Select(n => $@"""{Path.Combine(PathLocator.Folders.SystemDrive, "Program Files", "WindowsApps")}\*{n}*"""))}) do takeown /f ""%i"" /r /d Y & icacls ""%i"" /inheritance:r /remove S-1-5-32-544 S-1-5-11 S-1-5-32-545 S-1-5-18 & icacls ""%i"" /grant {Environment.UserName}:F & rd /s /q ""%i""");
                }
                catch (Exception ex) { ErrorLogging.LogDebug(ex); }

                switch (packageName)
                {
                    case "Widgets":
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0, RegistryValueKind.DWord);
                        break;
                    case "Cortana":
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Microsoft\Speech_OneCore\Preferences", "ModelDownloadAllowed", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCloudSearch", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowSearchToUseLocation", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowNewsAndInterests", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.CurrentUser, @"Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.CurrentUser, @"Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.CurrentUser, @"Software\Microsoft\InputPersonalization\TrainedDataStore", "HarvestContacts", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.CurrentUser, @"Software\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", 0, RegistryValueKind.DWord);
                        RegistryHelp.Write(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Windows Search", "CortanaConsent", 0, RegistryValueKind.DWord);
                        break;
                    case "Phone":
                        if (RegistryHelp.KeyExists(Registry.ClassesRoot, @"*\shellex\ContextMenuHandlers\ModernSharing", false))
                        {
                            RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, @"*\shellex\ContextMenuHandlers\SendTo");
                            RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, @"*\shellex\ContextMenuHandlers\ModernShare");
                        }
                        else
                        {
                            RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, @"AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo");
                            RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, @"AllFilesystemObjects\shellex\ContextMenuHandlers\ModernSharing");
                        }
                        CommandExecutor.RunCommandAsTrustedInstaller($@"/c reg delete ""HKEY_CLASSES_ROOT\CLSID\{{7AD84985-87B4-4a16-BE58-8B72A5B390F7}}"" /f & reg delete ""HKEY_CLASSES_ROOT\Wow6432Node\CLSID\{{7AD84985-87B4-4a16-BE58-8B72A5B390F7}}"" /f");
                        break;
                    case "Paint3D":
                        try
                        {
                            using RegistryKey baseKey = Registry.ClassesRoot.OpenSubKey("SystemFileAssociations", true);
                            if (baseKey != null)
                            {
                                foreach (string subkey in baseKey.GetSubKeyNames())
                                {
                                    try
                                    {
                                        using RegistryKey assocKey = baseKey.OpenSubKey(subkey, true);
                                        if (assocKey != null)
                                        {
                                            using RegistryKey shellKey = assocKey.OpenSubKey("Shell", true);
                                            if (shellKey != null)
                                            {
                                                if (shellKey.GetSubKeyNames().Any(k => k.Equals("3D Print", StringComparison.OrdinalIgnoreCase)))
                                                    RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, $@"SystemFileAssociations\{subkey}\shell\3D Print");
                                            }
                                        }
                                    }
                                    catch (Exception ex) { ErrorLogging.LogDebug(ex); }
                                }
                                baseKey.Close();
                            }
                        }
                        catch (Exception ex) { ErrorLogging.LogDebug(ex); }
                        break;
                    case "Edge":
                        TakingOwnership.GrantDebugPrivilege();
                        foreach (string process in new string[] { "msedge.exe", "pwahelper.exe", "edgeupdate.exe", "edgeupdatem.exe", "msedgewebview2.exe", "MicrosoftEdgeUpdate.exe", "msedgewebviewhost.exe", "msedgeuserbroker.exe", "usocoreworker.exe", "RuntimeBroker.exe" })
                            CommandExecutor.RunCommandAsTrustedInstaller($"/c taskkill /f /im {process} /t");

                        DeletingTask(edgeTasks);

                        CommandExecutor.RunCommandAsTrustedInstaller(@"/c rmdir /s /q %LocalAppData%\Microsoft\Edge");
                        CommandExecutor.RunCommandAsTrustedInstaller(@"/c for /r ""%AppData%\Microsoft\Internet Explorer\Quick Launch"" %f in (*Edge*) do del ""%f""");
                        CommandExecutor.RunCommandAsTrustedInstaller(@"/c del /q /f ""%AppData%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\*Edge*.lnk""");
                        CommandExecutor.RunCommandAsTrustedInstaller($@"/c for /r ""{PathLocator.Folders.SystemDrive}ProgramData\Microsoft\Windows\Start Menu\Programs"" %f in (*Edge*) do del ""%f""");
                        CommandExecutor.RunCommandAsTrustedInstaller($@"/c for /r ""{PathLocator.Folders.SystemDrive}Users"" %f in (*Edge*) do @if exist ""%f"" del /f /q ""%f""");
                        CommandExecutor.RunCommandAsTrustedInstaller($@"/c for /d %d in (""{PathLocator.Folders.SystemDrive}Program Files (x86)\Microsoft\*Edge*"") do rmdir /s /q ""%d""");
                        CommandExecutor.RunCommandAsTrustedInstaller($@"/c for /f ""delims="" %i in ('dir /b /s ""{PathLocator.Folders.SystemDrive}Windows\System32\Tasks\*Edge*""') do (if exist ""%i"" (if exist ""%i\"" (rmdir /s /q ""%i"") else (del /f /q ""%i"")))");

                        RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", true);
                        RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}", true);
                        RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Edge", true);
                        RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge", true);
                        RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\Classes\MSEdgeHTM", true);
                        RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\Clients\StartMenuInternet\Microsoft Edge", true);

                        if (shouldRemoveWebView)
                        {
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge Update", true);
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\edgeupdate", true);
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\edgeupdatem", true);
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate", true);
                            RegistryHelp.DeleteFolderTree(Registry.ClassesRoot, @"AppID\MicrosoftEdgeUpdate.exe", true);
                            RegistryHelp.DeleteFolderTree(Registry.CurrentUser, @"Software\Microsoft\EdgeUpdate", true);
                            RegistryHelp.DeleteValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "MicrosoftEdgeAutoLaunch_03AF54719E0271FA0A92D5F15CBA10EA");
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\EdgeWebView", true);
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\Microsoft\EdgeWebView", true);
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft EdgeWebView", true);
                            RegistryHelp.DeleteFolderTree(Registry.CurrentUser, @"Software\Microsoft\EdgeWebView", true);
                            RegistryHelp.DeleteFolderTree(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\MicrosoftEdgeElevationService", true);
                        }

                        static void RemoveDirectory(string path)
                        {
                            CommandExecutor.RunCommandAsTrustedInstaller($"/c takeown /f \"{path}\"");
                            CommandExecutor.RunCommandAsTrustedInstaller($"/c icacls \"{path}\" /inheritance:r /remove S-1-5-32-544 S-1-5-11 S-1-5-32-545 S-1-5-18");
                            CommandExecutor.RunCommandAsTrustedInstaller($"/c icacls \"{path}\" /grant {Environment.UserName}:F");
                            CommandExecutor.RunCommandAsTrustedInstaller($"/c rd /s /q \"{path}\"");

                            Thread.Sleep(1000);

                            if (Directory.Exists(path))
                            {
                                TakingOwnership.GrantAdministratorsAccess(path, TakingOwnership.SE_OBJECT_TYPE.SE_FILE_OBJECT);
                                CommandExecutor.RunCommandAsTrustedInstaller($"/c rd /s /q \"{path}\"");
                            }
                        }

                        foreach (string folder in new[] { "Edge", "EdgeCore", "EdgeUpdate", "Temp", "EdgeWebView" })
                        {
                            if (!shouldRemoveWebView && (folder == "EdgeWebView" || folder == "EdgeCore" || folder == "EdgeUpdate"))
                                continue;

                            RemoveDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", folder));
                        }

                        try
                        {
                            using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\InboxApplications");
                            foreach (string subKey in key?.GetSubKeyNames() ?? Array.Empty<string>())
                            {
                                using RegistryKey subKeyEntry = key.OpenSubKey(subKey);
                                string path = subKeyEntry?.GetValue("Path") as string;
                                if (!string.IsNullOrEmpty(path) && path.Equals("Edge"))
                                {
                                    if (!shouldRemoveWebView && path.Contains("WebView"))
                                        continue;

                                    if (path.EndsWith(@"\AppxManifest.xml", StringComparison.OrdinalIgnoreCase))
                                        path = path.Replace(@"\AppxManifest.xml", "").Trim();

                                    RemoveDirectory(path);

                                    key.DeleteSubKey(subKey);

                                    return;
                                }
                            }
                        }
                        catch (Exception ex) { ErrorLogging.LogDebug(ex); }
                        break;
                }
            });
        }
    }
}
