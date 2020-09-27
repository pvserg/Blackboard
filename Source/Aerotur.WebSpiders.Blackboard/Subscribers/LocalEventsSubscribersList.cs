using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Aerotur.WebSpiders.CoreInterfaces;

namespace Aerotur.WebSpiders.Blackboard
{
    /// <summary>
    /// Класс коллекции локальных подписчиков.
    /// </summary>
    internal class LocalEventsSubscribersList : IEnumerable<SubscriberLocalEvents>
    {
        // подписчики
        private readonly Dictionary<Guid, SubscriberLocalEvents> _subscribers = 
            new Dictionary<Guid, SubscriberLocalEvents>();

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Количество элементов в списке.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_subscribers)
                    return _subscribers.Count;
            }
        }

        /// <summary>
        /// Метод обработки появления нового события.
        /// </summary>
        /// <param name="newEvent">Новое событие.</param>
        public void OnNewEvent(Event newEvent)
        {
            // пропускаем событие через строй подписчиков
            SubscriberLocalEvents[] copy;
            lock (_subscribers)
            {
                // приходится копировать, так как коллеция может измениться в процессе 
                // выполнения событий подписчиков
                copy = new SubscriberLocalEvents[_subscribers.Values.Count];
                _subscribers.Values.CopyTo(copy, 0);
            }

            foreach (SubscriberLocalEvents subs in copy)
            {
                // вынесена проверка Filter.IsPass из ф-и OnNewEvent для уменьшения нагрузки на GC
                // При проверке IsPass внути OnNewEvent компилятор создавал экземпляр класса c__DisplayClass3 
                // в начале ф-и OnNewEvent вне зависимости от того нужно ли вызвать асинхронный таск или нет
                if (subs.Filter.IsPass(newEvent))
                {
                    subs.OnNewEvent(newEvent);
                }
            }
        }

        /// <summary>
        /// Метод добавления нового подписчика.
        /// </summary>
        /// <param name="cookie">Идентификатор.</param>
        /// <param name="filter">Фильтр событий.</param>
        /// <param name="channel">Канал передачи событий.</param>
        public void Add(Guid cookie, IEventFilter filter, ISenderChannel channel)
        {
#if LOG_SUBSCRIBERS
            _logger.Debug("LocalEventsSubscribersList.Add cookie: {0}; filter: {1}; channel: {2}", cookie.ToString(), filter.ToString(), channel.ToString());
#endif // LOG_SUBSCRIBERS

            lock (_subscribers)
            {
                if (!_subscribers.ContainsKey(cookie))
                {
                    if (channel == null)
                    {
                        // иногда, в нештатных ситуациях к нам приходит Update (канал null) для удаленного подписчика
                        // это возможно при обрыве связи между хостами или падении сред. Логгирование добавлено для 
                        // проверки кто и почему выполняет update.
                        _logger.Error("LocalEventsSubscribersList:Add channel is null!!! cookie: "+cookie);
                        throw new ArgumentNullException("channel");
                    }
                    _subscribers[cookie] = new SubscriberLocalEvents(cookie, filter, channel);

                    var subsriberChannel = _subscribers[cookie].SendChannel as ICommunicationChannel;
                    subsriberChannel.StateChanged += commChannel_StateChanged;
                }
                else // обновляем подписчика
                {
                    _subscribers[cookie].Filter = filter;

                    if (channel != null)
                    {
                        _subscribers[cookie].SendChannel = channel;
                    }
                }                
            }

            var commChannel = channel as ICommunicationChannel;
            if (null != commChannel)
            {
                commChannel.StateChanged += commChannel_StateChanged;
            }
        }

        /// <summary>
        /// Метод удаления подписчика по идентификатору.
        /// </summary>
        /// <param name="cookie">Идентификатор.</param>
        /// <returns>Возвращает удаленный элемент.</returns>
        public SubscriberLocalEvents Remove(Guid cookie)
        {
#if LOG_SUBSCRIBERS
            _logger.Debug("LocalEventsSubscribersList.Remove cookie: {0}", cookie.ToString());
#endif // LOG_SUBSCRIBERS

            SubscriberLocalEvents res = null;
            lock (_subscribers)
            {
                if (_subscribers.ContainsKey(cookie))
                {
                    res = _subscribers[cookie];
                    _subscribers.Remove(cookie);

                    var subsriberChannel = res.SendChannel as ICommunicationChannel;
                    if (subsriberChannel != null)
                    {
                        subsriberChannel.Close();
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Метод получения подписчика по идентификатору.
        /// </summary>
        /// <param name="cookie">Идентификатор.</param>
        /// <returns>Возвращает подписчика.</returns>
        public SubscriberLocalEvents GetSubscriber(Guid cookie)
        {
            SubscriberLocalEvents res = null;
            lock (_subscribers)
            {
                if (_subscribers.ContainsKey(cookie))
                {
                    res = _subscribers[cookie];
                }
            }
            return res;
        }

        #region события канала

        // если канал здох, надо отстрелять подписчика - в следующий раз придет с новым каналом
        void commChannel_StateChanged(ICommunicationChannel channel, ConnectionState state)
        {
            if (!channel.IsConnected())
            {
                lock (_subscribers)
                {
                    IEnumerable<Guid> toKill =
                        (from s in _subscribers.Where(t =>
                           (t.Value.SendChannel is ICommunicationChannel) &&
                           channel.ID == ((ICommunicationChannel)t.Value.SendChannel).ID)
                         select s.Key).ToList();

                    foreach (Guid id in toKill)
                    {
                        _subscribers.Remove(id);
                    }
                }
                
            }
        }

        #endregion события канала

        #region IEnumerable<SubscriberLocalEvents> Members

        public IEnumerator<SubscriberLocalEvents> GetEnumerator()
        {
            return _subscribers.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _subscribers.Values.GetEnumerator();
        }

        #endregion
    }
}
