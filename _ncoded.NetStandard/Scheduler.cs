using ncoded.NetStandard.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace ncoded.NetStandard
{
    public class SchedulerDateException : Exception
    {
        public SchedulerDateException(string msg) : base(msg)
        {

        }
    }
    
    public class Scheduler
    {
        private readonly IDateTimeNowProvider _dateTimeProvider;
        private readonly ILogger _logger;
        private readonly List<Schedule> _schedules = new List<Schedule>();
        public ReadOnlyCollection<Schedule> Schedules { get { return _schedules.AsReadOnly(); } }

        public Scheduler(ILogger logger) : this(new DefaultDateTimeProvider(), logger)
        {

        }

        public Scheduler(IDateTimeNowProvider dateTimeProvider, ILogger logger)
        {
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts the give schedule.
        /// </summary>
        /// <param name="schedule">The schedule that will be started.</param>
        public void StartSchedule(Schedule schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException("schedule");
            }

            if (schedule.Begin <= _dateTimeProvider.Now)
            {
                if (schedule.Recurrence <= TimeSpan.Zero)
                {
                    throw new SchedulerDateException("Can't add schedule in past!");
                }

                TryStartRecurringSchedule(schedule);
                return;
            }

            _schedules.Add(schedule);
            _schedules.Sort((A, B) => A.Begin.CompareTo(B.Begin));

            var delay = schedule.Begin - _dateTimeProvider.Now;
            if (schedule.Recurrence != TimeSpan.Zero && schedule.IsInitialStart)
            {
                schedule.IsInitialStart = false;
                delay = TimeSpan.Zero;
            }

            _logger.Info($"Starting schedule in {delay}");
            schedule.Task = Task.Delay(delay, schedule.CancellationTokenSource.Token)
                .ContinueWith(task => {
                    // remove schedule first!
                    _schedules.Remove(schedule);

                    if (task.IsCompleted)
                    {
                        _logger.Info("Launching schedule action.");
                        LaunchScheduleAction(schedule);
                        TryStartRecurringSchedule(schedule);
                    }
                    task.Dispose();
                });
        }

        private void LaunchScheduleAction(Schedule schedule)
        {
            try
            {
                schedule.Action?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }

        private void TryStartRecurringSchedule(Schedule schedule)
        {
            if (schedule.Recurrence > TimeSpan.Zero)
            {
                schedule.Begin = _dateTimeProvider.Now.Add(schedule.Recurrence);
                StartSchedule(schedule);
            }
        }

        /// <summary>
        /// Removes given schedule and cancel task.
        /// </summary>
        /// <param name="schedule">Schedule to remove and Cancel</param>
        public void CancelSchedule(Schedule schedule)
        {
            if (_schedules.Contains(schedule))
            {
                schedule.CancellationTokenSource.Cancel();
                _schedules.Remove(schedule);
            }
        }

        public static bool IsWeekend(DateTime dt)
        {
            return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
        }

        public Schedule CreateRecurringScheduleFromToday(Action action, TimeSpan when, TimeSpan recurrence)
        {
            var schedule = new Schedule(_dateTimeProvider.Now.Date.Add(when), action)
            {
                Recurrence = recurrence
            };
            return schedule;
        }
    }
}
