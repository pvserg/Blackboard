using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.ServiceModel;
using Aerotur.WebSpiders.CoreInterfaces;
using Aerotur.WebSpiders.DataDescription;
using Aerotur.WebSpiders.Tools;
using Aerotur.WebSpiders.DataDescription.Errors;
using Aerotur.WebSpiders.DataDescription.Commands;

namespace Aerotur.WebSpiders.Blackboard
{
    /// <summary>
    /// Класс журнала.
    /// </summary>
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
	class Blackboard : 
		IBlackboard,
		ISrvcBlackboard
	{
        // хранилище событий
		private readonly IEventList _eventList;
        // локальные подписчики
        private readonly LocalEventsSubscribersList _localEventsSubscribers;
        // логгер
        private static readonly  NLog.Logger Logger = NLog.LogManager.GetLogger("Blackboard");


        private readonly ConsecutiveTask _subsribeTask = new ConsecutiveTask();

        private readonly ConsecutiveTask _publishEventTask = new ConsecutiveTask();

        /// <summary>
        /// текстовое имя доски объявлений
        /// </summary>
        internal const string ServiceName = "Blackboard";

        /// <summary>
        /// Конструктор.
        /// </summary>
		public Blackboard()
        {
			// создаём локальный список событий
            _eventList = EventListFactory.CreateEventList();
			_localEventsSubscribers = new LocalEventsSubscribersList();
		}

        /// <summary>
        /// Метод исполнения команды.
        /// </summary>
        /// <param name="command">Команда.</param>
        public void ExecuteBlackboardCommand(Command command)
        {
            if (command == null)
                return;

            Exception error = null;
            if (command.CommandID == GeneralServiceCommands.UpdateEntityCollection ||
                command.CommandID == GeneralServiceCommands.UpdateEntity)
            {
                OnUpdateEntityCommand(command);
            }
            else if (command.CommandID == GeneralServiceCommands.CloseService)
            {
                Close();
            }
            else
            {
                error = new NotSupportedException(String.Format("Компонента {0} не поддерживает команду с CommandID:{1}",
                                                            BlackboardService.ServiceName, DebugNames.GetDebugName(command.CommandID)));
            }

            // если есть канал ответа - отсылаем ошибку
            if (command.ResponseChannel != null)
            {
                object data = null;
                if (error != null)
                    data = new ErrorResult(BlackboardService.ServiceId, error, "Ошибка при исполнении команды в Blackboard");
                command.SendResponseData(data);
            }
            else if (error != null)
                throw error;
        }

        /// <summary>
        /// Метод инициализации Журнала по команде.
        /// </summary>
        /// <param name="command">Команда.</param>
        private void OnUpdateEntityCommand(Command command)
        {            
        }

        /// <summary>
        /// Закрытие сервиса.
        /// </summary>
        private void Close()
        {
        }

        // последняя добавленная задача
        private readonly ConsecutiveTask _lastTask = new ConsecutiveTask();

        private void AddEvent(Event newEvent)
        {
            // Добавляем событие, заполняем дополнительные параметры
            _eventList.AddEvent(ref newEvent);
            _localEventsSubscribers.OnNewEvent(newEvent);
        }

        /// <summary>
        /// Метод постановки события в очередь на добавление.
        /// </summary>
        /// <param name="newEvent">Новое событие.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddEventToQueue(Event newEvent)
        {
            _lastTask.Run(() => AddEvent(newEvent));
        }

        #region Члены IBlackboard

        /// <summary>
        /// Метод публикации события в журнале.
        /// </summary>
        /// <param name="newEvent">Новое событие.</param>
        void IBlackboard.PublishEvent(Event newEvent)
		{
            try
            {
                // добавляем событие
                AddEventToQueue(newEvent);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Ошибка публикации события в журнале.");
                throw;
            }
        }

        /// <summary>
        /// Метод подписки на события журнала.
        /// </summary>
        /// <param name="filter">Фильтр событий.</param>
        /// <param name="channel">Канал передачи событий.</param>
        /// <param name="cookie">Идентификатор подписки.</param>
        void IBlackboard.Subscribe(IEventFilter filter, ISenderChannel channel, Guid cookie)
		{
            // локальный подписчик
            if (filter.Host == UnitySingleton.Resolve<ITransport>().HostID)
            {
                // добавляем нового подписчика в карту
                _localEventsSubscribers.Add(cookie, filter, channel);
            }
		}

        /// <summary>
        /// Метод отписки от событий журнала.
        /// </summary>
        /// <param name="cookie">Идентификатор подписки.</param>
        void IBlackboard.Unsubscribe(Guid cookie)
		{
			// удаляем подписчика из карты
            _localEventsSubscribers.Remove(cookie);
		}

		#endregion

		#region Члены ISrvcBlackboard

        /// <summary>
        /// Метод публикации события в журнале.
        /// </summary>
        /// <param name="newEvent">Нвове событие.</param>
        void ISrvcBlackboard.PublishEvent(Event newEvent)
		{
            Logger.Debug("ISrvcBlackboard.PublishEvent: " + newEvent);
            try
            {
                _publishEventTask.Run(() => (this as IBlackboard).PublishEvent(newEvent));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Ошибка публикации события из удаленного процесса");
            }			
		}

        /// <summary>
        /// Метод подписки на события журнала.
        /// </summary>
        /// <param name="filter">Фильтр событий.</param>
        /// <param name="channel">Канал передачи событий.</param>
        /// <param name="cookie">Идентификатор подписки.</param>
        void ISrvcBlackboard.Subscribe(IEventFilter filter, ChannelDescription channel, Guid cookie)
		{
            Logger.Debug("ISrvcBlackboard.Subscribe: подписался {0}", cookie);
			// создаем канал
            ISenderChannel ch = channel != null ? UnitySingleton.Resolve<ITransport>().CreateSenderChannel(channel) : null;

            _subsribeTask.Run(() => (this as IBlackboard).Subscribe(filter, ch, cookie));
		}

        /// <summary>
        /// Метод отписки от событий журнала.
        /// </summary>
        /// <param name="cookie">Идентификатор подписки.</param>
        void ISrvcBlackboard.Unsubscribe(Guid cookie)
		{
            Logger.Debug("ISrvcBlackboard.Unsubscribe: отписался {0}", cookie);

            _subsribeTask.Run(() => (this as IBlackboard).Unsubscribe(cookie));
		}

        /// <summary>
        /// Идентификатор хоста.
        /// </summary>
        Guid ISrvcBlackboard.GetHostID()
        {
            Logger.Debug("ISrvcBlackboard.GetHostID: Запросили HostId");
            return UnitySingleton.Resolve<ITransport>().HostID;
        }

        #endregion

        #region Implementation of IExecutiveService

        /// <summary>
        /// Метод исполнения команды.
        /// </summary>
        /// <param name="command">Команда.</param>
        void IExecutiveService.ExecuteCommand(Command command)
        {
            ExecuteBlackboardCommand(command);
        }

        #endregion

        #region Implementation of ISrvcExecutiveService

        ///// <summary>
        ///// Метод исполнения команды.
        ///// </summary>
        ///// <param name="command">Команда.</param>
        //void ISrvcExecutiveService.ExecuteCommand(Command command)
        //{
        //    ExecuteBlackboardCommand(command);
        //}

        #endregion
    }
}
