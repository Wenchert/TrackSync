using System.Collections.Generic;
using TrackSync.DataBase.Model;


namespace TrackSync.Interfaces
{
    public interface IEquipmentCallback
    {
        void OnEquipmentSelected(List<Equipment> equipment);
    }
}