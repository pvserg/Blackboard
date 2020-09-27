using System;
using System.Threading;
using Aerotur.WebSpiders.Blackboard;
using Aerotur.WebSpiders.Communication;
using Aerotur.WebSpiders.CoreInterfaces;
using Aerotur.WebSpiders.DataDescription.Events;
using BlackboardTest;


namespace BlackboardTester
{
	class Tester
	{
        /// <summary>
        /// Тестовый Id - представляет сущность, к которой относится событие
        /// </summary>
        private static Guid TestId = new Guid("96494B12-5796-48FE-99BA-42549B140ABA");

        /// <summary>
        /// Время генерации события (мс)
        /// </summary>
        private const long EventGenerationDelay1 = 1 * 1 * 10 * 1000;

        /// <summary>
        /// таймер для генерации
        /// </summary>
        private static readonly Timer EventGenerationTimer1 = new Timer(InvokeGenerationEventTimer1, null, EventGenerationDelay1, EventGenerationDelay1);

        /// <summary>
        /// событие - генерация нового эвента
        /// </summary>
        private static event Action GenerationEvent1 = delegate { };


        /// <summary>
        /// вызов события генерации
        /// </summary>
        /// <param name="state"></param>
        private static void InvokeGenerationEventTimer1(object state)
        {
            GenerationEvent1();
        }


        public Tester()
		{
            var transport = new Transport(Guid.NewGuid());
            UnitySingleton.RegisterInstance(transport);
            UnitySingleton.RegisterInstance<ITransport>(transport);

            var blackboardWrapper = new BlackboardWrapper();
            // регистрируем в Unity
            UnitySingleton.RegisterInstance(blackboardWrapper);
            UnitySingleton.RegisterInstance<IBlackboardWrapper>(blackboardWrapper);

            // подписываемся на таймер генерации события
            GenerationEvent1 += OnGenerationEvent1;
        }

        private static int _eventCount = 0;
        private void OnGenerationEvent1()
        {
            EventGenerationTimer1.Change(Timeout.Infinite, 0);
            try
            {
                ++_eventCount;
                var newTime = DateTime.Now;
                //var eventParams = new List<Event.EventParam>
                //{
                //    new Event.EventParam("%s", "UserEvent"),
                //    new Event.EventParam("%Count", (_eventCount).ToString()),//count
                //    new Event.EventParam("%Time", newTime.ToString(CultureInfo.InvariantCulture))//time
                //};

                var eventData = new EventData("UserEvent", _eventCount, newTime);
                var newEvent = new Event(EventTypes.UserEvent, TestId, eventData);
                UnitySingleton.Resolve<IBlackboardWrapper>().PublishEvent(newEvent);
            }
            finally
            {
                EventGenerationTimer1.Change(EventGenerationDelay1, EventGenerationDelay1);
            }
        }

	}
}
