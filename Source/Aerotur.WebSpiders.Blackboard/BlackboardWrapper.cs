using System;
using NLog;
using Aerotur.WebSpiders.CoreInterfaces;



namespace Aerotur.WebSpiders.Blackboard
{
    /// <summary>
    /// Класс обертки над Blackboard. Инкапсулирует в себе способ взаимодействия с Blackboard.
    /// </summary>
    public class BlackboardWrapper : IExecutiveService, IBlackboardWrapper
	{
        // логгер
        private static readonly Logger Logger = LogManager.GetLogger("Blackboard");

        // локальный Blackboard
		private IBlackboard	_localBlackboard;

        /// <summary>
        /// Конструктор.
        /// </summary>
        public BlackboardWrapper()
		{
            _localBlackboard = new Blackboard();
		}

        /// <summary>
        /// Finalize объекта.
        /// </summary>
        ~BlackboardWrapper()
		{
		}
        
        /// <summary>
        /// Метод исполнения команды.
        /// </summary>
        /// <param name="command">Команда.</param>
        public void ExecuteCommand(Command command)
        {
            _localBlackboard.ExecuteCommand(command);
        }

		#region Публичный интерфейс BlackboardWrapper

        /// <summary>
        /// Метод публикации события в журнале.
        /// </summary>
        /// <param name="newEvent">Нвове событие.</param>
        public void PublishEvent(Event newEvent)
		{
            // посылаем команду в Blackboard
		    _localBlackboard.PublishEvent(newEvent);
		}

        private Guid SubscribeCore(IEventFilter filter,
            DataReceiveEventHandler receiveEventHandler,
            out IReceiverChannel channel)
        {
            Guid cookie;

            // создаем канал ответа (интегральный)
            var localChannel = UnitySingleton.Resolve<ITransport>().CreateIntegralChannel();
            channel = (IReceiverChannel)localChannel;

            // создаем кук 
            cookie = Guid.NewGuid();

            // подписываемся на событие прихода данных по каналу
            if (receiveEventHandler != null)
                channel.DataReceived += receiveEventHandler;

            // посылаем команду в Blackboard
            _localBlackboard.Subscribe(filter, localChannel as ISenderChannel, cookie);
                      
            return cookie;
        }

        /// <summary>
        /// Метод подписки на события журнала.
        /// </summary>
        /// <param name="filter">Фильтр событий.</param>
        /// <param name="channel">Канал передачи событий.</param>
        /// <param name="receiveEventHandler">Обработчик события получения данных в канал. Необязательный параметр.</param>
        /// <param name="cookie">Идентификатор подписки.</param>
        public void Subscribe(IEventFilter filter, out IReceiverChannel channel, 
								DataReceiveEventHandler receiveEventHandler, out Guid cookie)
        {
            cookie = SubscribeCore(filter, receiveEventHandler, out channel);
        }

        /// <summary>
        /// Метод обновления подписки на события журнала.
        /// </summary>
        /// <param name="filter">Фильтр событий.</param>
        /// <param name="cookie">Идентификатор подписки.</param>
        public void Update(IEventFilter filter,Guid cookie)
        {
            _localBlackboard.Subscribe(filter, null, cookie);
        }

        /// <summary>
        /// Метод отписки от событий журнала.
        /// </summary>
        /// <param name="cookie">Идентификатор подписки.</param>
        public void Unsubscribe(Guid cookie)
		{
            UnsubscribeCore(cookie);
        }

        private void UnsubscribeCore(Guid cookie)
        {
            // посылаем команду в Blackboard
            _localBlackboard.Unsubscribe(cookie);			
		}

		#endregion
    }
}
