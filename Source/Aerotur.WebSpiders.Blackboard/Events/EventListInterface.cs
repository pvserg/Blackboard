using Aerotur.WebSpiders.CoreInterfaces;

namespace Aerotur.WebSpiders.Blackboard
{
    /// <summary>
    /// Интерфейс списка событий.
    /// </summary>
	internal interface IEventList
	{
        /// <summary>
        /// Метод добавления нового сыбытия.
        /// </summary>
        /// <param name="newEvent">Новое событие.</param>
		void AddEvent(ref Event newEvent);
	}
}
