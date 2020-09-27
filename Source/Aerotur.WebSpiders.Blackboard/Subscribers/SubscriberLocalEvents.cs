using System;

using NLog;
using Aerotur.WebSpiders.CoreInterfaces;
using Aerotur.WebSpiders.Tools;

namespace Aerotur.WebSpiders.Blackboard
{
    /// <summary>
    /// Класс подписчика локальных событий.
    /// </summary>
	internal class SubscriberLocalEvents
	{
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ConsecutiveTask _task = new ConsecutiveTask();

        private int _activeEventsCount;
        private readonly object _counterLock = new object();
        private int _prevEventsCount;
        private const int PeriodEventsCount = 40;

        /// <summary>
        /// Фильтр событий.
        /// </summary>
        public IEventFilter Filter { get;  set; }

        /// <summary>
        /// Канал передачи событий.
        /// </summary>
        public ISenderChannel SendChannel { get; set; }

        /// <summary>
        /// Идентификатор.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Конструктор класса.
        /// </summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="filter">Фильтр событий.</param>
        /// <param name="subsChannel">Канал передачи событий.</param>
		public SubscriberLocalEvents(Guid id, IEventFilter filter, ISenderChannel subsChannel)
		{
			ID = id;
			Filter = filter;
			SendChannel = subsChannel;
		}

        /// <summary>
        /// Метод вызывается при наступлении нового события.
        /// </summary>
        /// <param name="newEvent">Новое событие.</param>
        public void OnNewEvent(Event newEvent)
        {

            // если событие проходит через фильтр
            lock (_counterLock)
            {
                ++_activeEventsCount;
                if (_activeEventsCount - _prevEventsCount >= PeriodEventsCount)
                {
                    Log.Warn("Растёт количество не обработанных событий в очереди подписчика {0}: {1}, Filter={2}",
                        ID,
                        _activeEventsCount,
                        Filter);
                    _prevEventsCount = _activeEventsCount;
                }
                else if (_prevEventsCount - _activeEventsCount > PeriodEventsCount)
                {
                    Log.Warn("Уменьшается количество не обработанных событий в очереди подписчика {0}: {1}, Filter={2}",
                        ID,
                        _activeEventsCount,
                        Filter);
                    _prevEventsCount = _activeEventsCount;
                }

                _task.Run(() => ProcessNewEvent(newEvent));
            }
        }

        #region Private Methods

        private void ProcessNewEvent(Event newEvent)
        {
            try
            {
                // посылаем событие подписчику
                SendChannel.SetData(newEvent);
            }
            catch (Exception ex)
            {
                // что-то произошло при передаче события, но это не повод для прекращения работы
                Log.Error(ex, "Ошибка передачи события " + newEvent, ex);
            }
            finally
            {
                lock (_counterLock)
                {
                    --_activeEventsCount;
                }
            }
        }

        #endregion
    }
}
