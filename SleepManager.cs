using RunLog;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AaTurpin.SleepManager
{
    /// <summary>
    /// Static helper class to manage system power settings related to sleep functionality.
    /// </summary>
    public static class SleepManager
    {
        private static Logger _logger = Log.Logger;
        private static bool _sleepPrevented;
        private static bool _displayPrevented;
        private static readonly object _stateLock = new object();
        private static bool _exitHandlerRegistered;

        // Import the necessary Windows API functions
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(ExecutionState esFlags);

        // Execution state flags
        [Flags]
        private enum ExecutionState : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_CONTINUOUS = 0x80000000
        }

        /// <summary>
        /// Sets a custom logger instance for the SleepManager class.
        /// </summary>
        /// <param name="logger">The logger instance to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when the logger is null.</exception>
        public static void SetLogger(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.Information("SleepManager logger has been configured.");
        }

        /// <summary>
        /// Prevents the system from entering sleep mode.
        /// </summary>
        /// <param name="keepDisplayOn">If true, also prevents the display from turning off.</param>
        /// <returns>True if operation was successful, false otherwise.</returns>
        public static bool PreventSleep(bool keepDisplayOn = false)
        {
            try
            {
                ExecutionState state = ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED;
                if (keepDisplayOn)
                    state |= ExecutionState.ES_DISPLAY_REQUIRED;

                uint result = SetThreadExecutionState(state);

                if (result == 0)
                {
                    _logger.Error("Failed to prevent system sleep. Error code: {0}", Marshal.GetLastWin32Error());
                    return false;
                }

                lock (_stateLock)
                {
                    _sleepPrevented = true;
                    _displayPrevented = keepDisplayOn;
                }

                _logger.Information("System sleep prevented. Keep display on: {0}", keepDisplayOn);

                // Register the exit handler if not already registered
                RegisterExitHandler();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception occurred while trying to prevent system sleep.");
                return false;
            }
        }

        /// <summary>
        /// Allows the system to enter sleep mode normally.
        /// </summary>
        /// <returns>True if operation was successful, false otherwise.</returns>
        public static bool AllowSleep()
        {
            try
            {
                // Reset to default (allow sleep)
                uint result = SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);

                if (result == 0)
                {
                    _logger.Error("Failed to reset system sleep settings. Error code: {0}", Marshal.GetLastWin32Error());
                    return false;
                }

                lock (_stateLock)
                {
                    _sleepPrevented = false;
                    _displayPrevented = false;
                }

                _logger.Information("System sleep settings restored to default.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception occurred while trying to restore system sleep settings.");
                return false;
            }
        }

        /// <summary>
        /// Temporarily prevents the system from sleeping for a specified duration.
        /// </summary>
        /// <param name="duration">The duration to prevent sleep.</param>
        /// <param name="keepDisplayOn">If true, also prevents the display from turning off.</param>
        /// <returns>A task that completes when the temporary prevention period ends.</returns>
        public static async Task PreventSleepTemporarilyAsync(TimeSpan duration, bool keepDisplayOn = false)
        {
            try
            {
                _logger.Information("Temporarily preventing system sleep for {0} minutes.", duration.TotalMinutes);

                // Store previous state
                bool wasPrevented, wasDisplayPrevented;
                lock (_stateLock)
                {
                    wasPrevented = _sleepPrevented;
                    wasDisplayPrevented = _displayPrevented;
                }

                PreventSleep(keepDisplayOn);
                await Task.Delay(duration);

                // Restore previous state
                if (wasPrevented)
                    PreventSleep(wasDisplayPrevented);
                else
                    AllowSleep();

                _logger.Information("Temporary sleep prevention completed.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception during temporary sleep prevention.");
                // Make sure we don't leave the system in a state where it can't sleep
                AllowSleep();
            }
        }

        /// <summary>
        /// Checks if the system is currently prevented from sleeping by this class.
        /// </summary>
        /// <returns>True if sleep is currently being prevented by this class.</returns>
        public static bool IsSleepCurrentlyPrevented()
        {
            lock (_stateLock)
            {
                return _sleepPrevented;
            }
        }

        /// <summary>
        /// Checks if the display is currently prevented from turning off by this class.
        /// </summary>
        /// <returns>True if display power-off is currently being prevented.</returns>
        public static bool IsDisplayCurrentlyPrevented()
        {
            lock (_stateLock)
            {
                return _displayPrevented;
            }
        }

        /// <summary>
        /// Registers an application exit handler to restore sleep settings automatically.
        /// Only registers once to avoid multiple handlers.
        /// </summary>
        private static void RegisterExitHandler()
        {
            lock (_stateLock)
            {
                if (!_exitHandlerRegistered)
                {
                    // Register for process exit event
                    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                    {
                        if (_sleepPrevented)
                        {
                            _logger.Information("Application exiting, automatically restoring sleep settings");
                            AllowSleep();
                        }
                    };

                    // Also register for unhandled exceptions
                    AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                    {
                        if (_sleepPrevented)
                        {
                            _logger.Warning("Unhandled exception occurred, restoring sleep settings");
                            AllowSleep();
                        }
                    };

                    _exitHandlerRegistered = true;
                    _logger.Debug("Application exit handler registered for sleep management");
                }
            }
        }
    }
}