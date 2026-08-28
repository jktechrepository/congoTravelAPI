using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel
{
    /// <summary>
    /// Règle hold : tout item avec RoomTypeId &gt; 0 → ClassQuota ;
    /// tous sans type → GlobalQuota ; mélange interdit (XOR).
    /// </summary>
    public static class HotelInventoryModeResolver
    {
        public static HotelInventoryMode FromHoldItems(IReadOnlyList<HotelHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Au moins un item est requis.");

            var anyClass = items.Any(i => i.RoomTypeId is > 0);
            var anyGlobal = items.Any(i => i.RoomTypeId is null or <= 0);
            if (anyClass && anyGlobal)
                throw new InvalidOperationException(
                    "Items mixtes ClassQuota/GlobalQuota interdits (InventoryMode XOR).");
            return anyClass ? HotelInventoryMode.ClassQuota : HotelInventoryMode.GlobalQuota;
        }
    }
}
