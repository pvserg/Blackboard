using System;
using System.Collections.Generic;
using System.Diagnostics;
using Aerotur.WebSpiders.CoreInterfaces;
using Aerotur.WebSpiders.DataDescription.Events;

namespace BlackboardTest
{
    public class ReceveEventBase
    {
        protected readonly List<Guid> _listCookies2Unsubscribe = new List<Guid>();

        public void SubscribeEvent(IEnumerable<Guid> guidEventIds, DataReceiveEventHandler handler)
        {
            var filter = new EventFilter();
            filter.Types.AddRange(guidEventIds);

            Guid guidEventCookie;
            IReceiverChannel eventChannel;
            UnitySingleton.Resolve<IBlackboardWrapper>().Subscribe(filter, out eventChannel, handler, out guidEventCookie);

            _listCookies2Unsubscribe.Add(guidEventCookie);
        }
    }

    public class ReceiveEvent1 : ReceveEventBase
    {

        public void StartTracking()
        {
            // подписываемся на изменения состояния источника
            SubscribeEvent(new[] { EventTypes.UserEvent }, OnReceiveEvent1);
        }

        void StopTracking()
        {
            foreach (var guid in _listCookies2Unsubscribe)
            {
                UnitySingleton.Resolve<IBlackboardWrapper>().Unsubscribe(guid);
            }

            _listCookies2Unsubscribe.Clear();
        }

        void OnReceiveEvent1(IReceiverChannel channel)
        {
            try
            {
                object obj;
                channel.GetData(out obj);

                var ev = (Event)obj;

                if (ev.ExtData is EventData eventData)
                {
                    Console.WriteLine($"Пришло сообщение из журнала: {eventData.EventName} {eventData.EventCount} {eventData.EventDate}");
                }
                else
                {
                    Debug.Assert(false, "Данные должны приходить типа {EventData}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка при обработке данных из журнала.");
                //Log.ErrorException("Ошибка при обработке данных из журнала.", e);
            }
        }
    }

    
}
