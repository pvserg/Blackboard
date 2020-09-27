using System;
using System.Diagnostics;
using Aerotur.WebSpiders.CoreInterfaces;
using Aerotur.WebSpiders.DataDescription;
using Aerotur.WebSpiders.Tools;

namespace Aerotur.WebSpiders.Blackboard
{
	// мини-фабрика для создания листа локального или подключенного к базе
	internal class EventListFactory
	{
        /// <summary>
        /// Метод создания списка событий.
        /// </summary>
        /// <returns>Возвращает созданный список.</returns>
		static public IEventList CreateEventList()
        {
            return new EventListDB();
        }
	}

    /// <summary>
    /// Класс списка событий в базе данных.
    /// </summary>
	internal class EventListDB : IEventList
	{
        // логгер
        static private readonly NLog.Logger Logger = NLog.LogManager.GetLogger("EventListDB");

        private static readonly ConsecutiveTask AddToDBTask = new ConsecutiveTask();

        #region Private Methods

        private static void AddObject(ref Event newEvent)
        {
            // проверка предусловий
            Debug.Assert(newEvent != null);

            if (newEvent.EventSubject == Guid.Empty)
            {
                Logger.Warn("В событии типа '{0}'не задан EventSubject", newEvent.Type);
            }

            if (newEvent.SourceComputer == Guid.Empty)
            {
                Logger.Warn("В событии типа '{0}'не задан SourceComputer", newEvent.Type);
            }

            // получаем xml c параметрами
            var xmlParams = newEvent.GetParamsAsXml();
            string xmlParamsString = null;

            if (xmlParams != null)
            {
                // сериализуем xml в строку
                xmlParamsString = xmlParams.ToString();
                if (string.IsNullOrWhiteSpace(xmlParamsString))
                {
                    xmlParamsString = null;
                }
            }

        }

        #endregion

        #region IEventList

        /// <summary>
	    /// Метод добавления нового сыбытия.
	    /// </summary>
	    /// <param name="newEvent">Новое событие.</param>
	    public void AddEvent(ref CoreInterfaces.Event newEvent)
		{
		    try
		    {
		        if (!newEvent.PublishToDataBase)
		        {
                    // внутреннее событие, не имеющее отражения в базе данных событий
		            return;
		        }

			    // добавляем событие
			    AddObject(ref newEvent);
            }
            catch (Exception ex)
            {
                // записать ошибку в лог
                Logger.Error(ex, "Ошибка добавления нового события в БД. Событие: " + DebugNames.GetDebugName(newEvent.Type) , ex);
            }		
		}

		#endregion
	}
}
