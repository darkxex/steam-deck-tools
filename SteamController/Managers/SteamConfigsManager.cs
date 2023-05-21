using System.Diagnostics;
using CommonHelpers;
using SteamController.Helpers;

namespace SteamController.Managers
{
    public sealed class SteamConfigsManager : Manager
    {
        static readonly Dictionary<String, byte[]> lockedSteamControllerFiles = new Dictionary<string, byte[]>
        {
            // Use existing defaults in BasicUI and BigPicture
            // { "controller_base/basicui_neptune.vdf", Resources.basicui_neptune },
            // { "controller_base/bigpicture_neptune.vdf", Resources.bigpicture_neptune },
            { "controller_base/desktop_neptune.vdf", Resources.empty_neptune },
            { "controller_base/chord_neptune.vdf", Resources.chord_neptune }
        };
        static readonly Dictionary<String, byte[]> lockedSteamControllerGuideFiles = new Dictionary<string, byte[]>
        {
            { "controller_base/desktop_neptune.vdf", Resources.empty_neptune },
            { "controller_base/chord_neptune.vdf", Resources.chord_neptune_guide }
        };
        static readonly Dictionary<String, byte[]> installedSteamControllerFiles = new Dictionary<string, byte[]>
        {
            { "controller_base/templates/controller_neptune_steamcontroller.vdf", Resources.empty_neptune },
        };

        private enum LockState
        {
            Disabled,
            GuideLock,
            FullLock
        }

        private LockState lockState = LockState.Disabled;

        public SteamConfigsManager()
        {
            // always unlock configs when changed
            Settings.Default.SettingChanging += UnlockControllerFiles;
            SetSteamControllerFilesLock(LockState.Disabled);
        }

        private bool IsActive
        {
            get
            {
                return Settings.Default.SteamControllerConfigs == Settings.SteamControllerConfigsMode.Overwrite &&
                Settings.Default.EnableSteamDetection == true;
            }
        }

        public override void Dispose()
        {
            SetSteamControllerFilesLock(LockState.Disabled);
            Settings.Default.SettingChanging -= UnlockControllerFiles;
        }

        private void UnlockControllerFiles(string key)
        {
            SetSteamControllerFilesLock(LockState.Disabled);
        }

        public override void Tick(Context context)
        {
            if (!IsActive)
                return;

            LockState currentState = LockState.Disabled;

            if (Helpers.SteamConfiguration.IsRunning)
                currentState = Settings.Default.EnableSteamKeyboard ? LockState.GuideLock : LockState.FullLock;

            if (currentState == lockState)
                return;

            SetSteamControllerFilesLock(currentState);
        }

        private void SetSteamControllerFilesLock(LockState newState)
        {
            if (!IsActive)
                return;

            Log.TraceLine("SetSteamControllerFilesLock: {0}", newState);

            switch (newState)
            {
                case LockState.Disabled:
                    foreach (var config in lockedSteamControllerFiles)
                        Helpers.SteamConfiguration.ResetConfigFile(config.Key);
                    break;

                case LockState.GuideLock:
                    foreach (var config in lockedSteamControllerGuideFiles)
                        Helpers.SteamConfiguration.OverwriteConfigFile(config.Key, config.Value, true);
                    foreach (var config in installedSteamControllerFiles)
                        Helpers.SteamConfiguration.OverwriteConfigFile(config.Key, config.Value, false);
                    break;

                case LockState.FullLock:
                    foreach (var config in lockedSteamControllerFiles)
                        Helpers.SteamConfiguration.OverwriteConfigFile(config.Key, config.Value, true);
                    foreach (var config in installedSteamControllerFiles)
                        Helpers.SteamConfiguration.OverwriteConfigFile(config.Key, config.Value, false);
                    break;
            }

            lockState = newState;
        }
    }
}
