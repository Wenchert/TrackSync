using System.Collections.Generic;


namespace TrackSync.Interfaces
{
    /// <summary>
    /// Интерфейс для обратного вызова при выборе снаряжения
    /// </summary>
    public interface IEquipmentCallback
    {
        /// <summary>
        /// Метод для обработки выбранного снаряжения
        /// </summary>
        void OnEquipmentSelected(List<Equipment> equipment);
    }
}